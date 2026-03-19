-- Digital Clock Widget
-- Runs on ESP32, updates every second

clear()

-- Get current time
local h = hour()
local m = minute()
local s = second()

-- Format time string
local timeStr = string.format("%02d:%02d:%02d", h, m, s)

-- Draw centered clock
setFont(2)
text(16, 20, timeStr)

-- Draw date below
setFont(1)
text(20, 50, date())

show()
