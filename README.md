# HDrive
This is the Software repository to handle a Henschel-Robotics "HDrive". Including a C# Driver.

The Driver is able to communicate over TCP with the HDrive. It is also possible to chose in between TCP or UDP for receiving messages from the motor. 
Therefore the right interface in the HDrive GUI has to be set up.

Example:

HDrive myDrive = new HDrive(0, IPAddress.Parse("192.168.1.102"));     //your global hdrive variable

void init(){

  myDrive.Connect(NewDataFromMotorCallback, 1000);  // Connecto to HDrive
}


void NewDataFromMotorCallback( int motorNumber ) // HDrive callback is getting trigggered as soon as there is new Data recieved
{
  int x = myDrive.Position;  
}
