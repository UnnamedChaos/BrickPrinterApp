#ifndef DISPLAY_H
#define DISPLAY_H

#include <Arduino.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C
#define NUM_DISPLAYS 3
#define SDA_PIN_0 10
#define SCL_PIN_0 21
#define SDA_PIN_1 4
#define SCL_PIN_1 5
#define SDA_PIN_2 8
#define SCL_PIN_2 9
#define DISPLAY_BUFFER_SIZE 1024

bool displayInit();
void displayShowMessage(uint8_t screenId, const char* line1, const char* line2 = nullptr,
                        const char* line3 = nullptr, const char* line4 = nullptr,
                        const char* line5 = nullptr, const char* line6 = nullptr);
void displayShowMessageAll(const char* line1, const char* line2 = nullptr,
                           const char* line3 = nullptr, const char* line4 = nullptr,
                           const char* line5 = nullptr, const char* line6 = nullptr);
void displayShowIP(const char* ip);
void displayUpdate(uint8_t screenId, const uint8_t* buffer);
void displayClear(uint8_t screenId);
bool displayIsValidScreen(uint8_t screenId);
bool displayCheckScreen(uint8_t screenId);
uint8_t displayCheckAllScreens();
void displayClearBuffer(uint8_t screenId);
void displayDrawText(uint8_t screenId, int x, int y, const char* text);
void displaySetFontSize(uint8_t screenId, int size);
void displayDrawPixel(uint8_t screenId, int x, int y, bool on);
void displayDrawRect(uint8_t screenId, int x, int y, int w, int h, bool filled);
void displayDrawLine(uint8_t screenId, int x1, int y1, int x2, int y2);
void displayDrawCircle(uint8_t screenId, int x, int y, int r, bool filled);
void displayRefresh(uint8_t screenId);

#endif
