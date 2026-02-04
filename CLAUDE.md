# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BrickPrinter is a Windows Forms application that sends images to an ESP32-based display device. The app runs in the system tray and automatically updates a 128x64 pixel OLED display every minute via HTTP POST requests.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Build for release
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Architecture

### Dependency Injection Setup

The application uses Microsoft.Extensions.Hosting for DI configuration (Program.cs:20-35):
- `IDisplayService` / `DisplayService`: Singleton - handles image conversion and HTTP transmission
- `SettingService`: Singleton - manages ESP32 IP address configuration
- `SettingsForm`: Transient - settings dialog instantiated on demand
- `BrickPrinter`: Transient - main form (hidden, runs in tray)

### Core Components

**BrickPrinter (Forms/BrickPrinter.cs)**
- Main form that runs minimized in the system tray
- Initializes a 60-second timer (line 64) to auto-send images via `_displayService.SendImageAsync()`
- Tray menu provides "Jetzt Updaten" (manual update), "Einstellungen" (settings), and "Beenden" (exit)
- Uses `_host.Services.GetRequiredService<SettingsForm>()` to open settings dialog

**DisplayService (Services/DisplayService.cs)**
- Converts 128x64 pixel images to 1-bit raw format (1024 bytes) for OLED display
- Sends binary data to ESP32 via HTTP POST to `http://{IP}/upload`
- `ConvertTo1BitRaw()` packs 8 vertical pixels per byte in column-major order

**SettingService (Services/SettingService.cs)**
- Stores ESP32 IP address (default: 192.168.178.50)
- Provides `EndpointUrl` property for HTTP endpoint construction
- Note: Currently in-memory only; settings do not persist between app restarts

**SettingsForm (Forms/SettingsForm.cs)**
- Modal dialog for configuring ESP32 IP address
- Uses manual component initialization instead of Designer-generated code (InitializeComponentManual)

### Image Requirements

- Images must be exactly 128x64 pixels (enforced in DisplayService.cs:13)
- Source image: `Resources/img.PNG`
- Icon: `Resources/brick.ico`

## Target Framework

.NET 10.0 Windows with Windows Forms (net10.0-windows)
