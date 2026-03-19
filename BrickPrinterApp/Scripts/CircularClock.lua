-- Circular Clock Widget
-- Analog clock face with hour, minute, second hands

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

-- Calculate hand angles (0 degrees = 12 o'clock, clockwise)
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
