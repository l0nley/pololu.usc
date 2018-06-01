namespace Pololu.Usc.Structs
{
    public struct MaestroVariables
    {
        /// <summary>
        /// The number of values on the data stack (0-32).  A value of 0 means the stack is empty.
        /// </summary>
        public byte StackPointer;

        /// <summary>
        /// The number of return locations on the call stack (0-10).  A value of 0 means the stack is empty.
        /// </summary>
        public byte CallStackPointer;

        /// <summary>
        /// The error register.  Each bit stands for a different error (see uscError).
        /// If the bit is one, then it means that error occurred some time since the last
        /// GET_ERRORS serial command or CLEAR_ERRORS USB command.
        /// </summary>
        public ushort Errors;

        /// <summary>
        /// The address (in bytes) of the next bytecode instruction that will be executed.
        /// </summary>
        public ushort ProgramCounter;

        /// <summary>
        /// 0 = script is running.
        /// 1 = script is done.
        /// 2 = script will be done as soon as it executes one more instruction
        ///     (used to implement step-through debugging features)
        /// </summary>
        public byte ScriptDone;

        /// <summary>
        /// The performance flag register.  Each bit represents a different flag.
        /// If it is 1, then it means that the flag occurred some time since the last
        /// getVariables request.  This register is always 0 for the Micro Maestro
        /// because performance flags only apply to the Mini Maestros.
        /// </summary>
        public byte PerformanceFlags;
    }
}