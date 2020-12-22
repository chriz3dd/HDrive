
namespace HDrive
{
    // This are the different HDrive Tickets the drive can send to the host.
    public enum HDriveTicket
    {
        HDriveTicket = 0,
        CANTicket = 1,
        BinaryDebugTicket = 2,
        BinaryTicket = 3,
        BinaryCANTicket = 4,
        EPROMConfigTicket = 5,
        ObjTableTicket = 6,
        BinaryDataLoggerTicket = 7,
        UnknownTicket = 8,
    }

    // These are the different opperation modes the HDrive is currently supporting
    public enum OperationModes
    {
        Error = -1,
        Stop = 0,
        Stepper = 8,
        Calibration = 9,

        NegativeLimitSwitchAdvanced = 15,
        PositivLimitSwitchAdvanced = 16,
        NegativLimitSwitch = 17,
        PositivLimitSwitch = 18,
        HomingCompleted = 20,

        TorqueMode = 128,
        PositionControl = 129,
        PositionControl_NPP = 133,
        PositionControl_STEP = 134,
        SpeedControl = 130,
        SpeedControl_NPP = 132,
        PathPlaner = 151,
        SetZero = 101
    }

    // These are the current System modes 
    public enum SystemModes
    {
        BootloaderUpgrade = 1, // Starts bootloder update mode
        ResetPosition = 2, // Resets position to 0
        FactoryReset = 3, // Loads the factory defaults
        SaveDataToEEPROM = 4, // Saves all objects into the EEPROM
        ResetLastError = 5, // Resets and confirm the last motor error
        SystemReset = 6 // Resets the HDrive
    }

    // Struct to hold a 3D-Vector
    public struct Double3
    {
        public int X, Y, Z;
    }

    // Special function in CAN-Mode
    public enum CanSpecialFunction
    {
        NoFunction = 0,
        ResetLastError = 5
    }
    // Motion variables ton configure a motion from a to b
    public struct HDriveMotionVariables
    {
        public OperationModes ControlMode;
        public int TargetPosition; // Target position in 1/10°
        public int TargetSpeed; // For HDrive17 from -1500 RPM to +1500 RPM
        public int TargetTorque; // For HDrive17 from -600 mNm to 600 mNm
        public int TargetAcceleration; // Target acceleration in RPM/s
        public int TargetDeceleration; // Target deceleration in RPM/s
    }

}
