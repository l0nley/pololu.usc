using System.Runtime.InteropServices;

namespace Pololu.Usc.Linux
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct MicroMaestroVariables
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

        /// <summary>Meaningless bytes to protect the program from stack underflows.</summary>
        /// <remarks>This is public to avoid mono warning CS0169.</remarks>
        public fixed short Buffer[3];

        /// <summary>
        /// The data stack used by the script.  The values in locations 0 through stackPointer-1
        /// are on the stack.
        /// </summary>
        public fixed short Stack[32];

        /// <summary>
        /// The call stack used by the script.  The addresses in locations 0 through
        /// callStackPointer-1 are on the call stack.  The next return will make the
        /// program counter go to callStack[callStackPointer-1].
        /// </summary>
        public fixed ushort CallStack[10];

        /// <summary>
        /// 0 = script is running.
        /// 1 = script is done.
        /// 2 = script will be done as soon as it executes one more instruction
        ///     (used to implement step-through debugging features)
        /// </summary>
        public byte ScriptDone;

        /// <summary>Meaningless byte to protect the program from call stack overflows.</summary>
        /// <remarks>This is public to avoid mono warning CS0169.</remarks>
        public byte Buffer2;

        // NOTE: C# does not allow fixed arrays of structs; after these variables,
        // 6 copies of servoSetting follow on the Micro Maestro.
    }
    
}