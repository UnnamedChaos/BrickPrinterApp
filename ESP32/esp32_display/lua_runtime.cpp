#include "lua_runtime.h"
#include "display.h"
#include <time.h>
#include <LuaWrapper.h>

// Single shared Lua instance to save memory
static LuaWrapper* sharedLua = nullptr;

// Script state per screen (no Lua instance per screen)
struct LuaScreen {
    String script;
    bool active;
    unsigned long lastRun;
    unsigned long interval;
};

static LuaScreen screens[NUM_DISPLAYS];
static char lastError[128] = "";
static uint8_t currentScreen = 0;

// Lua bindings - flat function names since we can't create tables
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

// Time functions
static int lua_time_hour(lua_State* L) {
    time_t now = time(nullptr);
    struct tm* tm = localtime(&now);
    lua_pushinteger(L, tm ? tm->tm_hour : 0);
    return 1;
}

static int lua_time_minute(lua_State* L) {
    time_t now = time(nullptr);
    struct tm* tm = localtime(&now);
    lua_pushinteger(L, tm ? tm->tm_min : 0);
    return 1;
}

static int lua_time_second(lua_State* L) {
    time_t now = time(nullptr);
    struct tm* tm = localtime(&now);
    lua_pushinteger(L, tm ? tm->tm_sec : 0);
    return 1;
}

static int lua_time_date(lua_State* L) {
    time_t now = time(nullptr);
    struct tm* tm = localtime(&now);
    if (tm) {
        char buf[16];
        snprintf(buf, sizeof(buf), "%02d.%02d.%04d",
                 tm->tm_mday, tm->tm_mon + 1, tm->tm_year + 1900);
        lua_pushstring(L, buf);
    } else {
        lua_pushstring(L, "00.00.0000");
    }
    return 1;
}

static void registerCustomFunctions(LuaWrapper* lua) {
    // Display functions
    lua->Lua_register("clear", lua_display_clear);
    lua->Lua_register("text", lua_display_text);
    lua->Lua_register("setFont", lua_display_setFont);
    lua->Lua_register("pixel", lua_display_pixel);
    lua->Lua_register("rect", lua_display_rect);
    lua->Lua_register("line", lua_display_line);
    lua->Lua_register("circle", lua_display_circle);
    lua->Lua_register("show", lua_display_show);

    // Time functions
    lua->Lua_register("hour", lua_time_hour);
    lua->Lua_register("minute", lua_time_minute);
    lua->Lua_register("second", lua_time_second);
    lua->Lua_register("date", lua_time_date);
}

static bool ensureLuaInitialized() {
    if (sharedLua != nullptr) return true;

    sharedLua = new LuaWrapper();
    if (!sharedLua) {
        snprintf(lastError, sizeof(lastError), "Failed to create Lua instance");
        return false;
    }

    registerCustomFunctions(sharedLua);
    Serial.println("Shared Lua instance created");
    return true;
}

void luaInit() {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        screens[i].script = "";
        screens[i].active = false;
        screens[i].lastRun = 0;
        screens[i].interval = 1000;
    }
    Serial.println("Lua runtime initialized");
}

bool luaLoadScript(uint8_t screenId, const char* script) {
    if (screenId >= NUM_DISPLAYS) {
        snprintf(lastError, sizeof(lastError), "Invalid screen ID: %d", screenId);
        return false;
    }

    // Stop any existing script on this screen
    luaStopScript(screenId);

    // Initialize shared Lua if needed
    if (!ensureLuaInitialized()) {
        return false;
    }

    // Store script
    screens[screenId].script = String(script);

    // Run script once
    currentScreen = screenId;
    String result = sharedLua->Lua_dostring(&screens[screenId].script);

    Serial.printf("Lua dostring result: '%s'\n", result.c_str());

    if (result.length() > 0 && result != "OK") {
        snprintf(lastError, sizeof(lastError), "Lua error: %.100s", result.c_str());
        Serial.printf("Lua error on screen %d: %s\n", screenId, lastError);
        screens[screenId].script = "";
        return false;
    }

    screens[screenId].active = true;
    screens[screenId].lastRun = millis();

    Serial.printf("Lua script loaded on screen %d\n", screenId);
    return true;
}

void luaStopScript(uint8_t screenId) {
    if (screenId >= NUM_DISPLAYS) return;

    screens[screenId].script = "";
    screens[screenId].active = false;

    displayClear(screenId);
    Serial.printf("Lua script stopped on screen %d\n", screenId);
}

bool luaHasScript(uint8_t screenId) {
    if (screenId >= NUM_DISPLAYS) return false;
    return screens[screenId].active;
}

void luaTick() {
    if (!sharedLua) return;

    unsigned long now = millis();

    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (!screens[i].active) continue;

        if (now - screens[i].lastRun < screens[i].interval) continue;

        screens[i].lastRun = now;
        currentScreen = i;

        // Re-run the script
        String result = sharedLua->Lua_dostring(&screens[i].script);
        if (result.length() > 0 && result != "OK") {
            Serial.printf("Lua tick error on screen %d: '%s'\n", i, result.c_str());
        }
    }
}

const char* luaGetLastError() {
    return lastError;
}
