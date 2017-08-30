# HDrive
This is the Software repository to handle a Henschel-Robotics "HDrive". Including a C# Driver.

The Driver is able to communicate over TCP with the HDrive. It is also possible to chose in between TCP or UDP for receiving messages from the motor. 
Therefore the right interface in the HDrive GUI has to be set up.

Example:

HDrive myDrive;     //your global hdrive variable

void init(){

  AutoResetEvent resetEvent = new AutoResetEvent(false);              //creat autoreset event
  myDrive = new HDrive(IPAddress.Parse("192.168.1.102"), NewDataFromMotor, 1000, resetEvent, 1001);  //creat drive
  resetEvent-.WaitOne();          //Wait until drive is connected
  
}


void NewDataFromMotor( int motorNumber ) //Hdrive callback is getting trigggered as soon as there is new Data recieved
{

  int x = myDrive.Position;
  
}
