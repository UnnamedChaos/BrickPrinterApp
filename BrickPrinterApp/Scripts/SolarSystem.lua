-- Solar System Widget
-- Top-down view with elliptical orbits

clear()

local cx, cy = 64, 32
local pi = 3.14159
local t = second() + minute() * 60

-- Sun at center
circle(cx, cy, 3, true)

-- Planet data: {semi-major axis, semi-minor axis, orbital speed, size, has_rings}
-- Using ellipses to maximize screen space (128x64)
local planets = {
    {10, 6, 4.0, 1, false},    -- Mercury
    {16, 9, 1.6, 1, false},    -- Venus
    {22, 13, 1.0, 1, false},   -- Earth
    {28, 16, 0.5, 1, false},   -- Mars
    {40, 23, 0.08, 2, false},  -- Jupiter
    {50, 29, 0.03, 2, true},   -- Saturn (with rings)
    {56, 32, 0.01, 1, false},  -- Uranus
    {62, 30, 0.006, 1, false}  -- Neptune
}

-- Function to draw ellipse outline
local function drawEllipse(cx, cy, a, b)
    local prevX, prevY = nil, nil
    for angle = 0, 360, 6 do
        local rad = angle * pi / 180
        local x = math.floor(cx + a * math.cos(rad))
        local y = math.floor(cy + b * math.sin(rad))
        if prevX and x >= 0 and x < 128 and y >= 0 and y < 64 then
            line(prevX, prevY, x, y)
        end
        prevX, prevY = x, y
    end
end

-- Draw orbital paths (ellipses)
for i = 1, #planets do
    local a = planets[i][1]
    local b = planets[i][2]
    drawEllipse(cx, cy, a, b)
end

-- Draw planets
for i = 1, #planets do
    local a = planets[i][1]
    local b = planets[i][2]
    local speed = planets[i][3]
    local size = planets[i][4]
    local has_rings = planets[i][5]

    -- Calculate planet position on ellipse
    local angle = t * speed * pi / 180
    local px = math.floor(cx + a * math.cos(angle))
    local py = math.floor(cy + b * math.sin(angle))

    -- Draw planet
    if px >= 0 and px < 128 and py >= 0 and py < 64 then
        if size == 1 then
            circle(px, py, 1, true)
        else
            circle(px, py, 2, true)
        end

        -- Draw rings for Saturn (top-down view: concentric circles)
        if has_rings then
            circle(px, py, 3, false)
            circle(px, py, 4, false)
        end
    end
end

show()
