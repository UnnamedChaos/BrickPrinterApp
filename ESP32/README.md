# ESP32 Multi-Display Receiver

Receives 1024 bytes of binary display data via HTTP POST and shows it on up to 3x 128x64 SSD1306 I2C OLED displays.

## Features

- 3 independent displays on different I2C pins
- WiFi credentials configurable via USB serial (no code changes needed)
- API accepts `?screen=X` parameter to target specific display
- Keep-alive support for fast transfers

## File Structure

```
ESP32/
├── esp32_display.ino   # Main sketch (WiFi setup, main loop)
├── display.h           # Display interface
├── display.cpp         # Display implementation (3x SSD1306)
├── webserver.h         # Web server interface
├── webserver.cpp       # Web server implementation (endpoints)
├── config.h            # Configuration interface
├── config.cpp          # Serial config + Preferences storage
├── platformio.ini      # PlatformIO configuration
└── README.md
```

## Hardware Requirements

- ESP32 development board
- 1-3x SSD1306 128x64 OLED displays (I2C)

## Wiring

| Display | SDA Pin | SCL Pin |
|---------|---------|---------|
| 0       | GPIO 21 | GPIO 22 |
| 1       | GPIO 17 | GPIO 16 |
| 2       | GPIO 32 | GPIO 33 |

All displays: VCC → 3.3V, GND → GND

## Setup

### Option 1: PlatformIO (Recommended)

1. Open folder in VS Code with PlatformIO extension
2. Build and upload: `pio run -t upload`
3. Configure WiFi via serial (see below)

### Option 2: Arduino IDE

1. Install ESP32 board support
2. Install libraries via Library Manager:
   - Adafruit SSD1306
   - Adafruit GFX Library
   - ESPAsyncWebServer
   - AsyncTCP
3. Select board: "ESP32 Dev Module"
4. Upload

## WiFi Configuration (USB Serial)

**No need to edit code!** WiFi credentials are configured via USB serial and stored in flash.

1. Connect ESP32 via USB
2. Open serial monitor (115200 baud)
3. Type commands:

| Command | Description |
|---------|-------------|
| `WIFI:MySSID:MyPassword` | Set WiFi credentials |
| `STATUS` | Show current config |
| `CLEAR` | Clear stored config |
| `REBOOT` | Restart ESP32 |
| `HELP` | Show help |

Example:
```
WIFI:HomeNetwork:SecretPassword123
REBOOT
```

The display shows "WiFi not configured" until you set credentials.

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/upload?screen=X` | POST | Send 1024 bytes to screen X (0-2) |
| `/clear?screen=X` | POST | Clear specific display |
| `/clear` | POST | Clear all displays |
| `/ping` | GET | Keep-alive (returns "ok") |
| `/status` | GET | Device status (JSON) |
| `/` | GET | Info page |

### Examples

```bash
# Send to screen 0
curl -X POST --data-binary @screen.bin "http://192.168.1.100/upload?screen=0"

# Send to screen 1
curl -X POST --data-binary @screen.bin "http://192.168.1.100/upload?screen=1"

# Clear screen 2
curl -X POST "http://192.168.1.100/clear?screen=2"

# Get status
curl "http://192.168.1.100/status"
```

### Status Response

```json
{
  "status": "ok",
  "ip": "192.168.1.100",
  "numDisplays": 3,
  "screens": [
    {"id": 0, "valid": true},
    {"id": 1, "valid": true},
    {"id": 2, "valid": false}
  ],
  "connected": true,
  "lastContactSec": 5
}
```

## Data Format

- 1024 bytes total (128x64 pixels)
- SSD1306 page-based format
- 8 pages of 128 bytes each
- Each byte = 8 vertical pixels (bit 0 = top, bit 7 = bottom)

This matches the format sent by BrickPrinterApp's DisplayService.
