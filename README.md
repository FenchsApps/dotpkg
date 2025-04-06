Package manager for my C# applications.

## Features
- Install packages directly from GitHub
- Automatic compilation
- Simple configuration

## Installation
```bash
git clone https://github.com/FenchsApps/dotpkg.git
cd dotpkg
dotnet publish -c Release -r linux-x64 --self-contained
sudo ln -s $PWD/bin/Release/net6.0/linux-x64/publish/dotpkg /usr/local/bin/dotpkg
```

## Usage
```bash
dotpkg install package-name
```

## configuration
```bash
cd dotpkg
cp pkg-list.json bin/Release/net6.0/linux-x64/publish
```

## Updating configuration file:

```bash
git clone https://github.com/FenchsApps/dotpkg.git/pkg-list.json
cp path/to/new/pkg-list.json path/to/dotpkg/bin/Release/net6.0/linux-x64/publish
```
