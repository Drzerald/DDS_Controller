#include  <SPI.h>
byte response[4] = { 0b00000111, 0b00000111, 0b00000111, 0b00000111 }; //Responce that will be send to PC after reciving correct messeage
// Pinout description
const int DigPotSSPin = 4;// 
const int DDS_SSPin0 = 8;// DDS SPI Slaveselect
const int DDS_SSPin1 = 7;// DDS SPI Slaveselect
const int DDS_SSPin2 = 6;// DDS SPI Slaveselect
const int DDS_SSPin3 = 5;// DDS SPI Slaveselect
const int Mclk_Pin = 9;// Pin used as clock for DDS
//const int PotReset = A0;//Not Used

uint16_t freqH = 0; //Higher half of FREQ registery of AD9833 - used to set frequency 
uint16_t freqL = 0; //Lower half of FREQ registery 
uint16_t phase0 = 0; //PHASE registery of AD9833 (Ch1)
uint16_t phase1 = 0; //PHASE registery of AD9833 (Ch2)
uint16_t phase2 = 0; //PHASE registery of AD9833 (Ch3)
uint16_t phase3 = 0; //PHASE registery of AD9833 (Ch4)
byte amp0 = 128; // Aplitude; DigiPot position (ch1)
byte amp1 = 128; // Aplitude; DigiPot position (ch2)
byte amp2 = 128; // Aplitude; DigiPot position (ch3)
byte amp3 = 128; // Aplitude; DigiPot position (ch4)

byte readBuffer[18]; // UART read Buffer


void setup() {
  delay(100); // Wait for other elemets on PCB power up
  // put your setup code here, to run once:
  OCR1A = 0;            //Timer compare value
  TCCR1A = 0b01000000;  //Timer T1 Config - Setting timer T1 to toggle MCLK on every clock tick
  TCCR1B = 0b00001000;  //Timer T1 Config
  setMclk(0);           //Turn off MCLK Signal
  //Setting GPIO ports as Outpust inputs:
  pinMode(DigPotSSPin, OUTPUT); 
  pinMode(DDS_SSPin0, OUTPUT);
  pinMode(DDS_SSPin1, OUTPUT);
  pinMode(DDS_SSPin2, OUTPUT);
  pinMode(DDS_SSPin3, OUTPUT);
  pinMode(Mclk_Pin, OUTPUT);
  //Setting SPIs slave select to High:
  digitalWrite(DigPotSSPin,HIGH);
  digitalWrite(DDS_SSPin0,HIGH);
  digitalWrite(DDS_SSPin1,HIGH);
  digitalWrite(DDS_SSPin2,HIGH);
  digitalWrite(DDS_SSPin3,HIGH);
  //digitalWrite(PotReset, HIGH); //Not Used

  //SPI.beginTransaction(Settings(14000000, MSBFIRST, SPI_MODE0));
  SPI.begin();

  delay(1);
  setMclk(1);
  /*AD9833 CONFIG
  *   B28 mode -  Setting frequency requires two consecutive writes into FREQ registery 
                  First one sets 14 LSB bits and the second 14 MSB. 
  *   DDS Reset on
  */
  SpiAllDDSWrite(0b0010000000000000); 
  delay(1);
  SpiAllDDSWrite( 0b0010000100000000); // reset is on
  //Freq
  SpiAllDDSWrite( 0b0100001100010011); // First write to FREQ0 registery  (14 LSB)
  SpiAllDDSWrite(0b0100000000000010);  // Second write to FREQ0 registery (14 MSB) - setting frequency to 1kHz 
  //Setting PhaseOffset:
  SpiDDSWrite(DDS_SSPin0, 0b1100000000000000); //000 Deg  
  SpiDDSWrite(DDS_SSPin1, 0b1100000000000000); //000 Deg
  SpiDDSWrite(DDS_SSPin2, 0b1100100000000000); //180 Deg
  SpiDDSWrite(DDS_SSPin3, 0b1100100000000000); //180 Deg
 
  //Reset off
  setMclk(0);
  delay(1);
  SpiAllDDSWrite(0b0010000000000000);
  //MCLK ON
  delayMicroseconds(200);
  setMclk(1);
  //setting Amplitude:
  SpiDigPotWrite(0,amp0); //Ch 1
  SpiDigPotWrite(1,amp1); //Ch 2
  SpiDigPotWrite(2,amp2); //Ch 3
  SpiDigPotWrite(3,amp3); //Ch 4

  Serial.begin(9600);
}

