using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Figgle;
using System.Text.Json.Serialization;
class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine(FiggleFonts.Standard.Render("DotPkg Manager"));
            
            if (args.Length < 2 || args[0] != "install")
            {
                PrintUsage();
                return;
            }

            string packageName = args[1];
            string configPath = GetConfigPath();

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Error: Configuration file not found at {configPath}");
                return;
            }

            var pkgList = LoadPackageList(configPath);
            if (pkgList == null)
            {
                return;
            }

            if (!pkgList.Packages.TryGetValue(packageName, out var package))
            {
                Console.WriteLine($"Error: Package '{packageName}' not found in configuration.");
                Console.WriteLine($"Available packages: {string.Join(", ", pkgList.Packages.Keys)}");
                return;
            }

            InstallPackage(package);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: dotpkg install <package-name>");
        Console.WriteLine("Example: dotpkg install tux-print");
    }

    static string GetConfigPath()
    {
        // 1. Try executable directory
        string exeDir = AppContext.BaseDirectory;
        string exePath = Path.Combine(exeDir, "pkg-list.json");
        
        if (File.Exists(exePath))
            return exePath;

        // 2. Try current working directory
        string workingDir = Directory.GetCurrentDirectory();
        string workingPath = Path.Combine(workingDir, "pkg-list.json");
        
        return workingPath;
    }

    static PkgList? LoadPackageList(string configPath)
    {
        try
        {
            string configContent = File.ReadAllText(configPath, Encoding.UTF8);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var pkgList = JsonSerializer.Deserialize<PkgList>(configContent, options);
            
            if (pkgList?.Packages == null || pkgList.Packages.Count == 0)
            {
                Console.WriteLine("Error: No packages found in configuration file");
                return null;
            }

            return pkgList;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing configuration: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            return null;
        }
    }

    static void InstallPackage(Package pkg)
    {
        try
        {
            Console.WriteLine($"Installing package: {pkg.Symlink}");

            // Validate package configuration
            if (string.IsNullOrEmpty(pkg.Repo))
                throw new ArgumentException("Repository URL is missing");
                
            if (string.IsNullOrEmpty(pkg.BuildCommand))
                throw new ArgumentException("Build command is missing");

            string repoName = pkg.Repo.Split('/').Last().Replace(".git", "");
            string cacheDir = Path.Combine("cache", repoName);

            // 1. Clone repository
            if (!Directory.Exists(cacheDir))
            {
                Console.WriteLine($"Cloning repository: {pkg.Repo}");
                RunCommand($"git clone {pkg.Repo} {cacheDir}");
            }

            // 2. Build project
            Directory.SetCurrentDirectory(cacheDir);
            Console.WriteLine("Building project...");
            RunCommand(pkg.BuildCommand);

            // 3. Verify binary
            string fullBinPath = Path.GetFullPath(pkg.BinPath);
            if (!File.Exists(fullBinPath))
                throw new FileNotFoundException($"Binary not found at: {fullBinPath}");

            // 4. Create symlink
            Console.WriteLine($"Creating symlink in /usr/local/bin/{pkg.Symlink}");
            RunCommand($"sudo ln -sf {fullBinPath} /usr/local/bin/{pkg.Symlink}");

            Console.WriteLine($"Successfully installed: {pkg.Symlink}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Installation failed: {ex.Message}");
        }
        finally
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }
    }

    static void RunCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed ({process.ExitCode}): {command}\n{error}");
        }

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine(output);
        }
    }
}

public class PkgList
{
    public Dictionary<string, Package> Packages { get; set; } = new Dictionary<string, Package>();
}

public class Package
{
    [JsonPropertyName("repo")]
    public string Repo { get; set; } = string.Empty;
    
    [JsonPropertyName("build_command")]
    public string BuildCommand { get; set; } = string.Empty;
    
    [JsonPropertyName("bin_path")]
    public string BinPath { get; set; } = string.Empty;
    
    [JsonPropertyName("symlink")]
    public string Symlink { get; set; } = string.Empty;
}
