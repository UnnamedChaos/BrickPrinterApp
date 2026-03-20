-- Cyberpunk Space-Time Clock
clear()
local cx, cy = 64, 32
local h, m, s = hour(), minute(), second()
local pi = 3.14159
local t = s + m * 60

-- Rotating hex frame with breathing radius
local rot = t * 0.08
for i = 0, 5 do
    local a1 = rot + i * pi / 3
    local a2 = rot + (i + 1) * pi / 3
    local r = 48 + math.floor(3 * math.sin(t * 0.4 + i))
    local x1 = math.floor(cx + r * math.cos(a1))
    local y1 = math.floor(cy + r * 0.55 * math.sin(a1))
    local x2 = math.floor(cx + r * math.cos(a2))
    local y2 = math.floor(cy + r * 0.55 * math.sin(a2))
    line(x1, y1, x2, y2)
end

-- Inner pulsing dotted ring
local pr = 32 + math.floor(3 * math.sin(t * 0.6))
for i = 0, 19 do
    local a = i * pi / 10
    local px = math.floor(cx + pr * math.cos(a))
    local py = math.floor(cy + pr * 0.55 * math.sin(a))
    pixel(px, py, true)
end

-- Three orbiting particles (seconds)
for j = 0, 2 do
    local orb = (s * 6 - 90 + j * 120) * pi / 180
    local od = 38 + j * 3
    local ox = math.floor(cx + od * math.cos(orb))
    local oy = math.floor(cy + od * 0.55 * math.sin(orb))
    if j == 0 then
        rect(ox - 2, oy - 2, 5, 5, true)
    else
        rect(ox - 1, oy - 1, 3, 3, false)
    end
end

-- Digital time - bigger font
setFont(2)
local timeStr = string.format("%02d:%02d", h, m)
local gx = 0
if s % 5 == 0 then gx = (s % 5) - 2 end
text(28 + gx, 24, timeStr)
setFont(1)

-- Seconds progress arc
for i = 0, s do
    local a = (i * 6 - 90) * pi / 180
    local px = math.floor(cx + 50 * math.cos(a))
    local py = math.floor(cy + 28 * math.sin(a))
    pixel(px, py, true)
end

-- Data streams left side
for i = 0, 2 do
    local col = 4 + i * 5
    local fall = (t * (8 + i * 2) + i * 20) % 64
    for k = 0, 4 do
        local yy = math.floor(fall + k)
        if yy < 64 then pixel(col, yy, true) end
    end
end

-- Data streams right side
for i = 0, 2 do
    local col = 124 - i * 5
    local fall = (t * (9 + i * 2) + i * 15) % 64
    for k = 0, 4 do
        local yy = math.floor(fall + k)
        if yy < 64 then pixel(col, yy, true) end
    end
end

-- Corner brackets
line(1, 1, 12, 1)
line(1, 1, 1, 10)
line(116, 1, 127, 1)
line(127, 1, 127, 10)
line(1, 54, 1, 63)
line(1, 63, 12, 63)
line(127, 54, 127, 63)
line(116, 63, 127, 63)

-- Blinking pulse core
if s % 2 == 0 then
    rect(61, 50, 6, 6, true)
end

show()
