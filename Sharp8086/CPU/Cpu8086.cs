#region License
// The MIT License (MIT)
// 
// Copyright (c) 2016 Digital Singularity
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using Sharp8086.Core;
using Sharp8086.Peripheral.IO;

namespace Sharp8086.CPU
{
    public sealed class Cpu8086 : ICpu, IInstructionFetcher
    {
        public const int PAGE_SHIFT = 12;

        public const int AX = 0;
        public const int CX = 1;
        public const int DX = 2;
        public const int BX = 3;

        public const int SP = 4;
        public const int BP = 5;
        public const int SI = 6;
        public const int DI = 7;

        public const int ES = 8;
        public const int CS = 9;
        public const int SS = 10;
        public const int DS = 11;

        public const int IP = 12;
        public const int FLAGS = 13;

        public const int AL = 0xFFE0;
        public const int CL = 0xFFE1;
        public const int DL = 0xFFE2;
        public const int BL = 0xFFE3;
        public const int AH = 0xFFE4;
        public const int CH = 0xFFE5;
        public const int DH = 0xFFE6;
        public const int BH = 0xFFE7;

        private const int CF = 0;
        private const int PF = 2;
        private const int AF = 4;
        private const int ZF = 6;
        private const int SF = 7;
        private const int TF = 8;
        private const int IF = 9;
        private const int DF = 10;
        private const int OF = 11;

        private const int IO_PORT_OFFSET = 0xE0000;
        private const int IO_PORT_SIZE = 0x10000;

        private static readonly bool[] parityLookup =
        {
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true
        };

        private readonly IPageController[] pages;
        private readonly IDrive[] drives = new IDrive[0x100];
        private readonly ushort[] registers;

        static Cpu8086()
        {
            Debug.Assert(parityLookup.Length == 256);
        }
        public Cpu8086(Stream biosFile, uint memorySize)
        {
            registers = new ushort[14];
            var numberPages = memorySize >> PAGE_SHIFT;
            pages = new IPageController[numberPages];
            var memory = new byte[memorySize];
            for (var i = 0u; i != memorySize; i++)
                memory[i] = 0xCC;

            var defaultPageController = new ConventionPageController(memory);
            for (var i = 0; i < numberPages; i++)
                pages[i] = defaultPageController;

            const int ioPortOffsetPage = IO_PORT_OFFSET >> PAGE_SHIFT;
            var ioPageController = new IOPageController(IO_PORT_OFFSET, IO_PORT_SIZE);
            for (var i = 0; i < IO_PORT_SIZE >> PAGE_SHIFT; i++)
                pages[ioPortOffsetPage + i] = ioPageController;

            if (biosFile.Length != 0x10000)
                throw new InvalidDataException();
            biosFile.Read(memory, 0xF0000, (int)biosFile.Length);

            registers[CS] = 0xF000;
            registers[IP] = 0xFFF0;
        }

