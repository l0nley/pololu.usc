using Pololu.Usc.Enums;
using Pololu.Usc.Exceptions;
using System;
using System.Collections.Generic;

namespace Pololu.Usc.Models
{
    public class BytecodeProgram
    {
        private readonly List<string> _privateSourceLines = new List<string>();
        private readonly List<BytecodeInstruction> _instructionList = new List<BytecodeInstruction>();
        private readonly Stack<int> _openBlocks = new Stack<int>();
        private readonly Stack<BlockType> _openBlockTypes = new Stack<BlockType>();
        public Dictionary<string, ushort> subroutineAddresses = new Dictionary<string, ushort>();
        public Dictionary<string, byte> subroutineCommands = new Dictionary<string, byte>();
        // private const ushort CRC16_POLY = 40961;
        private int _maxBlock;

        public string GetSourceLine(int line)
        {
            return _privateSourceLines[line - 1];
        }

        public void AddSourceLine(string line)
        {
            _privateSourceLines.Add(line);
        }

        public int GetSourceLineCount()
        {
            return _privateSourceLines.Count;
        }

        public BytecodeInstruction this[int index]
        {
            get
            {
                return _instructionList[index];
            }
        }

        public int Count
        {
            get
            {
                return _instructionList.Count;
            }
        }

        internal void AddInstruction(BytecodeInstruction instruction)
        {
            _instructionList.Add(instruction);
        }

        internal void AddLiteral(int literal, string filename, int lineNumber, int columnNumber, bool isMiniMaestro)
        {
            if (_instructionList.Count == 0 || _instructionList[_instructionList.Count - 1].Operation != OperationCode.LITERAL)
            {
                AddInstruction(new BytecodeInstruction(OperationCode.LITERAL, filename, lineNumber, columnNumber));
            }
            _instructionList[_instructionList.Count - 1].AddLiteralArgument(literal, isMiniMaestro);
        }

        public IList<byte> GetByteList()
        {
            var byteList = new List<byte>();
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                byteList.AddRange(instruction.ToByteList());
            }
            return byteList;
        }


        public int FindLabelIndex(string name)
        {
            for (int index = 0; index < _instructionList.Count; ++index)
            {
                if (_instructionList[index].IsLabel && _instructionList[index].LabelName == name)
                {
                    return index;
                }
            }
            throw new IndexOutOfRangeException("Label not found.");
        }

        public BytecodeInstruction FindLabelInstruction(string name)
        {
            return _instructionList[FindLabelIndex(name)];
        }

        public BytecodeInstruction GetInstructionAt(ushort program_counter)
        {
            int num = 0;
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                List<byte> byteList = instruction.ToByteList();
                if (num >= program_counter && byteList.Count != 0)
                    return instruction;
                num += byteList.Count;
            }
            return null;
        }

        public ushort GetCRC()
        {
            List<byte> message = new List<byte>();
            ushort[] numArray = new ushort[128];
            foreach (string key in subroutineCommands.Keys)
            {
                if (subroutineCommands[key] != (byte)54)
                    numArray[(int)subroutineCommands[key] - 128] = subroutineAddresses[key];
            }
            foreach (ushort num in numArray)
            {
                message.Add((byte)((uint)num & (uint)byte.MaxValue));
                message.Add((byte)((uint)num >> 8));
            }
            message.AddRange(GetByteList());
            return BytecodeProgram.CRC(message);
        }

        internal void OpenBlock(BlockType blocktype, string filename, int line_number, int column_number)
        {
            AddInstruction(BytecodeInstruction.NewLabel("block_start_" + _maxBlock.ToString(), filename, line_number, column_number));
            _openBlocks.Push(_maxBlock);
            _openBlockTypes.Push(blocktype);
            ++_maxBlock;
        }

        internal BlockType GetCurrentBlockType()
        {
            return _openBlockTypes.Peek();
        }

        internal string GetCurrentBlockStartLabel()
        {
            return "block_start_" + _openBlocks.Peek().ToString();
        }

        internal string GetCurrentBlockEndLabel()
        {
            return "block_end_" + _openBlocks.Peek().ToString();
        }

        internal string GetNextBlockEndLabel()
        {
            return "block_end_" + _maxBlock.ToString();
        }

        internal void CloseBlock(string filename, int line_number, int column_number)
        {
            AddInstruction(BytecodeInstruction.NewLabel("block_end_" + _openBlocks.Pop().ToString(), filename, line_number, column_number));
            int num = (int)_openBlockTypes.Pop();
        }

        internal bool BlockIsOpen
        {
            get
            {
                return _openBlocks.Count > 0;
            }
        }

        internal void CompleteJumps()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            int num = 0;
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                if (instruction.IsLabel)
                {
                    if (dictionary.ContainsKey(instruction.LabelName))
                        GenerateInstructionException(instruction, "The label " + instruction.LabelName + " has already been used.");
                    dictionary[instruction.LabelName] = num;
                }
                num += instruction.ToByteList().Count;
            }
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                try
                {
                    if (instruction.IsJumpToLabel)
                        instruction.AddLiteralArgument(dictionary[instruction.LabelName], false);
                }
                catch (KeyNotFoundException)
                {
                    GenerateInstructionException(instruction, "The label " + instruction.LabelName + " was not found.");
                }
            }
        }

        internal void CompleteCalls(bool isMiniMaestro)
        {
            uint num1 = 128;
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                if (instruction.IsSubroutine)
                {
                    if (subroutineCommands.ContainsKey(instruction.LabelName))
                    {
                        GenerateInstructionException(instruction, "The subroutine " + instruction.LabelName + " has already been defined.");
                    }
                    subroutineCommands[instruction.LabelName] = num1 < 256U ? (byte)num1 : (byte)54;
                    ++num1;
                    if (num1 > (uint)byte.MaxValue && !isMiniMaestro)
                    {
                        GenerateInstructionException(instruction, "Too many subroutines.  The limit for the Micro Maestro is 128.");
                    }
                }
            }
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                try
                {
                    if (instruction.IsCall)
                        instruction.SetOpcode(subroutineCommands[instruction.LabelName]);
                }
                catch (KeyNotFoundException)
                {
                    GenerateInstructionException(instruction, "Did not understand '" + instruction.LabelName + "'");
                }
            }
            int num2 = 0;
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                if (instruction.IsSubroutine)
                    subroutineAddresses[instruction.LabelName] = (ushort)num2;
                num2 += instruction.ToByteList().Count;
            }
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                if (instruction.Operation == OperationCode.CALL)
                    instruction.LiteralArguments.Add(subroutineAddresses[instruction.LabelName]);
            }
        }

        internal void CompleteLiterals()
        {
            foreach (BytecodeInstruction instruction in _instructionList)
            {
                instruction.CompleteLiterals();
            }
        }

        private static void GenerateInstructionException(BytecodeInstruction instruction, string message)
        {
            throw new InstructionException($"{instruction.FileName}:{instruction.LineNumber}:{instruction.ColumnNumber}: {message}");
        }


        private static ushort OneByteCRC(byte v)
        {
            ushort num = (ushort)v;
            for (int index = 0; index < 8; ++index)
            {
                if (((int)num & 1) == 1)
                    num = (ushort)((int)num >> 1 ^ 40961);
                else
                    num >>= 1;
            }
            return num;
        }

        private static ushort CRC(List<byte> message)
        {
            ushort num = 0;
            for (ushort index = 0; (int)index < message.Count; ++index)
                num = (ushort)((uint)num >> 8 ^ (uint)OneByteCRC((byte)((uint)(byte)num ^ (uint)message[(int)index])));
            return num;
        }
    }
}