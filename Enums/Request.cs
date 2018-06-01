namespace Pololu.Usc.Enums
{
    public enum Request
    {
        REQUEST_GET_PARAMETER = 0x81,
        REQUEST_SET_PARAMETER = 0x82,
        REQUEST_GET_VARIABLES = 0x83,
        REQUEST_SET_SERVO_VARIABLE = 0x84,
        REQUEST_SET_TARGET = 0x85,
        REQUEST_CLEAR_ERRORS = 0x86,

        // These four requests are only valid on *Mini* Maestros.
        REQUEST_GET_SERVO_SETTINGS = 0x87,
        REQUEST_GET_STACK = 0x88,
        REQUEST_GET_CALL_STACK = 0x89,
        REQUEST_SET_PWM = 0x8A,

        REQUEST_REINITIALIZE = 0x90,
        REQUEST_ERASE_SCRIPT = 0xA0,
        REQUEST_WRITE_SCRIPT = 0xA1,
        REQUEST_SET_SCRIPT_DONE = 0xA2, // wValue is 0 for go, 1 for stop, 2 for single-step
        REQUEST_RESTART_SCRIPT_AT_SUBROUTINE = 0xA3,
        REQUEST_RESTART_SCRIPT_AT_SUBROUTINE_WITH_PARAMETER = 0xA4,
        REQUEST_RESTART_SCRIPT = 0xA5,
        REQUEST_START_BOOTLOADER = 0xFF
    }
}