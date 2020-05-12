namespace Pololu.Usc.Enums
{
    /// <summary>
    /// The correspondence between errors and bits in the two-byte error register.
    /// For more details about what the errors mean, see the user's guide. 
    /// </summary>
    public enum ErrorCode : byte
    {
        ERROR_SERIAL_SIGNAL = 0,
        ERROR_SERIAL_OVERRUN = 1,
        ERROR_SERIAL_BUFFER_FULL = 2,
        ERROR_SERIAL_CRC = 3,
        ERROR_SERIAL_PROTOCOL = 4,
        ERROR_SERIAL_TIMEOUT = 5,
        ERROR_SCRIPT_STACK = 6,
        ERROR_SCRIPT_CALL_STACK = 7,
        ERROR_SCRIPT_PROGRAM_COUNTER = 8,
    };

}