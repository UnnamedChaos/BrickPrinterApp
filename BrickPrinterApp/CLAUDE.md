# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BrickPrinter is a Windows Forms application that sends images to an ESP32-based display device. The app runs in the system tray and automatically updates a 128x64 pixel OLED display every minute via HTTP POST requests.

## Solution Structure

This repository contains two projects:

1. **BrickPrinterApp** - Windows Forms application that sends images to ESP32 OLED display
2. **BrickPrinterDev** - ASP.NET Core minimal API for local testing/development (simulates ESP32 endpoint)

## Build and Run Commands

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build BrickPrinterApp/BrickPrinterApp.csproj
dotnet build BrickPrinterDev/BrickPrinterDev.csproj

# Run the Windows Forms app
dotnet run --project BrickPrinterApp/BrickPrinterApp.csproj

# Run the dev API server (for testing without ESP32 hardware)
dotnet run --project BrickPrinterDev/BrickPrinterDev.csproj

# Build for release
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Architecture

### Dependency Injection Setup

The application uses Microsoft.Extensions.Hosting for DI configuration (Program.cs:25-34):
- `IDisplayService` / `DisplayService`: Singleton - converts images to 1-bit binary format
- `ITransferService` / `TransferService`: Singleton - handles HTTP transmission of binary data
- `ITextService` / `TextService`: Singleton - converts text lines to 1-bit binary format
- `SettingService`: Singleton - manages ESP32 IP address configuration
- `SettingsForm`: Transient - settings dialog instantiated on demand
- `BrickPrinter`: Transient - main form (hidden, runs in tray)

### Core Components

**BrickPrinter (Forms/BrickPrinter.cs)**
- Main form that runs minimized in the system tray
- Initializes a 60-second timer that converts image to binary and sends via TransferService
- Tray menu provides:
  - "Jetzt Updaten" - sends current image immediately
  - "Text Senden" - sends sample text with timestamp
  - "Einstellungen" - opens settings dialog
  - "Beenden" - exits application
- Uses `_host.Services.GetRequiredService<SettingsForm>()` to open settings dialog
- Orchestrates the flow: Image/Text → DisplayService/TextService (binary conversion) → TransferService (HTTP POST)

**DisplayService (Services/DisplayService.cs)**
- Converts 128x64 pixel images to 1-bit raw format (1024 bytes) for OLED display
- Returns byte[] via `ConvertImageToBinary()` - does not handle transmission
- `ConvertTo1BitRaw()` packs 8 vertical pixels per byte in column-major order

**TextService (Services/TextService.cs)**
- Converts string arrays (text lines) to 1-bit binary format (1024 bytes)
- Creates a 128x64 Bitmap with black background and white text (Consolas 8pt)
- Each array element represents one line of text on the display
- Uses DisplayService internally for the final binary conversion
- Line height is 10 pixels, allowing ~6 lines of text on the screen

**TransferService (Services/TransferService.cs)**
- Handles HTTP transmission of binary data to ESP32
- Sends data via POST to `http://{IP}/upload` with content-type `application/octet-stream`
- Returns bool indicating success/failure of transmission
- Designed to be reusable by future services that generate binary content

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

- **BrickPrinterApp**: .NET 10.0 Windows with Windows Forms (net10.0-windows)
- **BrickPrinterDev**: .NET 10.0 ASP.NET Core (net10.0)

## Development Testing

**BrickPrinterDev** is a minimal ASP.NET Core API that simulates the ESP32 endpoint for local development without hardware:

- Exposes a POST endpoint at `/upload` to receive binary data (1024 bytes)
- Runs on http://localhost:5224 by default
- Converts received binary data back to PNG images and saves them in `BrickPrinterDev/output/` with timestamps
- Validates that exactly 1024 bytes are received
- Useful for testing image conversion and transfer logic without connecting to physical ESP32 device
- Update SettingService IP to `localhost:5224` to test against the dev server instead of ESP32
