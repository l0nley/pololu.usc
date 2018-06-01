using System.Collections.Generic;
using System;
using Pololu.Usc.Exceptions;
using Pololu.Usc.Enums;

namespace Pololu.Usc
{
    public class BytecodeInstruction
    {
        public string FileName { get; private set; }

        public int LineNumber { get; private set; }

        public int ColumnNumber { get; private set; }

        public OperationCode Operation { get; private set; }

        public bool IsLabel { get; private set; }

        public bool IsJumpToLabel { get; private set; }

        public string LabelName { get; private set; }

        public bool IsSubroutine { get; private set; }

        public bool IsCall { get; private set; }

        public IList<int> LiteralArguments { get; private set; } = new List<int>();

        public void AddLiteralArgument(int value, bool isMiniMaestro)
        {
            LiteralArguments.Add(value);
            if (!isMiniMaestro && LiteralArguments.Count > 32)
            {
                throw new TooManyLiteralsException("Too many literals (> 32) in a row: this will overflow the stack.");
            }

            if (LiteralArguments.Count > 126)
            {
                throw new TooManyLiteralsException("Too many literals (> 126) in a row: this will overflow the stack.");
            }
        }

        public void SetOpcode(byte value)
        {
            if (Operation != OperationCode.QUIT)
            {
                throw new Exception("The opcode has already been set.");
            }
            Operation = (OperationCode)value;
        }

        

        public BytecodeInstruction(OperationCode op, string filename, int lineNumber, int columnNumber)
        {
            Operation = op;
            FileName = filename;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public BytecodeInstruction(OperationCode op, int literalArgument, string filename, int lineNumber, int columnNumber)
        {
            Operation = op;
            LiteralArguments.Add(literalArgument);
            FileName = filename;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public List<byte> ToByteList()
        {
            List<byte> byteList = new List<byte>();
            if (IsLabel || IsSubroutine)
            {
                return byteList;
            }
            byteList.Add((byte)Operation);
            if (Operation == OperationCode.LITERAL || Operation == OperationCode.JUMP || (Operation == OperationCode.JUMP_Z || Operation == OperationCode.CALL))
            {
                if (LiteralArguments.Count == 0)
                {
                    byteList.Add(0);
                    byteList.Add(0);
                }
                else
                {
                    byteList.Add((byte)((uint)(ushort)LiteralArguments[0] % 256U));
                    byteList.Add((byte)((uint)(ushort)LiteralArguments[0] / 256U));
                }
            }
            else if (Operation == OperationCode.LITERAL8)
            {
                byteList.Add((byte)LiteralArguments[0]);
            }
            else if (Operation == OperationCode.LITERAL_N)
            {
                byteList.Add((byte)(LiteralArguments.Count * 2));
                foreach (int literalArgument in LiteralArguments)
                {
                    byteList.Add((byte)((uint)(ushort)literalArgument % 256U));
                    byteList.Add((byte)((uint)(ushort)literalArgument / 256U));
                }
            }
            else if (Operation == OperationCode.LITERAL8_N)
            {
                byteList.Add((byte)LiteralArguments.Count);
                foreach (int literalArgument in LiteralArguments)
                {
                    byteList.Add((byte)literalArgument);
                }
            }
            return byteList;
        }

        [Obsolete]
        public void Error(string msg)
        {
            throw new Exception($"{FileName}:{LineNumber}:{ColumnNumber}: {msg}");
        }

        public static BytecodeInstruction NewSubroutine(string name, string filename, int column_number, int line_number)
        {
            return new BytecodeInstruction(OperationCode.QUIT, filename, column_number, line_number)
            {
                IsSubroutine = true,
                LabelName = name
            };
        }

        public static BytecodeInstruction NewCall(string name, string filename, int column_number, int line_number)
        {
            return new BytecodeInstruction(OperationCode.QUIT, filename, column_number, line_number)
            {
                IsCall = true,
                LabelName = name
            };
        }

        public static BytecodeInstruction NewLabel(string name, string filename, int column_number, int line_number)
        {
            return new BytecodeInstruction(OperationCode.QUIT, filename, column_number, line_number)
            {
                IsLabel = true,
                LabelName = name
            };
        }

        public static BytecodeInstruction NewJumpToLabel(string name, string filename, int column_number, int line_number)
        {
            return new BytecodeInstruction(OperationCode.JUMP, filename, column_number, line_number)
            {
                IsJumpToLabel = true,
                LabelName = name
            };
        }

        public static BytecodeInstruction NewConditionalJumpToLabel(string name, string filename, int column_number, int line_number)
        {
            return new BytecodeInstruction(OperationCode.JUMP_Z, filename, column_number, line_number)
            {
                IsJumpToLabel = true,
                LabelName = name
            };
        }

        public void CompleteLiterals()
        {
            if (Operation != OperationCode.LITERAL)
            {
                return;
            }
            var flag = false;
            foreach (int literalArgument in LiteralArguments)
            {
                if (literalArgument > (int)byte.MaxValue || literalArgument < 0)
                    flag = true;
            }
            if (flag && LiteralArguments.Count > 1)
            {
                Operation = OperationCode.LITERAL_N;
            }
            else if (flag && LiteralArguments.Count == 1)
            {
                Operation = OperationCode.LITERAL;
            }
            else if (LiteralArguments.Count > 1)
            {
                Operation = OperationCode.LITERAL8_N;
            }
            else
            {
                Operation = OperationCode.LITERAL8;
            }
        }
    }
}
