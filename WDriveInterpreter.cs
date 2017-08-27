namespace WDriveConnection
{
    internal class WDriveInterpreter
    {
        private readonly NewDataFromSerialArrived _newDataEvent;

        public WDriveInterpreter(NewDataFromSerialArrived newDataEvent)
        {
            _newDataEvent = newDataEvent;
        }

        public void InterpretStringFragment(string data2)
        {
            _newDataEvent(data2, new byte[]{});
        }

        internal void InterpretBytes(byte[] receiveBytes)
        {
            _newDataEvent("", receiveBytes);
        }
    }
}