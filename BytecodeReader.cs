using Pololu.Usc.Enums;
using Pololu.Usc.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Pololu.Usc
{
    public static class BytecodeReader
    {
        private static Dictionary<string, OperationCode> dictionary;
        private static BytecodeReader.Mode mode;

        private static void InitDictionary()
        {
            if (dictionary != null)
            {
                return;
            }
            var names = Enum.GetNames(typeof(OperationCode));
            var values = (OperationCode[])Enum.GetValues(typeof(OperationCode));
            dictionary = new Dictionary<string, OperationCode>();
            for (int index = 0; index < names.Length; ++index)
            {
                dictionary[names[index]] = values[index];
            }
        }

        public static void WriteListing(BytecodeProgram program, string filename)
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter((Stream)fileStream))
                {
                    int index1 = 0;
                    int num1 = 0;
                    BytecodeInstruction bytecodeInstruction = (BytecodeInstruction)null;
                    if (program.Count != 0)
                        bytecodeInstruction = program[index1];
                    for (int line = 1; line <= program.GetSourceLineCount(); ++line)
                    {
                        int num2 = 0;
                        streamWriter.Write(num1.ToString("X4") + ": ");
                        for (; bytecodeInstruction != null && bytecodeInstruction.LineNumber == line; bytecodeInstruction = index1 < program.Count ? program[index1] : (BytecodeInstruction)null)
                        {
                            foreach (byte num3 in bytecodeInstruction.ToByteList())
                            {
                                streamWriter.Write(num3.ToString("X2"));
                                ++num1;
                                num2 += 2;
                            }
                            ++index1;
                        }
                        for (int index2 = 0; index2 < 20 - num2; ++index2)
                            streamWriter.Write(" ");
                        streamWriter.Write(" -- ");
                        streamWriter.WriteLine(program.GetSourceLine(line));
                    }
                    streamWriter.WriteLine("");
                    streamWriter.WriteLine("Subroutines:");
                    streamWriter.WriteLine("Hex Decimal Address Name");
                    string[] strArray = new string[128];
                    foreach (KeyValuePair<string, ushort> subroutineAddress1 in program.subroutineAddresses)
                    {
                        string key = subroutineAddress1.Key;
                        if (program.subroutineCommands[key] != (byte)54)
                        {
                            byte num2 = (byte)((uint)program.subroutineCommands[key] - 128U);
                            ushort subroutineAddress2 = program.subroutineAddresses[key];
                            strArray[(int)num2] = num2.ToString("X2") + "  " + num2.ToString("D3") + "     " + subroutineAddress2.ToString("X4") + "    " + key;
                        }
                    }
                    for (int index2 = 0; index2 < strArray.Length && strArray[index2] != null; ++index2)
                        streamWriter.WriteLine(strArray[index2]);
                    foreach (KeyValuePair<string, ushort> subroutineAddress1 in program.subroutineAddresses)
                    {
                        string key = subroutineAddress1.Key;
                        if (program.subroutineCommands[key] == (byte)54)
                        {
                            ushort subroutineAddress2 = program.subroutineAddresses[key];
                            streamWriter.WriteLine("--  ---     " + subroutineAddress2.ToString("X4") + "    " + key);
                        }
                    }
                }
            }
        }

        public static BytecodeProgram Read(string program, bool isMiniMaestro)
        {
            InitDictionary();
            var bytecodeProgram = new BytecodeProgram();
            mode = Mode.NORMAL;
            if (program == null)
            {
                program = "";
            }
            var strArray = program.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int lineNumber = 1; lineNumber <= strArray.Length; ++lineNumber)
            {
                string str1 = strArray[lineNumber - 1];
                bytecodeProgram.AddSourceLine(str1);
                int column_number = 1;
                string str2 = Regex.Replace(str1, "#.*", "");
                char[] chArray = new char[2] { ' ', '\t' };
                foreach (string str3 in str2.Split(chArray))
                {
                    if (str3 == "")
                    {
                        ++column_number;
                    }
                    else
                    {
                        string upperInvariant = str3.ToUpperInvariant();
                        switch (mode)
                        {
                            case Mode.NORMAL:
                                ParseString(upperInvariant, bytecodeProgram, "script", lineNumber, column_number, isMiniMaestro);
                                break;
                            case Mode.GOTO:
                                ParseGoto(upperInvariant, bytecodeProgram, "script", lineNumber, column_number);
                                break;
                            case Mode.SUBROUTINE:
                                ParseSubroutine(upperInvariant, bytecodeProgram, "script", lineNumber, column_number);
                                break;
                        }
                        column_number += upperInvariant.Length + 1;
                    }
                }
            }
            if (bytecodeProgram.BlockIsOpen)
            {
                string currentBlockStartLabel = bytecodeProgram.GetCurrentBlockStartLabel();
                var inst = bytecodeProgram.FindLabelInstruction(currentBlockStartLabel);
                throw new ParsingException($"{inst.FileName}:{inst.LineNumber}:{inst.ColumnNumber}: BEGIN block was never closed");
            }
            bytecodeProgram.CompleteLiterals();
            bytecodeProgram.CompleteCalls(isMiniMaestro);
            bytecodeProgram.CompleteJumps();
            return bytecodeProgram;
        }

        private static void ParseGoto(string s, BytecodeProgram bytecodeProgram, string filename, int lineNumber, int columnNumber)
        {
            bytecodeProgram.AddInstruction(BytecodeInstruction.NewJumpToLabel("USER_" + s, filename, lineNumber, columnNumber));
            mode = Mode.NORMAL;
        }

        private static void ParseSubroutine(string s, BytecodeProgram bytecodeProgram, string filename, int line_number, int column_number)
        {
            if (LooksLikeLiteral(s))
                throw new Exception("The name " + s + " is not valid as a subroutine name (it looks like a number).");
            if (dictionary.ContainsKey(s))
                throw new Exception("The name " + s + " is not valid as a subroutine name (it is a built-in command).");
            foreach (string name in Enum.GetNames(typeof(Keyword)))
            {
                if (name == s)
                    throw new Exception("The name " + s + " is not valid as a subroutine name (it is a keyword).");
            }
            bytecodeProgram.AddInstruction(BytecodeInstruction.NewSubroutine(s, filename, line_number, column_number));
            mode = Mode.NORMAL;
        }

        private static bool LooksLikeLiteral(string s)
        {
            if (!Regex.Match(s, "^-?[0-9.]+$").Success)
                return Regex.Match(s, "^0[xX][0-9a-fA-F.]+$").Success;
            return true;
        }

        private static void ParseString(string s, BytecodeProgram bytecodeProgram, string filename, int lineNumber, int columnNumber, bool isMiniMaestro)
        {
            try
            {
                if (LooksLikeLiteral(s))
                {
                    Decimal num;
                    if (s.StartsWith("0X"))
                    {
                        num = (Decimal)long.Parse(s.Substring(2), NumberStyles.HexNumber);
                        if (num > new Decimal((int)ushort.MaxValue) || num < new Decimal(0))
                            throw new Exception("Value " + s + " is not in the allowed range of " + (object)(ushort)0 + " to " + (object)ushort.MaxValue + ".");
                        if ((Decimal)(ushort)num != num)
                            throw new Exception("Value " + s + " must be an integer.");
                    }
                    else
                    {
                        num = Decimal.Parse(s);
                        if (num > new Decimal((int)short.MaxValue) || num < new Decimal((int)short.MinValue))
                            throw new Exception("Value " + s + " is not in the allowed range of " + (object)short.MinValue + " to " + (object)short.MaxValue + ".");
                        if ((Decimal)(short)num != num)
                            throw new Exception("Value " + s + " must be an integer.");
                    }
                    int literal = (int)(short)(long)(num % new Decimal((int)ushort.MaxValue));
                    bytecodeProgram.AddLiteral(literal, filename, lineNumber, columnNumber, isMiniMaestro);
                    return;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing " + s + ": " + ex.ToString());
            }
            if (s == Keyword.GOTO.ToString())
                mode = Mode.GOTO;
            else if (s == Keyword.SUB.ToString())
            {
                mode = Mode.SUBROUTINE;
            }
            else
            {
                Match match = Regex.Match(s, "(.*):$");
                if (match.Success)
                    bytecodeProgram.AddInstruction(BytecodeInstruction.NewLabel("USER_" + match.Groups[1].ToString(), filename, lineNumber, columnNumber));
                else if (s == Keyword.BEGIN.ToString())
                    bytecodeProgram.OpenBlock(BlockType.BEGIN, filename, lineNumber, columnNumber);
                else if (s == Keyword.WHILE.ToString())
                {
                    if (bytecodeProgram.GetCurrentBlockType() != BlockType.BEGIN)
                        throw new Exception("WHILE must be inside a BEGIN...REPEAT block");
                    bytecodeProgram.AddInstruction(BytecodeInstruction.NewConditionalJumpToLabel(bytecodeProgram.GetCurrentBlockEndLabel(), filename, lineNumber, columnNumber));
                }
                else if (s == Keyword.REPEAT.ToString())
                {
                    try
                    {
                        if (bytecodeProgram.GetCurrentBlockType() != BlockType.BEGIN)
                            throw new Exception("REPEAT must end a BEGIN...REPEAT block");
                        bytecodeProgram.AddInstruction(BytecodeInstruction.NewJumpToLabel(bytecodeProgram.GetCurrentBlockStartLabel(), filename, lineNumber, columnNumber));
                        bytecodeProgram.CloseBlock(filename, lineNumber, columnNumber);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": Found REPEAT without a corresponding BEGIN");
                    }
                }
                else if (s == Keyword.IF.ToString())
                {
                    bytecodeProgram.OpenBlock(BlockType.IF, filename, lineNumber, columnNumber);
                    bytecodeProgram.AddInstruction(BytecodeInstruction.NewConditionalJumpToLabel(bytecodeProgram.GetCurrentBlockEndLabel(), filename, lineNumber, columnNumber));
                }
                else if (s == Keyword.ENDIF.ToString())
                {
                    try
                    {
                        if (bytecodeProgram.GetCurrentBlockType() != BlockType.IF && bytecodeProgram.GetCurrentBlockType() != BlockType.ELSE)
                            throw new Exception("ENDIF must end an IF...ENDIF or an IF...ELSE...ENDIF block.");
                        bytecodeProgram.CloseBlock(filename, lineNumber, columnNumber);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": Found ENDIF without a corresponding IF");
                    }
                }
                else if (s == Keyword.ELSE.ToString())
                {
                    try
                    {
                        if (bytecodeProgram.GetCurrentBlockType() != BlockType.IF)
                            throw new Exception("ELSE must be part of an IF...ELSE...ENDIF block.");
                        bytecodeProgram.AddInstruction(BytecodeInstruction.NewJumpToLabel(bytecodeProgram.GetNextBlockEndLabel(), filename, lineNumber, columnNumber));
                        bytecodeProgram.CloseBlock(filename, lineNumber, columnNumber);
                        bytecodeProgram.OpenBlock(BlockType.ELSE, filename, lineNumber, columnNumber);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": Found ELSE without a corresponding IF");
                    }
                }
                else
                {
                    try
                    {
                        OperationCode op = dictionary[s];
                        switch (op)
                        {
                            case OperationCode.LITERAL:
                            case OperationCode.LITERAL8:
                            case OperationCode.LITERAL_N:
                            case OperationCode.LITERAL8_N:
                                throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": Literal commands may not be used directly in a program.  Integers should be entered directly.");
                            case OperationCode.JUMP:
                            case OperationCode.JUMP_Z:
                                throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": Jumps may not be used directly in a program.");
                            default:
                                if (!isMiniMaestro && (byte)op >= (byte)50)
                                    throw new Exception(filename + ":" + (object)lineNumber + ":" + (object)columnNumber + ": " + op.ToString() + " is only available on the Mini Maestro 12, 18, and 24.");
                                bytecodeProgram.AddInstruction(new BytecodeInstruction(op, filename, lineNumber, columnNumber));
                                break;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        bytecodeProgram.AddInstruction(BytecodeInstruction.NewCall(s, filename, lineNumber, columnNumber));
                    }
                }
            }
        }

        private enum Mode
        {
            NORMAL,
            GOTO,
            SUBROUTINE,
        }
    }
}