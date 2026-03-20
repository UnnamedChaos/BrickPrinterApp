# Lua Widget API Documentation

This document describes the Lua API available for creating custom widgets on the ESP32 OLED display.

## Overview

Lua scripts run on the ESP32 device and can draw graphics and text on the 128x64 pixel OLED display. Each script runs at a configurable interval and has access to drawing primitives and time functions.

## Display Specifications

- **Resolution**: 128x64 pixels
- **Color**: Monochrome (1-bit: on/off)
- **Coordinate System**: Origin (0,0) is top-left corner
- **Refresh**: Call `show()` at the end of your script to update the display

## Drawing Functions

### clear()
Clears the display buffer (fills with black/off pixels).

```lua
clear()
```

**Usage**: Call at the start of your script to reset the display.

---

### pixel(x, y, on)
Draws a single pixel at the specified coordinates.

**Parameters**:
- `x` (integer): X coordinate (0-127)
- `y` (integer): Y coordinate (0-63)
- `on` (boolean): `true` to turn pixel on (white), `false` to turn off (black)

```lua
pixel(64, 32, true)  -- Turn on pixel at center
pixel(10, 10, false) -- Turn off pixel
```

---

### line(x1, y1, x2, y2)
Draws a line between two points.

**Parameters**:
- `x1`, `y1` (integers): Starting point coordinates
- `x2`, `y2` (integers): Ending point coordinates

```lua
line(0, 0, 127, 63)  -- Draw diagonal line
```

---

### rect(x, y, w, h, filled)
Draws a rectangle.

**Parameters**:
- `x`, `y` (integers): Top-left corner coordinates
- `w` (integer): Width in pixels
- `h` (integer): Height in pixels
- `filled` (boolean): `true` for filled rectangle, `false` for outline only

```lua
rect(10, 10, 50, 30, false)  -- Draw rectangle outline
rect(20, 20, 30, 20, true)   -- Draw filled rectangle
```

---

### circle(x, y, r, filled)
Draws a circle.

**Parameters**:
- `x`, `y` (integers): Center point coordinates
- `r` (integer): Radius in pixels
- `filled` (boolean): `true` for filled circle, `false` for outline only

```lua
circle(64, 32, 20, false)  -- Draw circle outline at center
circle(30, 30, 5, true)    -- Draw filled circle
```

---

### text(x, y, str)
Draws text at the specified position.

**Parameters**:
- `x`, `y` (integers): Top-left corner of text
- `str` (string): Text to display

```lua
text(10, 10, "Hello World")
text(0, 0, string.format("%02d:%02d", hour(), minute()))
```

**Note**: Use `setFont()` to change text size before calling `text()`.

---

### setFont(size)
Sets the font size for subsequent `text()` calls.

**Parameters**:
- `size` (integer): Font size (1 = small, 2 = medium, 3+ = large)

```lua
setFont(1)
text(10, 10, "Small text")

setFont(2)
text(10, 25, "Bigger text")
```

---

### show()
Refreshes the display with the current buffer contents.

```lua
show()
```

**Usage**: Call at the end of your script to make all drawing operations visible.

---

## Time Functions

### hour()
Returns the current hour (0-23) in 24-hour format.

```lua
local h = hour()
```

**Returns**: Integer (0-23)

---

### minute()
Returns the current minute (0-59).

```lua
local m = minute()
```

**Returns**: Integer (0-59)

---

### second()
Returns the current second (0-59).

```lua
local s = second()
```

**Returns**: Integer (0-59)

---

### date()
Returns the current date as a formatted string.

```lua
local d = date()
```

**Returns**: String in format `"DD.MM.YYYY"` (e.g., `"20.03.2026"`)

---

## Standard Lua Libraries

The following standard Lua libraries are available:

### Math Library
```lua
math.floor(x)
math.ceil(x)
math.abs(x)
math.sin(x)   -- x in radians
math.cos(x)   -- x in radians
math.tan(x)
math.pi       -- 3.14159... (use local pi = 3.14159 for performance)
```

### String Library
```lua
string.format("%02d:%02d", h, m)
string.len(str)
string.upper(str)
string.lower(str)
```

---

## Complete Example: Analog Clock

```lua
-- Circular Clock Widget
clear()

-- Clock center and radius
local cx = 64
local cy = 32
local r = 30

-- Draw clock face
circle(cx, cy, r, false)

-- Draw hour markers
for i = 0, 11 do
    local angle = (i * 30 - 90) * 3.14159 / 180
    local x1 = math.floor(cx + (r - 4) * math.cos(angle))
    local y1 = math.floor(cy + (r - 4) * math.sin(angle))
    local x2 = math.floor(cx + r * math.cos(angle))
    local y2 = math.floor(cy + r * math.sin(angle))
    line(x1, y1, x2, y2)
end

-- Get current time
local h = hour() % 12
local m = minute()
local s = second()

-- Calculate hand angles (0 degrees = 12 o'clock)
local secAngle = (s * 6 - 90) * 3.14159 / 180
local minAngle = ((m + s / 60) * 6 - 90) * 3.14159 / 180
local hourAngle = ((h + m / 60) * 30 - 90) * 3.14159 / 180

-- Draw hour hand (shortest)
local hx = math.floor(cx + 15 * math.cos(hourAngle))
local hy = math.floor(cy + 15 * math.sin(hourAngle))
line(cx, cy, hx, hy)

-- Draw minute hand (medium)
local mx = math.floor(cx + 22 * math.cos(minAngle))
local my = math.floor(cy + 22 * math.sin(minAngle))
line(cx, cy, mx, my)

-- Draw second hand (longest)
local sx = math.floor(cx + 26 * math.cos(secAngle))
local sy = math.floor(cy + 26 * math.sin(secAngle))
line(cx, cy, sx, sy)

-- Draw center dot
circle(cx, cy, 2, true)

show()
```

---

## Widget Integration (C# Side)

To create a Lua widget in the BrickPrinterApp:

1. **Create Lua script**: Place in `BrickPrinterApp/Scripts/YourWidget.lua`
2. **Create widget class**: Create `BrickPrinterApp/Widgets/YourWidgetWidget.cs`:

```csharp
using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class YourWidgetWidget : IScriptWidget
{
    public string Name => "Your Widget Name";
    public string ScriptLanguage => "lua";
    public int IntervalMs => 1000;  // Update interval in milliseconds

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Scripts.YourWidget.lua");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: YourWidget.lua");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
```

3. **Register widget**: Add to `Program.cs`:

```csharp
widgetService.RegisterScriptWidget(new YourWidgetWidget());
```

The script is automatically embedded as a resource (*.lua files are included in .csproj).

---

## Performance Tips

1. **Use local variables**: Faster than globals
   ```lua
   local pi = 3.14159  -- Better than math.pi in loops
   ```

2. **Minimize calculations in loops**: Pre-calculate when possible
   ```lua
   local angle = i * 6 * pi / 180  -- Calculate once per iteration
   ```

3. **Bounds checking**: Ensure coordinates are within display bounds (0-127, 0-63) to avoid errors

4. **Update interval**: Set `IntervalMs` appropriately
   - Animations: 100-500ms
   - Clocks: 1000ms (every second)
   - Static content: Higher values to reduce CPU usage

---

## Debugging Tips

- Use `text()` to display debug values on screen
- Check ESP32 serial output for Lua errors
- Start simple and add features incrementally
- Test coordinate calculations with `pixel()` before drawing complex shapes
