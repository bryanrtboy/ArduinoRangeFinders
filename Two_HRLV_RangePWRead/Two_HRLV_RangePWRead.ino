/*
Test code for the Arduino controllers
Written by Tom Bonar for testing
Sensors being used for this code are the HR-MaxSonar from MaxBotix
Used to Read the PW input.
*/
const int pwPin1 = 3; //this may be different depending on the Arduino being used, and the other PW pins being used.
const int pwPin2 = 5;
int triggerPin1 = 12; //This pin may not be used.  IT IS RECOMEMEND to operate in free-run mode first!!
int triggerPin2 = 11; //Messages determine which pin to trigger rather than loop
long sensor1, sensor2, cm1, cm2, inches1, inches2;
char receivedChar;
boolean newData = false;

void setup () {
  Serial.begin(57600);
  pinMode(pwPin1, INPUT);
  pinMode(pwPin2, INPUT);
  pinMode(triggerPin1, OUTPUT); //If free-run works properly comment this code out.
  pinMode(triggerPin2, OUTPUT);
}

/*depending on mounting and sensor environment chaining may not be required for these sensors
The recommended mode of operation for these sensors is free-run mode.  It is recommended
to test the sensors in free-run mode before chaining.
If they free-run properly with minimal interference, the void loop delay can be reduced to 133
and the section that says "start_sensor" can be commented out.

For McNichol's installation, check if spacing is far enough
so that we don't need to run the start_sensor and comment out the triggerPin1...
*/
void start_sensor(){
  digitalWrite(triggerPin1,HIGH);
  delay(1);
  digitalWrite(triggerPin1,LOW);
}

void start_sensor2(){
  digitalWrite(triggerPin2,HIGH);
  delay(1);
  digitalWrite(triggerPin2,LOW);
}



/*
the inches and centimeters are provided if you want to convert the range reading
to a different measurement type.
*/
void read_sensor(){
  sensor1 = pulseIn(pwPin1, HIGH);
  cm1 = sensor1/10; // converts the range to cm
  inches1 = cm1/2.54 ;// converts the range to inches
  sensor2 = pulseIn(pwPin2, HIGH);
  cm2 = sensor2/10;// converts the range to cm
  inches2 = cm2/2.54; // converts the range to inches
}
void loop () {

  readOneCharacter();
  showNewData();
  
//  start_sensor();  //Remove this if chaining is not needed.
//  read_sensor();
//  delay(100); //make this match the refresh rate of the sensor or refresh rate x number of sensors in chain.
}

//Listen to the serial port for a single character to be sent,this is a command that is sent from Unity to ask
//the arduino to get a reading. This way, we do not need to create and read from a buffer if we tell the Arduino
//to constantly send data.
//This is also why we do not need to put a delay in our loop, we simply use Unity to constantly ask for either a
//Right 'R' or 'L' reading from the left or right sensor. We could easily scale this without worrying too much about
//interference from our ultrasonic sensors.
void readOneCharacter() {
    if (Serial.available() > 0) {
        receivedChar = Serial.read();
        newData = true;
    }
}

//if we have new data - write it once to the serial port immediately
void showNewData() {
    if (newData == true) {
      newData = false;
      
        if(receivedChar == 'L'){
          start_sensor();
          sensor1 = pulseIn(pwPin1, HIGH)/25.4;
          //Serial.print("L : ");
          Serial.println(sensor1);
        }

        if(receivedChar == 'R'){
          start_sensor2();
          sensor2 = pulseIn(pwPin2, HIGH)/25.4;
          //Serial.print("R : ");
          Serial.println(sensor2);
        }
        
    }
}


