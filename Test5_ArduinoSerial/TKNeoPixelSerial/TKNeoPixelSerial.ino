// NeoPixel Ring simple sketch (c) 2013 Shae Erisson
// released under the GPLv3 license to match the rest of the AdaFruit NeoPixel library

#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
  #include <avr/power.h>
#endif

// Which pin on the Arduino is connected to the NeoPixels?
// On a Trinket or Gemma we suggest changing this to 1
#define PIN            9

// How many NeoPixels are attached to the Arduino?
#define NUMPIXELS      5

// When we setup the NeoPixel library, we tell it how many pixels, and which pin to use to send signals.
// Note that for older NeoPixel strips you might need to change the third parameter--see the strandtest
// example for more information on possible values.
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, PIN, NEO_GRB + NEO_KHZ800);

int delayval = 200; // delay for half a second

void setup() {
  // This is for Trinket 5V 16MHz, you can remove these three lines if you are not using a Trinket
#if defined (__AVR_ATtiny85__)
  if (F_CPU == 16000000) clock_prescale_set(clock_div_1);
#endif
  // End of trinket special code

  pixels.begin(); // This initializes the NeoPixel library.
  Serial.begin(9600);
}
int r=0,g=0,b=0;
int enable = 0 ;
byte tmp;
void loop() {
  if (Serial.available()>0) {
    tmp = Serial.read();
    if (tmp=='E') enable = 1; else enable = 0;
  }
  if (enable!=0) {
    for(int i=0;i<NUMPIXELS;i++){
      r=random(255);
      g=random(255);
      b=random(255);
  
      pixels.setPixelColor(i, pixels.Color(r,g,b)); // Moderately bright green color.
  
      pixels.show(); // This sends the updated pixel color to the hardware.
  
  
    }
  } else {
    for(int i=0;i<NUMPIXELS;i++){
      pixels.setPixelColor(i, pixels.Color(0,0,0)); // Moderately bright green color.
      pixels.show(); // This sends the updated pixel color to the hardware.
    }
  }
  delay(delayval); // Delay for a period of time (in milliseconds).
}