void loop() {
    
  int readedBytes = 0; // used to count recived bytes by UART
  readedBytes = Serial.readBytes(readBuffer,18); //Waits 1s (timeout) or until 18 bytes are recived
 
  //PC is Reqesting response:
   if(readedBytes == 4){ //Response uses ony 4 bytes (code from older versions)
    if(readBuffer[0] == 0b00000111 && readBuffer[3] == 0b00000111){ //(if fuction bytes (0 and 17) were set to 7)
      ///byte response[4] = { 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
      for(int i =0; i<4; i++){
        Serial.write(response[i]);
      }
    }
  }
  
  //Recived new configuration:
  if(readedBytes == 18){ //If all bytes were recived 
    if(readBuffer[0] == 11 && readBuffer[17] == 11){ // if fuction bytes (0 and 17) were set to 11
      for(int i =0; i<4; i++){      //Write response to PC
        Serial.write(response[i]);
      }
      freqH = 0;
      freqL = 0;
      phase0 = 0;
      phase1 = 0;
      phase2 = 0;
      phase3 = 0;
      //Read Freq form SPI
      freqH |= readBuffer[1]<<8;
      freqH |= readBuffer[2];
      freqL |= readBuffer[3]<<8;
      freqL |= readBuffer[4];
      freqH |= 1<<14; // setting register selection bit
      freqL |= 1<<14;
      //Read Phase From SPI
      phase0 |= readBuffer[5]<<8;
      phase0 |= readBuffer[6];
      phase1 |= readBuffer[7]<<8;
      phase1 |= readBuffer[8];
      phase2 |= readBuffer[9]<<8;
      phase2 |= readBuffer[10];
      phase3 |= readBuffer[11]<<8;
      phase3 |= readBuffer[12];
      //Read Amplitude form SPI
      amp0 =  readBuffer[13];
      amp1 =  readBuffer[14];
      amp2 =  readBuffer[15];
      amp3 =  readBuffer[16];

      //Setting Aplitude, 
      SpiDigPotWrite(0, amp0);
      SpiDigPotWrite(1, amp1); 
      SpiDigPotWrite(2, amp2);
      SpiDigPotWrite(3, amp3); 
      //Stetting Phase, Frequency in DDS 
      DDS_SetFreqPhase(freqH, freqL, phase0, phase1, phase2, phase3);
    }
  }
  delayMicroseconds(100);
}


void setMclk(int mode){ // MODE 1 - MCLK is tuned on; MODE 0 - MCKL is off
  switch(mode){
    case 0:
      TCCR1A = 0b10000000;
      TCCR1B = 0b00001001;
      break;
    case 1:
      TCCR1A = 0b01000000;
      TCCR1B = 0b00001001;
      break;
    default:
      TCCR1B = 0b00001000;
      break;
  }
}


void SpiDigPotWrite(char channel, byte potVal){ // Setting digital potenciometers position of selected channel
  SPI.setDataMode(SPI_MODE0);  //DigiPot uses SPI_MODE0 and DDS SPI_MODE2.
  digitalWrite(DigPotSSPin,LOW); //SS down
  delayMicroseconds(20); // delay for SS to sent down
  SPI.transfer(channel);
  SPI.transfer(potVal);
  delayMicroseconds(10);
  digitalWrite(DigPotSSPin,HIGH); //SS up
  delayMicroseconds(10);
}


void SpiDDSWrite(int SS_Pin, uint16_t data){ // send meesage in 'data' to slected SS pinout of DDS
  SPI.setDataMode(SPI_MODE2); //DigiPot uses SPI_MODE0 and DDS SPI_MODE2.
  digitalWrite(SS_Pin,LOW);   //Set SS low
  delayMicroseconds(20);
  SPI.transfer(highByte(data)); //Transfer first half of messeage
  SPI.transfer(lowByte(data));  //Transfer second half of messeage
  delayMicroseconds(20);
  digitalWrite(SS_Pin,HIGH); //SS high
  delayMicroseconds(20);
}

void SpiAllDDSWrite(uint16_t data){ // send meesage in 'data' to all DDS (works like SpiDDSWrite() except to all)
  SPI.setDataMode(SPI_MODE2);  
  delayMicroseconds(20);
  digitalWrite(DDS_SSPin0,LOW);
  digitalWrite(DDS_SSPin1,LOW);
  digitalWrite(DDS_SSPin2,LOW);
  digitalWrite(DDS_SSPin3,LOW);
  delayMicroseconds(30);
  SPI.transfer(highByte(data));
  SPI.transfer(lowByte(data));
  delayMicroseconds(20);
  digitalWrite(DDS_SSPin0,HIGH);
  digitalWrite(DDS_SSPin1,HIGH);
  digitalWrite(DDS_SSPin2,HIGH);
  digitalWrite(DDS_SSPin3,HIGH);
  delayMicroseconds(20);
}



void DDS_SetFreqPhase(uint16_t MSBFreq, uint16_t LSBFreq, uint16_t Phase0,uint16_t Phase1,uint16_t Phase2,uint16_t Phase3){ //Reconfuture freq and Phase of all AD9833 on board.
  //Config
  SpiAllDDSWrite(0b0010000100000000); // Reset is on
  setMclk(0); //MCLK is off
  //F1 
  SpiAllDDSWrite(LSBFreq); //Setting Frequency - (B28 mode reqires to consecutive writes)
  SpiAllDDSWrite(MSBFreq); //Setting Frequency
  //Setting Phase
  SpiDDSWrite(DDS_SSPin0, Phase0);
  SpiDDSWrite(DDS_SSPin1, Phase1);
  SpiDDSWrite(DDS_SSPin2, Phase2);
  SpiDDSWrite(DDS_SSPin3, Phase3);
  delayMicroseconds(200);
  setMclk(0);
  delayMicroseconds(200);
  SpiAllDDSWrite(0b0010000000000000); //RESET off
  delayMicroseconds(300); 
  setMclk(1); // Mclk on
}
