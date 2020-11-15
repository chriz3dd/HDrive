
# HDrive

This is the software repository to handle a Henschel-Robotics "HDrive". 

The driver is able to communicate over TCP or UDP with the HDrive. In any case the drive only accept drive commands over TCP.  It is also possible to activate UDP protocol for receiving messages from the motor. Therefore the right interface in the HDrive GUI has to be set up.  

**Example:**
```C#

// Global HDrive variable
// id: any number to identify your drive if you have multiple
int id = 0;
HDrive myDrive = new HDrive(id, IPAddress.Parse("192.168.1.102")); 

void init()
{
	// Connecto to HDrive
	myDrive.Connect(NewDataFromMotorCallback, 1000); 
}

// HDrive callback is getting trigggered as soon as there is new Data recieved
void NewDataFromMotorCallback( int motorNumber )
{
	int x = myDrive.Position;
}
```
If the motor is configured to use UDP to send it's data, then the following initialization can be used:
```C#
int id = 0;
// Specifie the telegramm what you also have configured in the WebGUI
HDriveTicket telegram = HdriveTicket.BinaryTicket; 

// The UDP-Port can be setup in the WebGUI
int UDPPort = 1001;  

HDrive myDrive = new HDrive(id, IPAddress.Parse("192.168.1.102"), telegram , UDPPort); 

```