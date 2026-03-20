# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BrickPrinter is a Windows Forms application that sends images to an ESP32-based display device. The app runs in the system tray and supports multiple widget types including Lua-scripted widgets that run directly on the ESP32.

## Solution Structure

This repository contains two projects:

1. **BrickPrinterApp** - Windows Forms application that sends images and Lua scripts to ESP32 OLED displays
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

The application uses Microsoft.Extensions.Hosting for DI configuration (Program.cs:25-47):
- `IDisplayService` / `DisplayService`: Singleton - converts images to 1-bit binary format
- `ITransferService` / `TransferService`: Singleton - handles HTTP transmission of binary data and Lua scripts
- `ITextService` / `RawTextService`: Singleton - converts text lines to 1-bit binary format
- `SettingService`: Singleton - manages ESP32 IP address configuration
- `WidgetService`: Singleton - manages widget registration and scheduling
- `SettingsForm`: Transient - settings dialog instantiated on demand
- `WiFiSetupForm`: Transient - WiFi configuration dialog for ESP32
- `WidgetManagerForm`: Transient - widget assignment and management UI
- `BrickPrinter`: Transient - main form (hidden, runs in tray)

### Widget System

The application supports two types of widgets:

#### 1. Image-based Widgets (IWidget)
Traditional widgets that generate images in C# which are converted to binary and sent to ESP32.

**Example**: `WeatherWidget`, `StockWidget`, `LogoWidget`

#### 2. Script-based Widgets (IScriptWidget)
Widgets that run Lua scripts directly on the ESP32 for smooth animations and reduced network traffic.

**Example**: `CircularClockWidget`, `CyberpunkClockWidget`, `SolarSystemWidget`

**Creating Lua Widgets**:
1. Create Lua script in `BrickPrinterApp/Scripts/YourWidget.lua`
2. Create C# wrapper class in `BrickPrinterApp/Widgets/YourWidgetWidget.cs`
3. Register in `Program.cs` using `widgetService.RegisterScriptWidget()`

**IMPORTANT**: When creating Lua widgets, refer to **BrickPrinterApp/ESP32/LUA_API.md** for complete API documentation.

### Core Components

**BrickPrinter (Forms/BrickPrinter.cs)**
- Main form that runs minimized in the system tray
- Manages widget scheduling and updates via WidgetService
- Tray menu provides:
  - "Jetzt Updaten" - sends current widget state immediately
  - "Widget Manager" - opens widget assignment dialog
  - "Einstellungen" - opens settings dialog
  - "WiFi Setup" - configures ESP32 WiFi credentials
  - "Beenden" - exits application

**WidgetService (Services/WidgetService.cs)**
- Manages widget registration and screen assignments
- Coordinates widget execution on a timer
- Handles both image-based and script-based widgets
- Persists widget assignments to `widget_assignments.json`

**DisplayService (Services/DisplayService.cs)**
- Converts 128x64 pixel images to 1-bit raw format (1024 bytes) for OLED display
- Returns byte[] via `ConvertImageToBinary()` - does not handle transmission
- `ConvertTo1BitRaw()` packs 8 vertical pixels per byte in column-major order

**TransferService (Services/TransferService.cs)**
- Handles HTTP transmission of binary data and Lua scripts to ESP32
- Binary data: POST to `http://{IP}/upload?screen={N}` with content-type `application/octet-stream`
- Lua scripts: POST to `http://{IP}/script?screen={N}&interval={ms}` with content-type `text/plain`
- Returns bool indicating success/failure of transmission

**SettingService (Services/SettingService.cs)**
- Stores ESP32 IP address (persists to settings.json)
- Provides `EndpointUrl` property for HTTP endpoint construction

### Image Requirements

- Images must be exactly 128x64 pixels (enforced in DisplayService.cs)
- OLED displays are monochrome (1-bit)

## Lua Widget API

**Complete API documentation**: `BrickPrinterApp/ESP32/LUA_API.md`

