namespace Pololu.Usc.Enums
{
    public enum SerialMode : byte
    {
        ///<summary>On the Command Port, user can send commands and receive responses.
        ///TTL port/UART are connected to make a USB-to-serial adapter.</summary> 
        SERIAL_MODE_USB_DUAL_PORT = 0,

        ///<summary>On the Command Port, user can send commands to Maestro and
        /// simultaneously transmit bytes on the UART TX line, and user
        /// can receive bytes from the Maestro and the UART RX line.
        /// TTL port does not do anything.</summary>
        SERIAL_MODE_USB_CHAINED = 1,

        /// <summary>
        /// On the UART, user can send commands and receive reponses after
        /// sending a 0xAA byte to indicate the baud rate.
        /// Command Port receives bytes from the RX line.
        /// TTL Port does not do anything.
        /// </summary>
        SERIAL_MODE_UART_DETECT_BAUD_RATE = 2,

        /// <summary>
        /// On the UART, user can send commands and receive reponses
        /// at a predetermined, fixed baud rate.
        /// Command Port receives bytes from the RX line.
        /// TTL Port does not do anything.
        /// </summary>
        SERIAL_MODE_UART_FIXED_BAUD_RATE = 3,
    };
}