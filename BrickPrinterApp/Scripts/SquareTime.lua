-- Square Time Widget
-- Displays time using geometric square patterns

clear()

-- Get current time
local h = hour()
local m = minute()
local s = second()

-- Convert to 12-hour format
local h12 = h % 12
if h12 == 0 then h12 = 12 end

-- Draw corner squares that pulse with seconds
local pulseSize = 3 + math.floor(math.abs(math.sin(s * 3.14159 / 30)) * 3)
rect(0, 0, pulseSize, pulseSize, true)
rect(128 - pulseSize, 0, pulseSize, pulseSize, true)
rect(0, 64 - pulseSize, pulseSize, pulseSize, true)
rect(128 - pulseSize, 64 - pulseSize, pulseSize, pulseSize, true)

-- Draw expanded main square frame
rect(6, 4, 116, 56, false)
rect(8, 6, 112, 52, false)

-- Draw time text in upper center
setFont(2)
local timeStr = string.format("%02d:%02d", h, m)
text(35, 14, timeStr)

-- Draw separator line between time and seconds bar
line(12, 32, 116, 32)
line(12, 33, 116, 33)

-- Draw hour representation with vertical bars (compact, top section)
local hourBarWidth = 6
local hourBarSpacing = 1
for i = 0, h12 - 1 do
    local x = 14 + (i % 6) * (hourBarWidth + hourBarSpacing)
    local y = 40 + math.floor(i / 6) * 8
    rect(x, y, hourBarWidth, 6, true)
end

-- Draw second indicator as progress bar inside square (bottom section)
local barX = 12
local barY = 50
local barWidth = 104
local barHeight = 6

-- Draw progress bar outline
rect(barX, barY, barWidth, barHeight, false)

-- Fill progress based on seconds (0-60)
local fillWidth = math.floor((s / 60) * (barWidth - 2))
if fillWidth > 0 then
    rect(barX + 1, barY + 1, fillWidth, barHeight - 2, true)
end

show()