When writing Lua widgets, you have access to:
- **Drawing functions**: `clear()`, `pixel()`, `line()`, `rect()`, `circle()`, `text()`, `setFont()`, `show()`
- **Time functions**: `hour()`, `minute()`, `second()`, `date()`
- **Standard Lua**: `math.*`, `string.*` libraries

Example widget structure:
```lua
clear()
-- Your drawing code here
circle(64, 32, 20, false)
text(10, 10, "Hello")
show()
```

See existing widgets in `BrickPrinterApp/Scripts/` for examples.

## ESP32 Firmware

The ESP32 firmware is located in `BrickPrinterApp/ESP32/esp32_display/`:

**Key Files**:
- `esp32_display.ino` - Main sketch (WiFi, OTA, main loop)
- `display.cpp/.h` - Display driver (supports 3x SSD1306 OLED displays)
- `webserver.cpp/.h` - HTTP endpoints for receiving binary data and Lua scripts
- `lua_runtime.cpp/.h` - Lua VM integration and API implementation
- `config.cpp/.h` - WiFi configuration via serial port
- `platformio.ini` - Build configuration

**Supported Displays**: 3 independent 128x64 SSD1306 I2C OLED displays

**ESP32 API Endpoints**:
- `POST /upload?screen=N` - Upload binary frame data (1024 bytes)
- `POST /script?screen=N&interval=MS` - Upload and run Lua script
- `POST /clear?screen=N` - Clear specific display
- `GET /status` - Get device status

See `BrickPrinterApp/ESP32/README.md` for ESP32 setup and flashing instructions.

## Target Framework

- **BrickPrinterApp**: .NET 10.0 Windows with Windows Forms (net10.0-windows)
- **BrickPrinterDev**: .NET 10.0 ASP.NET Core (net10.0)

## Development Testing

**BrickPrinterDev** is a minimal ASP.NET Core API that simulates the ESP32 endpoint for local development without hardware:

- Exposes POST endpoints matching ESP32 API
- Runs on http://localhost:5224 by default
- Converts received binary data back to PNG images and saves them in `BrickPrinterDev/output/` with timestamps
- Logs received Lua scripts to console
- Update SettingService IP to `localhost:5224` to test against the dev server instead of ESP32

## Common Development Tasks

### Adding a New Lua Widget

1. **Read the Lua API**: Review `BrickPrinterApp/ESP32/LUA_API.md`
2. **Create Lua script**: `BrickPrinterApp/Scripts/MyWidget.lua`
3. **Create C# wrapper**: `BrickPrinterApp/Widgets/MyWidgetWidget.cs`
   - Implement `IScriptWidget` interface
   - Set appropriate `IntervalMs` (update frequency)
4. **Register widget**: Add to `Program.cs`:
   ```csharp
   widgetService.RegisterScriptWidget(new MyWidgetWidget());
   ```
5. **Test**: Run app, open Widget Manager, assign to display

### Adding a New Image Widget

1. **Create widget class**: `BrickPrinterApp/Widgets/MyWidget.cs`
   - Implement `IWidget` interface
   - Generate 128x64 pixel image in `GetImage()`
2. **Register widget**: Add to `Program.cs`:
   ```csharp
   widgetService.RegisterWidget(new MyWidget(displayService));
   ```

### Modifying ESP32 Firmware

1. **Make changes** to files in `BrickPrinterApp/ESP32/esp32_display/`
2. **Build and upload**:
   ```bash
   cd BrickPrinterApp/ESP32/esp32_display
   pio run -t upload --upload-port 192.168.x.x  # OTA update
   ```
3. **Monitor serial output**:
   ```bash
   pio device monitor -b 115200
   ```

## Project-Specific Notes

- **WiFi credentials** are configured via USB serial, not hardcoded
- **Widget assignments** persist to `widget_assignments.json`
- **App settings** persist to `settings.json`
- **Lua scripts** are embedded resources (*.lua auto-included in .csproj)
- **ESP32 supports OTA updates** after initial USB flash
- **Connection handling**: ESP32 may close connections unpredictably; TransferService uses `ConnectionClose: true` and zero connection pooling
