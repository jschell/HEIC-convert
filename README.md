# HEIC Auto Converter

[![CI](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/ci.yml)
[![Release](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml/badge.svg)](https://github.com/jschell/HEIC-convert/actions/workflows/release.yml)

iPhones and iPads save photos in HEIC format, which many Windows apps and websites don't support. This tool sits in your system tray and automatically converts those photos to JPG as soon as they appear in your chosen folders — no extra steps needed.

## Installation

### WinGet

```
winget install JSchell.HEICAutoConverter
```

### Download

Download `HEICAutoConverter.exe` from [Releases](../../releases) and run it. No installation required.

- Windows 10 (1809+) or Windows 11
- ~200 MB disk space
- No .NET runtime needed (self-contained)

### Build from Source

```bash
git clone https://github.com/jschell/HEIC-convert.git
cd HEIC-convert

# Build (requires .NET 8 SDK)
dotnet build

# Or publish as self-contained single file
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Usage

1. Launch the app — it appears in the system tray
2. Right-click the tray icon → **Settings...**
3. Add folders to watch, adjust quality/output preferences
4. Save — monitoring starts immediately

### System Tray Menu

| Action | Purpose |
|--------|---------|
| Status | Current monitoring status and conversion counts |
| Pause / Resume | Temporarily stop or restart conversion |
| Convert Existing Files | Scan watched folders for existing HEIC files |
| Settings... | Open settings (also: double-click the tray icon) |
| Exit | Close the application |

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
| Show Notifications | Yes | Balloon tips on conversion |

## How It Works

- **Real-time monitoring** — `FileSystemWatcher` detects new HEIC/HEIF files as they appear
- **Queued conversion** — thread-safe async queue with configurable concurrency (1-8)
- **Magick.NET** (ImageMagick) handles HEIC decoding and JPEG encoding
- **EXIF preservation** — auto-orients images based on embedded metadata
- **Error recovery** — retries locked files, cleans up partial output on failure

Logs are written to `%APPDATA%\HEICAutoConverter\logs\` with daily rotation. Logs older than 30 days are automatically cleaned up.

## Project Structure

```
HEIC-convert/
├── src/
│   ├── Core/              # Conversion engine, queue, file watcher, settings, logging
│   └── UI/                # System tray icon, settings window, notifications
├── tests/                 # Unit tests (xUnit)
├── docs/                  # Lessons learned, style guides
├── .github/workflows/     # CI and release automation
└── assets/icon.ico        # Application icon
```

## License

[MIT](LICENSE)
