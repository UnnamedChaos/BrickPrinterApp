#include "lua_runtime.h"
#include "display.h"
#include "config.h"
#include <time.h>
#include <LuaWrapper.h>

static LuaWrapper* sharedLua = nullptr;

struct LuaScreen {
    String script;
    bool active;
    unsigned long lastRun;
    unsigned long interval;
};

struct LuaScriptQueue {
    uint8_t screenId;
    String script;
    bool pending;
};

static LuaScreen screens[MAX_DISPLAYS];
static LuaScriptQueue scriptQueue[MAX_DISPLAYS];
static char lastError[128] = "";
static uint8_t currentScreen = 0;
static volatile bool luaVMBusy = false;
static volatile bool queueProcessing = false;
static unsigned long lastQueueProcessTime = 0;

static int lua_display_clear(lua_State* L) {
    displayClearBuffer(currentScreen);
    return 0;
}

static int lua_display_text(lua_State* L) {
    int x = luaL_checkinteger(L, 1);
    int y = luaL_checkinteger(L, 2);
    const char* text = luaL_checkstring(L, 3);
    displayDrawText(currentScreen, x, y, text);
    return 0;
}

static int lua_display_setFont(lua_State* L) {
    int size = luaL_checkinteger(L, 1);
    displaySetFontSize(currentScreen, size);
    return 0;
}

static int lua_display_pixel(lua_State* L) {
    int x = luaL_checkinteger(L, 1);
    int y = luaL_checkinteger(L, 2);
    bool on = lua_toboolean(L, 3);
    displayDrawPixel(currentScreen, x, y, on);
    return 0;
}

static int lua_display_rect(lua_State* L) {
    int x = luaL_checkinteger(L, 1);
    int y = luaL_checkinteger(L, 2);
    int w = luaL_checkinteger(L, 3);
    int h = luaL_checkinteger(L, 4);
    bool filled = lua_toboolean(L, 5);
    displayDrawRect(currentScreen, x, y, w, h, filled);
    return 0;
}

static int lua_display_line(lua_State* L) {
    int x1 = luaL_checkinteger(L, 1);
    int y1 = luaL_checkinteger(L, 2);
    int x2 = luaL_checkinteger(L, 3);
    int y2 = luaL_checkinteger(L, 4);
    displayDrawLine(currentScreen, x1, y1, x2, y2);
    return 0;
}

static int lua_display_circle(lua_State* L) {
    int x = luaL_checkinteger(L, 1);
    int y = luaL_checkinteger(L, 2);
    int r = luaL_checkinteger(L, 3);
    bool filled = lua_toboolean(L, 4);
    displayDrawCircle(currentScreen, x, y, r, filled);
    return 0;
}

static int lua_display_show(lua_State* L) {
    displayRefresh(currentScreen);
    return 0;
}

static struct tm* getTime() {
    time_t now = time(nullptr);
    return localtime(&now);
}

static int lua_time_hour(lua_State* L) {
    struct tm* t = getTime();
    lua_pushinteger(L, t ? t->tm_hour : 0);
    return 1;
}

static int lua_time_minute(lua_State* L) {
    struct tm* t = getTime();
    lua_pushinteger(L, t ? t->tm_min : 0);
    return 1;
}

static int lua_time_second(lua_State* L) {
    struct tm* t = getTime();
    lua_pushinteger(L, t ? t->tm_sec : 0);
    return 1;
}

static int lua_time_date(lua_State* L) {
    struct tm* t = getTime();
    if (t) {
        char buf[12];
        snprintf(buf, sizeof(buf), "%02d.%02d.%04d", t->tm_mday, t->tm_mon + 1, t->tm_year + 1900);
        lua_pushstring(L, buf);
    } else {
        lua_pushstring(L, "");
    }
    return 1;
}

static void registerCustomFunctions(LuaWrapper* lua) {
    lua->Lua_register("clear", lua_display_clear);
    lua->Lua_register("text", lua_display_text);
    lua->Lua_register("setFont", lua_display_setFont);
    lua->Lua_register("pixel", lua_display_pixel);
    lua->Lua_register("rect", lua_display_rect);
    lua->Lua_register("line", lua_display_line);
    lua->Lua_register("circle", lua_display_circle);
    lua->Lua_register("show", lua_display_show);
    lua->Lua_register("hour", lua_time_hour);
    lua->Lua_register("minute", lua_time_minute);
    lua->Lua_register("second", lua_time_second);
    lua->Lua_register("date", lua_time_date);
}

static void destroyLua() {
    if (sharedLua) {
        delete sharedLua;
        sharedLua = nullptr;
    }
}

