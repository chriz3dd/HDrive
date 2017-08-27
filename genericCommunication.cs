namespace WDriveConnection
{
    // Declaration
    public delegate void NewDataFromSerialArrived(string data, byte[] bindata);

    public class GenericCommunication
    {
        private readonly WDriveInterpreter _interpreter;

        public GenericCommunication(NewDataFromSerialArrived newData)
        {
            _interpreter = new WDriveInterpreter(newData);
        }

        public void InterpretStringFragment(string data)
        {
            _interpreter.InterpretStringFragment(data);
        }
    }
}