        public bool ProcessInstruction()
        {
            string instructionText = $"{registers[CS]:X4}:{registers[IP]:X4} ";
            var instruction = OpCodeManager.Decode(this);
            instructionText += OutputInstruction(instruction);

            if (registers[CS] != 0xF000)
                Console.WriteLine(instructionText);

            switch (instruction.Type)
            {
                case OpCodeManager.InstructionType.Clc:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << CF));
                    break;
                case OpCodeManager.InstructionType.Cld:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << DF));
                    break;
                case OpCodeManager.InstructionType.Cli:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << IF));
                    break;
                case OpCodeManager.InstructionType.Stc:
                    registers[FLAGS] = (ushort)(registers[FLAGS] | (1 << CF));
                    break;
                case OpCodeManager.InstructionType.Sti:
                    registers[FLAGS] = (ushort)(registers[FLAGS] | (1 << IF));
                    break;
                case OpCodeManager.InstructionType.JA:
                    if (((registers[FLAGS] >> CF) & 1) == 0 && ((registers[FLAGS] >> ZF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JB:
                    if (((registers[FLAGS] >> CF) & 1) != 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JNB:
                    if (((registers[FLAGS] >> CF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JL:
                    if (((registers[FLAGS] >> SF) & 1) != ((registers[FLAGS] >> OF) & 1))
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JGE:
                    if (((registers[FLAGS] >> SF) & 1) == ((registers[FLAGS] >> OF) & 1))
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JNZ:
                    if (((registers[FLAGS] >> ZF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.JZ:
                    if (((registers[FLAGS] >> ZF) & 1) != 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.Jump:
                    Debug.Assert(instruction.Argument1 == OpCodeManager.ARG_CONSTANT);
                    registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.FarJump:
                    ProcessFarJump(instruction);
                    break;
                case OpCodeManager.InstructionType.Move:
                    ProcessMov(instruction);
                    break;
                case OpCodeManager.InstructionType.Lea:
                    ProcessLea(instruction);
                    break;
                case OpCodeManager.InstructionType.Lds:
                    ProcessLoadFarPointer(DS, instruction);
                    break;
                case OpCodeManager.InstructionType.Les:
                    ProcessLoadFarPointer(ES, instruction);
                    break;
                case OpCodeManager.InstructionType.Out:
                    var port = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);
                    var value = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);
                    if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                        WriteU8((uint)(IO_PORT_OFFSET + port), (byte)value);
                    else WriteU16((uint)(IO_PORT_OFFSET + port), value);
                    break;
                case OpCodeManager.InstructionType.Pop:
                    registers[instruction.Argument1] = Pop();
                    break;
                case OpCodeManager.InstructionType.Push:
                    if (instruction.Argument1 == SP)
                    {
                        // 8086 has a bug where it pushes SP after it has been modified
                        registers[SP] -= 2;
                        WriteU16(SegmentToAddress(registers[SS], registers[SP]), registers[SP]);
                    }
                    else Push(registers[instruction.Argument1]);
                    break;
                case OpCodeManager.InstructionType.Cmps:
                case OpCodeManager.InstructionType.Lods:
                case OpCodeManager.InstructionType.Movs:
                case OpCodeManager.InstructionType.Stos:
                    ProcessStringOperation(instruction);
                    break;
                case OpCodeManager.InstructionType.Divide:
                    ProcessDivide(instruction);
                    break;
                case OpCodeManager.InstructionType.Multiply:
                    ProcessMultiply(instruction);
                    break;
                case OpCodeManager.InstructionType.Adc:
                case OpCodeManager.InstructionType.Add:
                case OpCodeManager.InstructionType.And:
                case OpCodeManager.InstructionType.Compare:
                case OpCodeManager.InstructionType.Or:
                case OpCodeManager.InstructionType.Ror:
                case OpCodeManager.InstructionType.Sar:
                case OpCodeManager.InstructionType.Shl:
                case OpCodeManager.InstructionType.Shr:
                case OpCodeManager.InstructionType.Subtract:
                case OpCodeManager.InstructionType.Test:
                case OpCodeManager.InstructionType.Xor:
                    ProcessArithmetic(instruction);
                    break;
                case OpCodeManager.InstructionType.Decrement:
                case OpCodeManager.InstructionType.Increment:
                    ProcessUnaryArithmetic(instruction);
                    break;
                case OpCodeManager.InstructionType.CallNearRelative:
                    Debug.Assert(instruction.Argument1 == OpCodeManager.ARG_CONSTANT);
                    Push(registers[IP]);
                    registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case OpCodeManager.InstructionType.ReturnNear:
                    registers[IP] = Pop();
                    if (instruction.Argument1 == OpCodeManager.ARG_CONSTANT)
                        registers[SP] = (ushort)(registers[SP] + instruction.Argument1Value);
                    else Debug.Assert(instruction.Argument1 == OpCodeManager.ARG_NONE);
                    break;
                case OpCodeManager.InstructionType.ReturnInterrupt:
                    registers[IP] = Pop();
                    registers[CS] = Pop();
                    registers[FLAGS] = Pop();
                    break;
                case OpCodeManager.InstructionType.Int:
                    Debug.Assert(instruction.Argument1 == OpCodeManager.ARG_CONSTANT);

                    if (instruction.Argument1Value == 3)
                        Debugger.Break();

                    Push(registers[FLAGS]);
                    Push(registers[CS]);
                    Push(registers[IP]);
                    registers[CS] = ReadU16((uint)instruction.Argument1Value * 4 + 2);
                    registers[IP] = ReadU16((uint)instruction.Argument1Value * 4);
                    break;
                case OpCodeManager.InstructionType.Xchg:
                    ProcessExchange(instruction);
                    break;
                case OpCodeManager.InstructionType.Cbw:
                    registers[AX] = (ushort)(sbyte)GetByteRegister(AL);
                    break;
                case OpCodeManager.InstructionType.EmulatorSpecial:
                    Debug.Assert(instruction.Argument1 == OpCodeManager.ARG_CONSTANT);
                    switch (instruction.Argument1Value)
                    {
                        case 0x02:
                            ProcessReadDrive();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case OpCodeManager.InstructionType.Hlt:
                    // Goes back one instruction so it halts again if process instruction is called
                    --registers[IP];
                    return false;
                default:
                    throw new NotImplementedException();
            }
            return true;
        }
        private void ProcessFarJump(OpCodeManager.Instruction instruction)
        {
            switch (instruction.Argument1)
            {
                case OpCodeManager.ARG_FAR_MEMORY:
                    registers[CS] = (ushort)((uint)instruction.Argument1Value >> 16);
                    registers[IP] = (ushort)(instruction.Argument1Value & 0xFFFF);
                    break;

                case OpCodeManager.ARG_MEMORY:
                    var prefix = instruction.Prefix;
                    if (prefix == 0) prefix = DS;
                    var address = SegmentToAddress(registers[prefix], (ushort)instruction.Argument1Value);
                    registers[CS] = ReadU16(address + 2);
                    registers[IP] = ReadU16(address);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessLoadFarPointer(int segmentRegister, OpCodeManager.Instruction instruction)
        {
            var prefix = instruction.Prefix;
            if (prefix == 0) prefix = DS;
            var address = SegmentToAddress(prefix, GetInstructionAddress(instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement));
            var memory = ReadU16(address);
            var segment = ReadU16(address + 2);

            registers[segmentRegister] = segment;
            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    registers[instruction.Argument1] = memory;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessReadDrive()
        {
            var driveNumber = ReadU8(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 1)));
            var headNumber = ReadU8(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 2)));
            var cylinderNumber = ReadU16(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 4)));
            var sectorNumber = ReadU8(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 5)));
            var sectorsToRead = ReadU8(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 6)));
            var destinationSegment = ReadU16(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 8)));
            var destinationOffset = ReadU16(SegmentToAddress(registers[SS], (ushort)(registers[BP] - 10)));

            var destination = SegmentToAddress(destinationSegment, destinationOffset);

            if (drives[driveNumber] != null)
            {
                var drive = drives[driveNumber];
                var driveOffset = headNumber * drive.NumberCylinders * drive.NumberSectors * drive.SectorSize;
                driveOffset += cylinderNumber * drive.NumberSectors * drive.SectorSize;
                driveOffset += (sectorNumber - 1) * drive.SectorSize;
                WriteBytes(destination, drive.Read(driveOffset, sectorsToRead * drive.SectorSize));
                registers[AX] = 0;
            }
            else registers[AX] = 1;
        }
        public void AttachDevice(IDevice device)
        {
            var hasUse = false;

            var drive = device as IDrive;
            if (drive != null)
            {
                var hdd = !drive.IsFloppyDrive;
                int i;
                for (i = 0; i < 0x80; i++)
                {
                    if (drives[i + (hdd ? 0x80 : 0)] == null)
                    {
                        drives[i + (hdd ? 0x80 : 0)] = drive;
                        break;
                    }
                }
                if (i == 0x80)
                    throw new NotImplementedException();
                hasUse = true;
            }

            var mappedDevice = device as IMemoryMappedDevice;
            if (mappedDevice != null)
            {
                foreach (var memory in mappedDevice.MappedMemory)
                {
                    const int pageMask = (1 << PAGE_SHIFT) - 1;
                    Debug.Assert((memory.Item1 & pageMask) == 0);
                    Debug.Assert((memory.Item2 & pageMask) == 0);

                    var startPage = (int)(memory.Item1 >> PAGE_SHIFT);
                    var numberPages = memory.Item2 >> PAGE_SHIFT;
                    for (var i = 0; i < numberPages; i++)
                        pages[startPage + i] = mappedDevice;
                }

                hasUse = true;
            }

            if (!hasUse)
                throw new NotImplementedException();
        }
        private void ProcessDivide(OpCodeManager.Instruction instruction)
        {
            uint value1;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                value1 = registers[AX];
            else value1 = (uint)registers[DX] << 16 | registers[AX];
            var value2 = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);

            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                value2 &= 0xFF;

            var quotient = value1 / value2;
            if ((instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) && quotient > 0xFF) || (!instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) && quotient > 0xFFFF))
                throw new NotImplementedException();

            var remainder = value1 % value2;

            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                registers[AX] = (ushort)((byte)quotient | ((remainder & 0xFF) << 8));
            else
            {
                registers[AX] = (ushort)quotient;
                registers[DX] = (ushort)remainder;
            }
        }
        private void ProcessMultiply(OpCodeManager.Instruction instruction)
        {
            var value1 = registers[AX];
            var value2 = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);

            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                value1 &= 0xFF;
                value2 &= 0xFF;
            }
            var result = (uint)value1 * value2;

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                truncResult = (byte)result;
                carry = result > 0xFF;
                sign = ((truncResult >> 7) & 1) == 1;
            }
            else
            {
                truncResult = (ushort)result;
                carry = result > 0xFFFF;
                sign = ((truncResult >> 15) & 1) == 1;
            }
            var auxiliary = ((value1 ^ value2 ^ truncResult) & 0x10) != 0;
            var zero = truncResult == 0;
            var parity = parityLookup[(byte)result];
            var overflow = carry ^ sign;

            registers[FLAGS] &= unchecked((ushort)~(ushort)((1u << CF) | (1u << PF) | (1u << AF) | (1u << ZF) | (1u << SF) | (1u << OF)));
            registers[FLAGS] |= (ushort)(((carry ? 1 : 0) << CF) | ((parity ? 1 : 0) << PF) | ((auxiliary ? 1 : 0) << AF) | ((zero ? 1 : 0) << ZF) | ((sign ? 1 : 0) << SF) | ((overflow ? 1 : 0) << OF));

            registers[AX] = (ushort)result;
            if (!instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                registers[DX] = (ushort)(result >> 16);
        }
        private void ProcessExchange(OpCodeManager.Instruction instruction)
        {
            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    registers[instruction.Argument1] = ProcessExchangeSecond(instruction, registers[instruction.Argument1]);
                    break;

                case OpCodeManager.ARG_BYTE_REGISTER:
                    SetByteRegister(instruction.Argument1Value, (byte)ProcessExchangeSecond(instruction, GetByteRegister(instruction.Argument1Value)));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private ushort ProcessExchangeSecond(OpCodeManager.Instruction instruction, ushort value)
        {
            ushort tmp;
            switch (instruction.Argument2)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    tmp = registers[instruction.Argument2];
                    registers[instruction.Argument2] = value;
                    return tmp;

                case OpCodeManager.ARG_BYTE_REGISTER:
                    tmp = GetByteRegister(instruction.Argument2Value);
                    SetByteRegister(instruction.Argument2Value, (byte)value);
                    return tmp;

                default:
                    throw new NotImplementedException();
            }
        }
        private void Push(ushort value)
        {
            registers[SP] -= 2;
            WriteU16(SegmentToAddress(registers[SS], registers[SP]), value);
        }
        private ushort Pop()
        {
            var value = ReadU16(SegmentToAddress(registers[SS], registers[SP]));
            registers[SP] += 2;
            return value;
        }
        private void ProcessStringOperation(OpCodeManager.Instruction instruction)
        {
            int counter;
            switch (instruction.Prefix)
            {
                case 0:
                    ProcessOneStringOperation(instruction);
                    break;
                case 0xF2:
                    counter = registers[CX];
                    while (counter != 0)
                    {
                        ProcessOneStringOperation(instruction);
                        counter--;
                        if (((registers[FLAGS] >> ZF) & 1) != 0)
                            break;
                    }
                    break;
                case 0xF3:
                    counter = registers[CX];
                    if (instruction.Type == OpCodeManager.InstructionType.Cmps || instruction.Type == OpCodeManager.InstructionType.Scas)
                    {
                        while (counter != 0)
                        {
                            ProcessOneStringOperation(instruction);
                            counter--;
                            if (((registers[FLAGS] >> ZF) & 1) == 0)
                                break;
                        }
                    }
                    else
                    {
                        while (counter != 0)
                        {
                            ProcessOneStringOperation(instruction);
                            counter--;
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessOneStringOperation(OpCodeManager.Instruction instruction)
        {
            switch (instruction.Type)
            {
                case OpCodeManager.InstructionType.Cmps:
                    ProcessCmps(instruction);
                    break;
                case OpCodeManager.InstructionType.Lods:
                    ProcessLods(instruction);
                    break;
                case OpCodeManager.InstructionType.Movs:
                    ProcessMovs(instruction);
                    break;
                case OpCodeManager.InstructionType.Stos:
                    ProcessStos(instruction);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessCmps(OpCodeManager.Instruction instruction)
        {
            ushort value1;
            ushort value2;
            byte size;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                value1 = ReadU8(SegmentToAddress(registers[DS], registers[SI]));
                value2 = ReadU8(SegmentToAddress(registers[ES], registers[DI]));
                size = 1;
            }
            else
            {
                value1 = ReadU16(SegmentToAddress(registers[DS], registers[SI]));
                value2 = ReadU16(SegmentToAddress(registers[ES], registers[DI]));
                size = 2;
            }
            var result = value1 - value2;

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                truncResult = (byte)result;
                carry = (uint)result > 0xFF;
                sign = ((truncResult >> 7) & 1) == 1;
            }
            else
            {
                truncResult = (ushort)result;
                carry = (uint)result > 0xFFFF;
                sign = ((truncResult >> 15) & 1) == 1;
            }
            var auxiliary = ((value1 ^ value2 ^ truncResult) & 0x10) != 0;
            var zero = truncResult == 0;
            var parity = parityLookup[(byte)result];
            var overflow = carry ^ sign;

            registers[FLAGS] &= unchecked((ushort)~(ushort)((1u << CF) | (1u << PF) | (1u << AF) | (1u << ZF) | (1u << SF) | (1u << OF)));
            registers[FLAGS] |= (ushort)(((carry ? 1 : 0) << CF) | ((parity ? 1 : 0) << PF) | ((auxiliary ? 1 : 0) << AF) | ((zero ? 1 : 0) << ZF) | ((sign ? 1 : 0) << SF) | ((overflow ? 1 : 0) << OF));

            if ((registers[FLAGS] & (1 << DF)) == 0)
            {
                registers[DI] += size;
                registers[SI] += size;
            }
            else
            {
                registers[DI] -= size;
                registers[SI] -= size;
            }
        }
        private void ProcessStos(OpCodeManager.Instruction instruction)
        {
            byte size;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                WriteU8(SegmentToAddress(registers[ES], registers[DI]), (byte)(registers[AX] & 0xFF));
                size = 1;
            }
            else
            {
                WriteU16(SegmentToAddress(registers[ES], registers[DI]), registers[AX]);
                size = 2;
            }

            if ((registers[FLAGS] & (1 << DF)) == 0)
                registers[DI] += size;
            else registers[DI] -= size;
        }
        private void ProcessLods(OpCodeManager.Instruction instruction)
        {
            int prefix = instruction.Prefix;
            if (prefix == 0) prefix = DS;
            var sourceAddress = SegmentToAddress(registers[prefix], registers[SI]);

            byte size;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                var value = ReadU8(sourceAddress);
                SetByteRegister(AL, value);
                size = 1;
            }
            else
            {
                var value = ReadU16(sourceAddress);
                registers[AX] = value;
                size = 2;
            }

            if ((registers[FLAGS] & (1 << DF)) == 0)
            {
                registers[DI] += size;
                registers[SI] += size;
            }
            else
            {
                registers[DI] -= size;
                registers[SI] -= size;
            }
        }
        private void ProcessMovs(OpCodeManager.Instruction instruction)
        {
            var sourceAddress = SegmentToAddress(registers[DS], registers[SI]);
            var destAddress = SegmentToAddress(registers[ES], registers[DI]);
            byte size;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                var value = ReadU8(sourceAddress);
                WriteU8(destAddress, value);
                size = 1;
            }
            else
            {
                var value = ReadU16(sourceAddress);
                WriteU16(destAddress, value);
                size = 2;
            }

            if ((registers[FLAGS] & (1 << DF)) == 0)
            {
                registers[DI] += size;
                registers[SI] += size;
            }
            else
            {
                registers[DI] -= size;
                registers[SI] -= size;
            }
        }
        private void ProcessUnaryArithmetic(OpCodeManager.Instruction instruction)
        {
            ushort value;
            int result;

            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    value = registers[instruction.Argument1];
                    if (instruction.Type == OpCodeManager.InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);
                    registers[instruction.Argument1] = (ushort)result;
                    break;

                case OpCodeManager.ARG_BYTE_REGISTER:
                    value = GetByteRegister(instruction.Argument1Value);

                    if (instruction.Type == OpCodeManager.InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);

                    SetByteRegister(instruction.Argument1Value, (byte)result);
                    break;

                case OpCodeManager.ARG_MEMORY:
                    var segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    var address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    value = instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? ReadU8(address) : ReadU16(address);
                    if (instruction.Type == OpCodeManager.InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);
                    if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                        WriteU8(address, (byte)result);
                    else WriteU16(address, (ushort)result);
                    break;

                default:
                    throw new NotImplementedException();
            }

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                truncResult = (byte)result;
                carry = (uint)result > 0xFF;
                sign = ((truncResult >> 7) & 1) == 1;
            }
            else
            {
                truncResult = (ushort)result;
                carry = (uint)result > 0xFFFF;
                sign = ((truncResult >> 15) & 1) == 1;
            }
            var auxiliary = ((value ^ 1 ^ truncResult) & 0x10) != 0;
            var zero = truncResult == 0;
            var parity = parityLookup[(byte)result];
            var overflow = carry ^ sign;

            registers[FLAGS] &= unchecked((ushort)~(ushort)((1u << PF) | (1u << AF) | (1u << ZF) | (1u << SF) | (1u << OF)));
            registers[FLAGS] |= (ushort)(((parity ? 1 : 0) << PF) | ((auxiliary ? 1 : 0) << AF) | ((zero ? 1 : 0) << ZF) | ((sign ? 1 : 0) << SF) | ((overflow ? 1 : 0) << OF));
        }
        private void ProcessArithmetic(OpCodeManager.Instruction instruction)
        {
            ushort value1;
            var value2 = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

            byte segment;
            uint address;
            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    value1 = registers[instruction.Argument1];
                    break;

                case OpCodeManager.ARG_BYTE_REGISTER:
                    value1 = GetByteRegister(instruction.Argument1Value);
                    break;

                case OpCodeManager.ARG_DEREFERENCE:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    switch (instruction.Argument1Value)
                    {
                        case 0:
                            address = (uint)registers[BX] + registers[SI];
                            break;
                        case 1:
                            address = (uint)registers[BX] + registers[DI];
                            break;
                        case 2:
                            address = (uint)registers[BP] + registers[SI];
                            break;
                        case 3:
                            address = (uint)registers[BP] + registers[DI];
                            break;
                        case 4:
                            address = registers[SI];
                            break;
                        case 5:
                            address = registers[DI];
                            break;
                        case 6:
                            address = registers[BP];
                            break;
                        case 7:
                            address = registers[BX];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    address = SegmentToAddress(registers[segment], (ushort)((ushort)address + instruction.Argument1Displacement));
                    value1 = instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? ReadU8(address) : ReadU16(address);
                    break;

                case OpCodeManager.ARG_MEMORY:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    value1 = instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? ReadU8(address) : ReadU16(address);
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                value1 &= 0xFF;
                value2 &= 0xFF;
            }

            int result;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Signed))
            {
                var value3 = (sbyte)(byte)value2;
                switch (instruction.Type)
                {
                    case OpCodeManager.InstructionType.Adc:
                        result = value1 + value3 + ((registers[FLAGS] >> CF) & 1);
                        break;

                    case OpCodeManager.InstructionType.Add:
                        result = value1 + value3;
                        break;

                    case OpCodeManager.InstructionType.Compare:
                    case OpCodeManager.InstructionType.Subtract:
                        result = value1 - value3;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (instruction.Type)
                {
                    case OpCodeManager.InstructionType.Adc:
                        result = value1 + value2 + ((registers[FLAGS] >> CF) & 1);
                        break;

                    case OpCodeManager.InstructionType.Add:
                        result = value1 + value2;
                        break;

                    case OpCodeManager.InstructionType.And:
                    case OpCodeManager.InstructionType.Test:
                        result = value1 & value2;
                        break;

                    case OpCodeManager.InstructionType.Compare:
                    case OpCodeManager.InstructionType.Subtract:
                        result = value1 - value2;
                        break;

                    case OpCodeManager.InstructionType.Or:
                        result = value1 | value2;
                        break;

                    case OpCodeManager.InstructionType.Ror:
                        if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                        {
                            const int mask = sizeof(byte) - 1;
                            var shift = value2 & mask;
                            result = (byte)(value1 >> shift) | (byte)(value1 << (-shift & mask));
                        }
                        else
                        {
                            const int mask = sizeof(ushort) - 1;
                            var shift = value2 & mask;
                            result = (ushort)(value1 >> shift) | (ushort)(value1 << (-shift & mask));
                        }
                        break;

                    case OpCodeManager.InstructionType.Shl:
                        result = value1 << value2;
                        break;

                    case OpCodeManager.InstructionType.Shr:
                        result = value1 >> value2;
                        break;

                    case OpCodeManager.InstructionType.Xor:
                        result = value1 ^ value2;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
            {
                truncResult = (byte)result;
                carry = (uint)result > 0xFF;
                sign = ((truncResult >> 7) & 1) == 1;
            }
            else
            {
                truncResult = (ushort)result;
                carry = (uint)result > 0xFFFF;
                sign = ((truncResult >> 15) & 1) == 1;
            }
            var auxiliary = ((value1 ^ value2 ^ truncResult) & 0x10) != 0;
            var zero = truncResult == 0;
            var parity = parityLookup[(byte)result];
            var overflow = carry ^ sign;

            registers[FLAGS] &= unchecked((ushort)~(ushort)((1u << CF) | (1u << PF) | (1u << AF) | (1u << ZF) | (1u << SF) | (1u << OF)));
            registers[FLAGS] |= (ushort)(((carry ? 1 : 0) << CF) | ((parity ? 1 : 0) << PF) | ((auxiliary ? 1 : 0) << AF) | ((zero ? 1 : 0) << ZF) | ((sign ? 1 : 0) << SF) | ((overflow ? 1 : 0) << OF));

            if (instruction.Type != OpCodeManager.InstructionType.Compare && instruction.Type != OpCodeManager.InstructionType.Test)
            {
                switch (instruction.Argument1)
                {
                    case AX:
                    case CX:
                    case DX:
                    case BX:
                    case SP:
                    case BP:
                    case SI:
                    case DI:
                    case IP:
                    case CS:
                    case DS:
                    case ES:
                    case SS:
                        registers[instruction.Argument1] = truncResult;
                        break;

                    case OpCodeManager.ARG_BYTE_REGISTER:
                        SetByteRegister(instruction.Argument1Value, (byte)truncResult);
                        break;

                    case OpCodeManager.ARG_DEREFERENCE:
                        segment = instruction.Prefix;
                        if (segment == 0) segment = DS;
                        switch (instruction.Argument1Value)
                        {
                            case 0:
                                address = (uint)registers[BX] + registers[SI];
                                break;
                            case 1:
                                address = (uint)registers[BX] + registers[DI];
                                break;
                            case 2:
                                address = (uint)registers[BP] + registers[SI];
                                break;
                            case 3:
                                address = (uint)registers[BP] + registers[DI];
                                break;
                            case 4:
                                address = registers[SI];
                                break;
                            case 5:
                                address = registers[DI];
                                break;
                            case 6:
                                address = registers[BP];
                                break;
                            case 7:
                                address = registers[BX];
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        address = SegmentToAddress(registers[segment], (ushort)((ushort)address + instruction.Argument1Displacement));
                        if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                            WriteU8(address, (byte)truncResult);
                        else WriteU16(address, truncResult);
                        break;

                    case OpCodeManager.ARG_MEMORY:
                        segment = instruction.Prefix;
                        if (segment == 0) segment = DS;
                        address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                        if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                            WriteU8(address, (byte)truncResult);
                        else WriteU16(address, truncResult);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ProcessLea(OpCodeManager.Instruction instruction)
        {
            var address = GetInstructionAddress(instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    registers[instruction.Argument1] = address;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessMov(OpCodeManager.Instruction instruction)
        {
            var value = GetInstructionValue(instruction.Flag, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

            byte segment;
            uint address;
            switch (instruction.Argument1)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    registers[instruction.Argument1] = value;
                    break;

                case OpCodeManager.ARG_BYTE_REGISTER:
                    SetByteRegister(instruction.Argument1Value, (byte)value);
                    break;

                case OpCodeManager.ARG_DEREFERENCE:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    switch (instruction.Argument1Value)
                    {
                        case 0:
                            address = (uint)registers[BX] + registers[SI];
                            break;
                        case 1:
                            address = (uint)registers[BX] + registers[DI];
                            break;
                        case 2:
                            address = (uint)registers[BP] + registers[SI];
                            break;
                        case 3:
                            address = (uint)registers[BP] + registers[DI];
                            break;
                        case 4:
                            address = registers[SI];
                            break;
                        case 5:
                            address = registers[DI];
                            break;
                        case 6:
                            address = registers[BP];
                            break;
                        case 7:
                            address = registers[BX];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    address = SegmentToAddress(registers[segment], (ushort)((ushort)address + instruction.Argument1Displacement));
                    if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                        WriteU8(address, (byte)value);
                    else WriteU16(address, value);
                    break;

                case OpCodeManager.ARG_MEMORY:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    if (instruction.Flag.HasFlag(OpCodeManager.OpCodeFlag.Size8))
                        WriteU8(address, (byte)value);
                    else WriteU16(address, value);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private ushort GetInstructionAddress(int instruction, int instructionValue, int instructionDisplacement)
        {
            switch (instruction)
            {
                case OpCodeManager.ARG_DEREFERENCE:
                    ushort address;
                    switch (instructionValue)
                    {
                        case 0:
                            address = (ushort)(registers[BX] + registers[SI]);
                            break;
                        case 1:
                            address = (ushort)(registers[BX] + registers[DI]);
                            break;
                        case 2:
                            address = (ushort)(registers[BP] + registers[SI]);
                            break;
                        case 3:
                            address = (ushort)(registers[BP] + registers[DI]);
                            break;
                        case 4:
                            address = registers[SI];
                            break;
                        case 5:
                            address = registers[DI];
                            break;
                        case 6:
                            address = registers[BP];
                            break;
                        case 7:
                            address = registers[BX];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    return (ushort)(address + instructionDisplacement);

                case OpCodeManager.ARG_MEMORY:
                    return (ushort)instructionValue;

                default:
                    throw new NotImplementedException();
            }
        }
        private ushort GetInstructionValue(OpCodeManager.OpCodeFlag flag, byte prefix, int instruction, int instructionValue, int instructionDisplacement)
        {
            byte segment;
            uint address;
            switch (instruction)
            {
                case AX:
                case CX:
                case DX:
                case BX:
                case SP:
                case BP:
                case SI:
                case DI:
                case IP:
                case CS:
                case DS:
                case ES:
                case SS:
                    return registers[instruction];

                case OpCodeManager.ARG_BYTE_REGISTER:
                    return GetByteRegister(instructionValue);

                case OpCodeManager.ARG_DEREFERENCE:
                    segment = prefix;
                    if (segment == 0) segment = DS;
                    switch (instructionValue)
                    {
                        case 0:
                            address = (uint)registers[BX] + registers[SI];
                            break;
                        case 1:
                            address = (uint)registers[BX] + registers[DI];
                            break;
                        case 2:
                            address = (uint)registers[BP] + registers[SI];
                            break;
                        case 3:
                            address = (uint)registers[BP] + registers[DI];
                            break;
                        case 4:
                            address = registers[SI];
                            break;
                        case 5:
                            address = registers[DI];
                            break;
                        case 6:
                            address = registers[BP];
                            break;
                        case 7:
                            address = registers[BX];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    address = SegmentToAddress(registers[segment], (ushort)((ushort)address + instructionDisplacement));
                    return flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? ReadU8(address) : ReadU16(address);

                case OpCodeManager.ARG_MEMORY:
                    segment = prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instructionValue);
                    return flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? ReadU8(address) : ReadU16(address);

                case OpCodeManager.ARG_CONSTANT:
                    return (ushort)instructionValue;

                default:
                    throw new NotImplementedException();
            }
        }

        private static string OutputInstruction(OpCodeManager.Instruction instruction)
        {
            var output = instruction.Type.ToString();
            var arg1 = OutputArgument(instruction.Prefix, instruction.Flag, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);
            var arg2 = OutputArgument(instruction.Prefix, instruction.Flag, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

            if (arg1 == null)
                return output;
            return arg2 == null ? $"{output} {arg1}" : $"{output} {arg1}, {arg2}";
        }

        private static string OutputArgument(int prefix, OpCodeManager.OpCodeFlag flag, int argument, int argumentValue, int argumentDisplacement)
        {
            if (argument == OpCodeManager.ARG_NONE)
                return null;
            switch (argument)
            {
                case AX:
                    return "AX";
                case CX:
                    return "CX";
                case DX:
                    return "DX";
                case BX:
                    return "BX";
                case SP:
                    return "SP";
                case BP:
                    return "BP";
                case SI:
                    return "SI";
                case DI:
                    return "DI";
                case IP:
                    return "IP";
                case CS:
                    return "CS";
                case DS:
                    return "DS";
                case ES:
                    return "ES";
                case SS:
                    return "SS";
                case FLAGS:
                    return "FLAGS";

                case OpCodeManager.ARG_BYTE_REGISTER:
                    switch (argumentValue)
                    {
                        case AL:
                            return "AL";
                        case CL:
                            return "CL";
                        case DL:
                            return "DL";
                        case BL:
                            return "BL";
                        case AH:
                            return "AH";
                        case CH:
                            return "CH";
                        case DH:
                            return "DH";
                        case BH:
                            return "BH";
                        default:
                            throw new NotImplementedException();
                    }
                case OpCodeManager.ARG_DEREFERENCE:
                    string value;
                    switch (argumentValue)
                    {
                        case 0:
                            value = "BX+SI";
                            break;
                        case 1:
                            value = "BX+DI";
                            break;
                        case 2:
                            value = "BP+SI";
                            break;
                        case 3:
                            value = "BP+DI";
                            break;
                        case 4:
                            value = "SI";
                            break;
                        case 5:
                            value = "DI";
                            break;
                        case 6:
                            value = "BP";
                            break;
                        case 7:
                            value = "BX";
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    switch (prefix)
                    {
                        case 0:
                            return argumentDisplacement < 0 ? $"[{value}{argumentDisplacement}]" : $"[{value}+{argumentDisplacement}]";
                        case ES:
                            return argumentDisplacement < 0 ? $"[ES:{value}{argumentDisplacement}]" : $"[ES:{value}+{argumentDisplacement}]";
                        case CS:
                            return argumentDisplacement < 0 ? $"[CS:{value}{argumentDisplacement}]" : $"[CS:{value}+{argumentDisplacement}]";
                        case SS:
                            return argumentDisplacement < 0 ? $"[SS:{value}{argumentDisplacement}]" : $"[SS:{value}+{argumentDisplacement}]";
                        case DS:
                            return argumentDisplacement < 0 ? $"[DS:{value}{argumentDisplacement}]" : $"[DS:{value}+{argumentDisplacement}]";
                        default:
                            throw new NotImplementedException();
                    }
                case OpCodeManager.ARG_MEMORY:
                    switch (prefix)
                    {
                        case 0:
                            return $"[{argumentValue:X4}]";
                        case ES:
                            return $"[ES:{argumentValue:X4}]";
                        case CS:
                            return $"[CS:{argumentValue:X4}]";
                        case SS:
                            return $"[SS:{argumentValue:X4}]";
                        case DS:
                            return $"[DS:{argumentValue:X4}]";
                        default:
                            throw new NotImplementedException();
                    }
                case OpCodeManager.ARG_FAR_MEMORY:
                    var segment = (uint)argumentValue >> 16;
                    var address = argumentValue & 0xFFFF;
                    return $"[{segment:X4}:{address:X4}]";
                case OpCodeManager.ARG_CONSTANT:
                    return flag.HasFlag(OpCodeManager.OpCodeFlag.Size8) ? $"{argumentValue:X2}" : $"{argumentValue:X4}";
                default:
                    throw new NotImplementedException();
            }
        }

        private static uint SegmentToAddress(ushort segment, ushort offset) => (uint)((segment << 4) + offset);

        public ushort GetRegister(int register) => registers[register];
        public void SetRegister(int register, ushort value) => registers[register] = value;
        public byte GetByteRegister(int register)
        {
            switch (register)
            {
                case AL:
                    return (byte)(registers[AX] & 0xFF);
                case CL:
                    return (byte)(registers[CX] & 0xFF);
                case DL:
                    return (byte)(registers[DX] & 0xFF);
                case BL:
                    return (byte)(registers[BX] & 0xFF);
                case AH:
                    return (byte)((registers[AX] >> 8) & 0xFF);
                case CH:
                    return (byte)((registers[CX] >> 8) & 0xFF);
                case DH:
                    return (byte)((registers[DX] >> 8) & 0xFF);
                case BH:
                    return (byte)((registers[BX] >> 8) & 0xFF);
                default:
                    throw new NotImplementedException();
            }
        }
        private void SetByteRegister(int register, byte value)
        {
            switch (register)
            {
                case AL:
                    registers[AX] = (ushort)((registers[AX] & 0xFF00) | value);
                    break;
                case CL:
                    registers[CX] = (ushort)((registers[CX] & 0xFF00) | value);
                    break;
                case DL:
                    registers[DX] = (ushort)((registers[DX] & 0xFF00) | value);
                    break;
                case BL:
                    registers[BX] = (ushort)((registers[BX] & 0xFF00) | value);
                    break;
                case AH:
                    registers[AX] = (ushort)((registers[AX] & 0x00FF) | (value << 8));
                    break;
                case CH:
                    registers[CX] = (ushort)((registers[CX] & 0x00FF) | (value << 8));
                    break;
                case DH:
                    registers[DX] = (ushort)((registers[DX] & 0x00FF) | (value << 8));
                    break;
                case BH:
                    registers[BX] = (ushort)((registers[BX] & 0x00FF) | (value << 8));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public byte ReadU8(uint address)
        {
            var pageNumber = address >> PAGE_SHIFT;
            return pages[pageNumber].ReadU8(address);
        }
        public ushort ReadU16(uint address) => (ushort)(ReadU8(address) | ReadU8(address + 1) << 8);
        public byte[] ReadBytes(uint address, uint size)
        {
            var buffer = new byte[size];
            for (var i = 0u; i < size; i++)
                buffer[i] = ReadU8(address + i);
            return buffer;
        }
        public void WriteU8(uint address, byte value)
        {
            var pageNumber = address >> PAGE_SHIFT;
            pages[pageNumber].WriteU8(address, value);
        }
        public void WriteU16(uint address, ushort value)
        {
            WriteU8(address, (byte)(value & 0xFF));
            WriteU8(address + 1, (byte)(value >> 8 & 0xFF));
        }
        public void WriteBytes(uint address, byte[] value)
        {
            for (var i = 0u; i < value.Length; i++)
                WriteU8(address + i, value[i]);
        }

        byte IInstructionFetcher.FetchU8()
        {
            var value = ReadU8(SegmentToAddress(registers[CS], registers[IP]));
            registers[IP] += 1;
            return value;
        }
        ushort IInstructionFetcher.FetchU16()
        {
            var value = ReadU16(SegmentToAddress(registers[CS], registers[IP]));
            registers[IP] += 2;
            return value;
        }
    }
}
