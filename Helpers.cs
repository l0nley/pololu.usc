using Pololu.Usc.Exceptions;

namespace Pololu.Usc
{
    internal static class Helpers
    {
        public static string GetErrorString(int error)
        {
            switch (error)
            {
                case -1:
                    return "I/O error.";
                case -2:
                    return "Invalid parameter.";
                case -3:
                    return "Access denied.";
                case -4:
                    return "Device does not exist.";
                case -5:
                    return "No such entity.";
                case -6:
                    return "Busy.";
                case -7:
                    return "Timeout.";
                case -8:
                    return "Overflow.";
                case -9:
                    return "Pipe error.";
                case -10:
                    return "System call was interrupted.";
                case -11:
                    return "Out of memory.";
                case -12:
                    return "Unsupported/unimplemented operation.";
                case -99:
                    return "Other error.";
                default:
                    return "Unknown error code " + error + ".";
            };
        }

        public static int ThrowIfErrorCode(int code)
        {
            if (code >= 0)
            {
                return code;
            }

            throw new UsbException(GetErrorString(code));
        }
    }
}
