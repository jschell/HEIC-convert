# HEIC Auto Converter

[![CI](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml)
[![Release](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml)

A Windows system tray application that automatically monitors folders and converts HEIC/HEIF photos to JPG format in real-time.

## Features

- **Real-time folder monitoring** - Watches one or more folders for new HEIC/HEIF files using `FileSystemWatcher`
- **Automatic conversion** - Converts HEIC to JPG immediately upon detection
- **System tray operation** - Runs silently in the background with a tray icon
- **Settings UI** - Configure watch folders, output locations, quality settings, and more
- **Start with Windows** - Optional auto-start on system boot
- **Conversion queue** - Handles multiple files efficiently with configurable concurrency
- **Batch conversion** - Scan and convert existing HEIC files in watched folders
- **Notifications** - Balloon notifications for conversion completion and errors
- **EXIF preservation** - Auto-orients images based on EXIF data
- **Robust error handling** - Retries locked files, handles invalid files gracefully

## Requirements

- Windows 10 (1809+) or Windows 11
- ~100 MB disk space
- No runtime installation needed (self-contained build)

## Installation

### Option 1: Download Release

Download the latest `HEICAutoConverter.exe` from [Releases](../../releases) and run it. No installation required.

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/yourname/HEIC-convert.git
cd HEIC-convert

# Build (requires .NET 8 SDK)
dotnet build

# Or publish as self-contained single file
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The published executable will be in `bin/Release/net8.0-windows/win-x64/publish/`.

## Usage

1. **Launch the application** - It starts in the system tray
2. **Configure watch folders** - Right-click the tray icon and select "Settings..."
3. **Add folders** - Click "Add Folder..." to select directories to monitor
4. **Adjust settings** - Set output location, JPEG quality, file handling preferences
5. **Save** - The app immediately begins monitoring

### System Tray Menu

- **Status** - Shows current monitoring status and conversion counts
- **Pause / Resume** - Temporarily pause or resume conversion
- **Convert Existing Files** - Scan watched folders for existing HEIC files and convert them
- **Settings...** - Open the settings window (also accessible by double-clicking the tray icon)
- **Exit** - Close the application

## Configuration

Settings are stored in `%APPDATA%\HEICAutoConverter\settings.json`.

| Setting | Default | Description |
|---------|---------|-------------|
| Watch Folders | *(none)* | Directories to monitor for HEIC files |
| Output Strategy | Same Folder | Where to save converted JPGs |
| JPEG Quality | 95 | Output quality (1-100) |
| Original Files | Keep | Keep, delete, or archive originals |
| Include Subdirs | Yes | Monitor subdirectories recursively |
| Max Concurrent | 2 | Simultaneous conversions (1-8) |
| Skip Existing | Yes | Don't re-convert if JPG already exists |
| Start with Windows | No | Launch on system startup |
| Show Notifications | Yes | Show balloon tips on conversion |

## Technology Stack

- **.NET 8** (C#) with self-contained deployment
- **WPF** for settings UI
- **Windows Forms** for system tray integration
- **Magick.NET** (ImageMagick) for HEIC/HEIF decoding and JPEG encoding
- **FileSystemWatcher** for real-time file monitoring

## Project Structure

```
HEICAutoConverter/
├── src/
│   ├── Core/
│   │   ├── ConversionEngine.cs    # HEIC to JPG conversion logic
│   │   ├── ConversionQueue.cs     # Thread-safe queue with concurrency control
│   │   ├── FileWatcher.cs         # FileSystemWatcher implementation
│   │   ├── Logger.cs              # Async file logging
│   │   └── Settings.cs            # Configuration management
│   ├── UI/
│   │   ├── NotificationManager.cs # Batched balloon notifications
│   │   ├── SettingsWindow.xaml    # Settings UI (WPF)
│   │   ├── SettingsWindow.xaml.cs # Settings code-behind
│   │   └── SystemTrayIcon.cs     # Tray icon and context menu
│   ├── App.xaml                   # WPF application definition
│   └── App.xaml.cs                # Application lifecycle
├── assets/
│   └── icon.ico                   # Application icon
├── HEICAutoConverter.csproj       # Project file
├── LICENSE                        # MIT License
└── README.md
```

## Logging

Logs are written to `%APPDATA%\HEICAutoConverter\logs\` with daily rotation. Logs older than 30 days are automatically cleaned up.

## License

[MIT](LICENSE)
