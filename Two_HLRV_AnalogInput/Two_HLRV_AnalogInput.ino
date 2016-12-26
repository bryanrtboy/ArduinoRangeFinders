/*
This code reads the Analog Voltage output from the 5 meter
HR-MaxSonar sensors and converts it to mm and inches
If you wish for code with averaging, please see
playground.arduino.cc/Main/MaxSonar
*/

const int anPin1 = 0;
const int anPin2 = 1;

int triggerPin1 = 1;
long distance1, distance2;

char receivedChar;
boolean newData = false;

void setup() {
  Serial.begin(19200);  // sets the serial port to 9600
  Serial.println("<Arduino is ready>");
  pinMode(triggerPin1, OUTPUT);
}


void loop() {
    //Read the serial ports, looking for a message
    ReadOneCharacter();
    ShowNewData();

//  Original Code to constantly read
//  start_sensor();
//  read_sensors();
//  print_all();
//  delay(133);

}


/*depending on mounting and sensor environment chaining may not be required for these sensors
The recommended mode of operation for these sensors is free-run mode.  It is recommended
to test the sensors in free-run mode before chaining.
If they free-run properly with minimal interference, the void loop delay can be reduced to 133
and the section that says "start_sensor" can be commented out.*/
void start_sensor(){
  digitalWrite(triggerPin1,HIGH);
  delay(1);
  digitalWrite(triggerPin1,LOW);
}

void read_sensors(){
  /* 
  The Arduinoâ€™s analog-to-digital converter (ADC) has a range of 1024,
  which means each bit is ~4.9mV.
  Each bit is equal to 5mm so it needs to be multiplied by 5
  */
  distance1 = (analogRead(anPin1)*5)/25.4;
  distance2 = (analogRead(anPin2)*5)/25.4;
}

void print_all(){
  Serial.print("S1= ");
  Serial.print(distance1);
  Serial.print("in : ");
  Serial.print(" S2= ");
  Serial.print(distance2);
  Serial.print("in");
  Serial.println();
}

void ReadOneCharacter() {
    if (Serial.available() > 0) {
        receivedChar = Serial.read();
        newData = true;
    }
}

void ShowNewData() {
    if (newData == true) {
      newData = false;
        if(receivedChar == 'L'){
                start_sensor();
                read_sensors(); //only reading sensors when we need it
                Serial.print(distance1);
                Serial.print(" ");
                Serial.println(distance2);
                //print_all();
        }
    
        if(receivedChar == 'R'){
                Serial.println(distance2);
        }
        
    }
}


