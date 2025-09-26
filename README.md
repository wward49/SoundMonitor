### BABY MONITOR
This device was an idea thrown at me from my brother-in-law who is an IT guy. He wanted to make a baby monitor that alerts you to your baby crying if you are gaming. I thought it would be a cool thing to try and design and get working, just to see if I could get something resembling a baby monitor working. It was nice getting more exprience with embedded systems...the previous project I had worked on was a wireless thermometer with the esp32, arduino, LED screen, and Weather API's. 
## Process
I started by setting up the componenets on a breadboard I bought. I had to find a pinout diagram for the particular microcontroller I had online...and there are several esp32's with little differences on where the analog and dialog pins are and the out pins and ground...so that was fun. Once I got the microphone and LED and battery pack wired, I wrote a simple program that just lit up the LED when the sound was detected. It didn't work. The theme of this project is that nothing ever works the first time. Or Second. I found out there is a thing called a potentiometer that you need to adjest with a screwdriver in order to get it to pick up sound. So I tried tuning it some. And then it still didn't work. I'll skip to the solution...the microphone was broken. I bought a new one and boom...good to go...we got sound being picked up (usually claps or whistles) and an LED that lights up. The next step was to send that bool trigger to a Desktop Application instead of an LED. 
<p>
  <img src="https://github.com/wward49/SoundMonitor/blob/main/20250926_143256.jpg" width="300">
  <img src="https://github.com/wward49/SoundMonitor/blob/main/20250926_143315.jpg" width="300">
</p>

I created a WPF Application with C# in Visual Studio. The UI was super quick and simple...nothing fancy at all. This part was not to hard...the logic was pretty simple...just wait for the trigger from the ESP and when it gets it...do soemthing (Display a Message in this case). The problem was getting the ESP32 and the Application on the same network.
<p>
  <img src="https://github.com/wward49/SoundMonitor/blob/main/Screenshot%202025-09-26%20160607.png" width="300">
</p>
At first I just hard coded my router's information into the ESP32. That worked well enough. But I didnt want to use the device only at my house. I wanted to be able to take it ANYWHERE and have it hop onto the same network the Application was running on. Because of this I had to have a way to dynamically get the username and Password of the network the Application was on, and pass it to the ESP.  The ESP32 can act as a host for a LAN and accept clients, so the plan was to get a username and password string, connect over to the esp's hosted network, drop off the strings, then connect both the application computer and the device back to the original network.

Getting this set up was a pain in the butt. I had to debug all sorts of timeout issues, permission issues, firewall issues, and more. And I am by no means a master at any of this, so a lot of it was learning on the fly. Finally I got it to work though. You can run the application on any(more like most home networks...not any) network, turn on the device, then connect the two together on that same network you started on. I'll admit there are a lot of bugs with it. There should be many more catches for issues that come up during connection, and you can certainly run into issues if you do not do it correctly. But it works.

## What I Learned
Use Bluetooth. About half way through this project I came to realize there was a much more simple way to get all this done. It is the reason almost all wireless devices for a PC come with a bluetooth adaptor to put into a USB port of your Computer. If the device has a keypad or some way to pass a username and password, its not a big deal. But I had a board with a microphone on it. The ESP32 I had could send out short range bluetooth signals and it would have been much better to use that. If I hadn't already been so dedicated to making the WiFi work, I would 100% have just used bluetooth. I would have been able to skip probably 70% of the annoyances I dealt with during this project. 

I learned a lot about RESTful APIs, Server Success and Error codes, GET and POST, transmitting data across devices, asynchronous functions and more.




# Final Thoughts
I am super proud of myself for getting this past the finish line. It could be way better...but I finally got it to work. 

Could I do it again from scratch without notes or looking anything up? Absolutely not. But I know the steps now. I could put it together much more quickly and with clearer, more concise code. 
