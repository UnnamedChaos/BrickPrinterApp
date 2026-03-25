# ESP32-C3 Multi-Display Receiver

Receives 1024 bytes of binary display data via HTTP POST and shows it on 1-3x 128x64 SSD1306 I2C OLED displays.

## Features

- Configurable 1-3 independent displays with custom I2C pins
- WiFi credentials and display configuration via USB serial (no code changes needed)
- **OTA updates** - upload firmware over WiFi (no USB needed after first flash)
- API accepts `?screen=X` parameter to target specific display
- Lua scripting support for smooth on-device animations
- Keep-alive support for fast transfers

## File Structure

```
ESP32/esp32_display/
├── esp32_display.ino   # Main sketch (WiFi setup, OTA, main loop)
├── display.h           # Display interface
├── display.cpp         # Display implementation (3x SSD1306)
├── webserver.h         # Web server interface
├── webserver.cpp       # Web server implementation (endpoints)
├── config.h            # Configuration interface
├── config.cpp          # Serial config + Preferences storage
└── platformio.ini      # PlatformIO configuration
```

## Hardware Requirements

- ESP32-C3 development board
- 1-3x SSD1306 128x64 OLED displays (I2C)

## Wiring

**Default Configuration (3 displays)**:

| Display | SDA Pin | SCL Pin |
|---------|---------|---------|
| 0       | GPIO 10 | GPIO 21 |
| 1       | GPIO 4  | GPIO 5  |
| 2       | GPIO 8  | GPIO 9  |

All displays: VCC → 3.3V, GND → GND

The pins are fully configurable via serial commands (see Display Configuration below).

## Initial Setup (USB - only needed once)

### Option 1: PlatformIO (Recommended)

1. Open folder in VS Code with PlatformIO extension
2. Hold **BOOT** button on ESP32-C3
3. Build and upload: `pio run -t upload`
4. Release BOOT when upload starts
5. Configure WiFi via serial (see below)

### Option 2: Arduino IDE

1. Install ESP32 board support
2. Install libraries via Library Manager:
   - Adafruit SSD1306
   - Adafruit GFX Library
   - ESPAsyncWebServer
   - AsyncTCP
3. Select board: **"ESP32C3 Dev Module"**
4. Set "USB CDC On Boot": **Enabled**
5. Hold BOOT, click Upload, release when uploading
6. Configure WiFi via serial

## OTA Updates (WiFi - no USB needed!)

After the first USB flash, you can update over WiFi:

### Arduino IDE
1. Tools → Port → Select network port **"ESP32_Display at 192.168.x.x"**
2. Click Upload

### PlatformIO
```bash
# Replace with your ESP32's IP address
pio run -t upload --upload-port 192.168.178.xxx
```

### Or edit platformio.ini:
```ini
upload_protocol = espota
upload_port = 192.168.178.xxx
```

The display shows "OTA Update" with progress during upload.

## Configuration (USB Serial)

**No need to edit code!** WiFi credentials and display setup are configured via USB serial and stored in flash.

1. Connect ESP32 via USB
2. Open serial monitor (115200 baud)
3. Type commands:

| Command | Description |
|---------|-------------|
| `WIFI:MySSID:MyPassword` | Set WiFi credentials |
| `DISPLAY:<count>:<sda0>:<scl0>[:<sda1>:<scl1>...]` | Configure displays and pins |
| `STATUS` | Show current config |
| `CLEAR` | Clear all stored config |
| `REBOOT` | Restart ESP32 |
| `HELP` | Show help |

### WiFi Setup Example:
```
WIFI:HomeNetwork:SecretPassword123
REBOOT
```

### Display Configuration Examples:

**Single display on GPIO 6/7:**
```
DISPLAY:1:6:7
REBOOT
```

**Three displays (default pins):**
```
DISPLAY:3:10:21:4:5:8:9
REBOOT
```

**Two displays:**
```
DISPLAY:2:6:7:4:5
REBOOT
```

The device shows "WiFi not configured" until you set credentials. If no display configuration is set, it uses the default 3-display setup.

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