static bool createLua() {
    sharedLua = new LuaWrapper();
    if (!sharedLua) return false;
    registerCustomFunctions(sharedLua);
    return true;
}

static bool ensureLuaInitialized(bool forceRecreate = false) {
    if (forceRecreate && sharedLua != nullptr) {
        destroyLua();
    }

    if (sharedLua != nullptr) return true;
    return createLua();
}

void luaInit() {
    for (uint8_t i = 0; i < MAX_DISPLAYS; i++) {
        screens[i].script = "";
        screens[i].active = false;
        screens[i].lastRun = 0;
        screens[i].interval = 1000;
        scriptQueue[i].pending = false;
        scriptQueue[i].script = "";
    }
}

void luaQueueScript(uint8_t screenId, const char* script) {
    if (screenId >= MAX_DISPLAYS) return;
    scriptQueue[screenId].screenId = screenId;
    scriptQueue[screenId].script = String(script);
    scriptQueue[screenId].pending = true;
}

bool luaIsQueueProcessing() {
    return queueProcessing;
}

bool luaLoadScript(uint8_t screenId, const char* script) {
    if (screenId >= MAX_DISPLAYS) return false;

    // Wait for any ongoing Lua execution to complete
    while (luaVMBusy) {
        yield();
        delay(1);
    }

    luaVMBusy = true;

    screens[screenId].script = "";
    screens[screenId].active = false;
    displayClear(screenId);

    // Simple approach: only create Lua VM if it doesn't exist
    // Don't recreate it - this avoids the expensive re-execution of all scripts
    if (!ensureLuaInitialized(false)) {
        luaVMBusy = false;
        return false;
    }

    screens[screenId].script = String(script);
    currentScreen = screenId;
    yield(); // Yield before executing the new script
    String result = sharedLua->Lua_dostring(&screens[screenId].script);
    yield(); // Yield after execution

    if (result.length() > 0 && result != "OK") {
        snprintf(lastError, sizeof(lastError), "%.120s", result.c_str());
        screens[screenId].script = "";
        luaVMBusy = false;
        return false;
    }

    screens[screenId].active = true;
    screens[screenId].lastRun = millis();
    luaVMBusy = false;
    return true;
}

void luaStopScript(uint8_t screenId, bool clearDisplay) {
    if (screenId >= MAX_DISPLAYS) return;

    bool wasActive = screens[screenId].active;
    screens[screenId].script = "";
    screens[screenId].active = false;

    if (clearDisplay) displayClear(screenId);

    if (wasActive) {
        bool anyActive = false;
        for (uint8_t i = 0; i < MAX_DISPLAYS; i++) {
            if (screens[i].active) { anyActive = true; break; }
        }
        if (!anyActive) {
            // Wait for any ongoing Lua execution to complete
            while (luaVMBusy) {
                yield();
                delay(1);
            }
            luaVMBusy = true;
            yield(); // Allow watchdog to reset before destroying VM
            destroyLua();
            yield(); // Allow watchdog to reset after destroying VM
            luaVMBusy = false;
        }
    }
}

bool luaHasScript(uint8_t screenId) {
    if (screenId >= MAX_DISPLAYS) return false;
    return screens[screenId].active;
}

void luaSetInterval(uint8_t screenId, unsigned long interval) {
    if (screenId >= MAX_DISPLAYS) return;
    screens[screenId].interval = interval < 50 ? 50 : interval;
}

void luaTick() {
    if (luaVMBusy) return;

    unsigned long now = millis();

    // Process queued script loads first (one per tick with rate limiting)
    if (now - lastQueueProcessTime >= 200) { // Wait at least 200ms between queue processing
        for (uint8_t i = 0; i < MAX_DISPLAYS; i++) {
            if (scriptQueue[i].pending) {
                queueProcessing = true;
                lastQueueProcessTime = now;

                // Use heap allocation to avoid stack overflow
                const char* scriptPtr = scriptQueue[i].script.c_str();
                scriptQueue[i].pending = false;

                // Load the script (this may take time but we're in main loop)
                luaLoadScript(i, scriptPtr);

                // Clear the string after loading
                scriptQueue[i].script = "";
                queueProcessing = false;
                return; // Only process one queued script per tick
            }
        }
    }

    if (!sharedLua) return;

    // Run active scripts on their intervals
    for (uint8_t i = 0; i < MAX_DISPLAYS; i++) {
        if (!screens[i].active || now - screens[i].lastRun < screens[i].interval) continue;
        screens[i].lastRun = now;
        currentScreen = i;
        sharedLua->Lua_dostring(&screens[i].script);
        yield();
    }
}

const char* luaGetLastError() {
    return lastError;
}
