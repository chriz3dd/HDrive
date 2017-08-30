# HDrive
This is the Software repository to handle a Henschel-Robotics "HDrive". Including a C# Driver.

The Driver is able to communicate over TCP with the HDrive. It is also possible to chose in between TCP or UDP for receiving messages from the motor. 
Therefore the right interface in the HDrive GUI has to be set up.

Example:

//add using directive
using hdrive;

//your global hdrive variable
HDrive myDrive;

void init()
{
  //creat autoreset event
  AutoResetEvent resetEvent = new AutoResetEvent(false);
  
  //creat drive
  myDrive = new HDrive(IPAddress.Parse("192.168.1.102"), NewDataFromMotor, 1000, resetEvent, 1001);
  
  //Wait until drive is connected
  resetEvent-.WaitOne();
}


void NewDataFromMotor( int motorNumber )
{
  int x = myDrive.Position;
}
