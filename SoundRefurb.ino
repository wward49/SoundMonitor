#include <WiFi.h>
#include <WebServer.h>
#include <HTTPClient.h>

//Set up ESP Access Point for Application to connect to
IPAddress ESPIP(192, 168, 4, 1); //This is the static IP I set instead of the default one.
IPAddress gateway(192, 168, 4, 1); //Gateway IP
IPAddress subnet(255, 255, 255, 0); //subnet mask

//SSID and Password for connecting to the ESP32:
const char* ssid = "ESP32WIFI";
const char* password = "esp32password";


String routerSSID = "";
String routerPassword = "";
String applicationIP = "";

bool SSIDReceived = false;
bool passwordReceived = false;
bool localIPReceived = false;

//set webserver to port 80.
WebServer server(80);

// Define the analog pin where the sound sensor is connected
const int soundSensorPin = 34;  // Analog input pin for the sensor (GPIO34)
const int greenLEDPin = 13;

// Variables for reading values
int soundValue = 0;
bool soundTriggered;

void setup() {
  // Start the serial communication
  Serial.begin(115200);
  delay(1000);

  //This section is for establishing the ESP32 as an open access point.
  Serial.print("Setting up Access Point ... ");
  Serial.println(WiFi.softAPConfig(ESPIP, gateway, subnet) ? "Setup Static IP" : "Failed to setup static IP/Gateway!");
  Serial.print("Setting Access Point ... ");
  Serial.println(WiFi.softAP(ssid, password) ? "Ready, ID and Password Set Up!" : "Failed to setup Id/Password!");
  Serial.print("IP address = ");
  Serial.println(WiFi.softAPIP());

  // Set the analog pin to input
  pinMode(soundSensorPin, INPUT);
  pinMode(greenLEDPin, OUTPUT);
  digitalWrite(greenLEDPin, LOW);

  //Define http req. Handler and start server
  server.on("/", HTTP_GET, handleConnection);
  server.on("/sendSSID", HTTP_POST, handleSSID);
  server.on("/sendpassword", HTTP_POST, handlePassword);
  server.on("/sendlocalIP", HTTP_POST, handleLocalIP);
  server.begin();

}

void DetectSound(){

  if(soundValue != 0){
    soundTriggered = true;
    digitalWrite(greenLEDPin, HIGH);
    Serial.println("Sound Triggered on board!");
    SoundDetectedToApp();
    
    delay(1000);
    digitalWrite(greenLEDPin, LOW);
    Serial.println("Waiting for Sound...");
  }
}

void loop() {
  server.handleClient();

  soundValue = digitalRead(soundSensorPin); //reads digital pin from sensor...the input
  DetectSound();
}

void handleConnection(){
  server.send(200, "text/plain", "ESP32 in Online!");
}

void handleSSID(){//this is called when the Application post to the root. goes with the server.on("/", HTTP_GET, handlePost); code. Can change directory for organization.

  if(server.hasArg("plain")){ 
    routerSSID = server.arg("plain"); //get the data sent by the application
    Serial.print("Recieved SSID data from WPF APP: ");
    SSIDReceived = true;
    Serial.println(routerSSID); //print recieved data
    server.send(200, "text/plain", "Message recieved: " + routerSSID);
  }
  else{
    server.send(400, "text/plain", "No message received");
  }

}
void handlePassword(){
  if(server.hasArg("plain")){ 
    routerPassword = server.arg("plain"); //get the data sent by the application
    Serial.print("Recieved password data from WPF APP: ");
    passwordReceived = true;
    Serial.println(routerPassword); //print recieved data
    server.send(200, "text/plain", "Message recieved: " + routerPassword);
  }
  else{
    server.send(400, "text/plain", "No message received");
  }
}

void handleLocalIP(){
  Serial.println("GOT INTO HANDLELOCALIP!");
  if(server.hasArg("plain")){ 
    applicationIP = server.arg("plain"); //get the data sent by the application
    Serial.print("Recieved IP data from WPF APP: ");
    localIPReceived = true;
    Serial.println(applicationIP); //print recieved data
    server.send(200, "text/plain", "Message recieved: " + applicationIP);
    checkAndConnect();
  }
  else{
    Serial.println("REACHED ELSE OF HANDLELOCALIP");
    server.send(400, "text/plain", "No message received");
  }
}

void checkAndConnect() {
  if (SSIDReceived && passwordReceived && localIPReceived) {
    connectToRouter();
  }
}

void connectToRouter() {
  WiFi.softAPdisconnect(true); // Turn off Access Point mode
  delay(1000);

  WiFi.begin(routerSSID.c_str(), routerPassword.c_str()); // Connect to router

  Serial.print("Connecting to ");
  Serial.print(routerSSID);

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    delay(500);
    Serial.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nJoined New Network!");
    Serial.print("IP Address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("\nFailed to join router.");
  }
}

void SoundDetectedToApp(){
  HTTPClient http;
  String url = "http://" + applicationIP + ":8080/detection/";
  http.begin(url);
  http.addHeader("Content-Type", "text/plain");
  int httpResponseCode = http.GET();

  Serial.print("Response Code sent back from WPF: ");
  Serial.println(httpResponseCode);

  if(httpResponseCode > 0){
    Serial.println("GET request sent successfully!");
  }
  else{
    Serial.println("Error sending GET request.");
  }
  http.end();
}

