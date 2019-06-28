namespace WDriveConnection
{
    public interface IWDriveConnection
    {
        void Open();
        void Close();
        bool Write(string str);
    }
}