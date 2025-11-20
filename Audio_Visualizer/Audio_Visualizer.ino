#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 32
#define OLED_RESET -1

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// Налаштування аквалайзера
const int bands = 16;
const int bandWidth = 7;
const int gap = 1;
const int maxHeight = 28;

// Буфери для анімації
float currentHeights[16] = {0};
float targetHeights[16] = {0};

void setup() {
  Serial.begin(115200);
  
  // Ініціалізація OLED
  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
    Serial.println(F("SSD1306 allocation failed"));
    for(;;);
  }
  
  display.clearDisplay();
  display.setTextColor(SSD1306_WHITE);
  
  Serial.println("READY"); // Сигнал для ПК, що ESP готова
}

void loop() {
  // Читаємо дані з Serial
  readSerialData();
  
  // Анімуємо смуги
  animateBars();
  
  // Малюємо аквалайзер
  drawEqualizer();
  
  delay(20);
}

void readSerialData() {
  if (Serial.available() > 0) {
    String data = Serial.readStringUntil('\n');
    data.trim();
    
    // Очікуємо дані у форматі: v1,v2,v3,...,v16
    if (data.length() > 0) {
      int startIndex = 0;
      int bandIndex = 0;
      
      for (int i = 0; i < data.length() && bandIndex < bands; i++) {
        if (data.charAt(i) == ',' || i == data.length() - 1) {
          String valueStr = data.substring(startIndex, i + (i == data.length() - 1 ? 1 : 0));
          int value = valueStr.toInt();
          
          // Обмежуємо значення та мапуємо на висоту
          value = constrain(value, 0, 100);
          targetHeights[bandIndex] = map(value, 0, 100, 0, maxHeight);
          
          startIndex = i + 1;
          bandIndex++;
        }
      }
    }
  }
}

void animateBars() {
  // Плавна анімація смуг
  for(int i = 0; i < bands; i++) {
    if(currentHeights[i] < targetHeights[i]) {
      currentHeights[i] += (targetHeights[i] - currentHeights[i]) * 0.3;
    } else {
      currentHeights[i] *= 0.8; // Плавне падіння
      if(currentHeights[i] < 1) currentHeights[i] = 0;
    }
  }
}

void drawEqualizer() {
  display.clearDisplay();
  
  // Малюємо смуги
  for(int i = 0; i < bands; i++) {
    int x = i * (bandWidth + gap);
    int height = (int)currentHeights[i];
    
    if(height > 0) {
      // Градієнт - нижні частини яскравіші
      display.fillRect(x, SCREEN_HEIGHT - height, bandWidth, height, SSD1306_WHITE);
    }
    
    // Лінія основи
    display.drawFastHLine(x, SCREEN_HEIGHT - 1, bandWidth, SSD1306_WHITE);
  }
  
  display.display();
}