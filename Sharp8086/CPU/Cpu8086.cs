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
    public sealed class Cpu8086 : ICpu
    {
        [Flags]
        private enum OpcodeFlag : byte
        {
            None = 0,
            HasRM = 1 << 0,
            Signed = 1 << 1
        }

        private enum InstructionType
        {
            Invalid,

            Add,
            Push,
            Pop,
            Or,
            Adc,
            Sbb,
            And,
            Subtract,
            Xor,
            Compare,
            Prefix,
            Daa,
            Das,
            Aaa,
            Aas,
            Increment,
            Decrement,
            Jump,
            FarJump,
            Group,
            Test,
            Xchg,
            Move,
            Lea,
            Nop,
            Cbw,
            Cwd,
            CallNearRelative,
            CallNear,
            CallFar,
            Wait,
            Sahf,
            Lahf,
            Movs,
            Cmps,
            Stos,
            Lods,
            Scas,
            ReturnNear,
            ReturnFar,
            Les,
            Lds,
            Int,
            Into,
            ReturnInterrupt,
            Aam,
            Aad,
            Xlat,
            Loopnz,
            Loopz,
            Loop,
            Clc,
            Stc,
            Jcxz,
            In,
            Out,
            Hlt,
            Cmc,
            Cli,
            Sti,
            Cld,
            Std,
            JO,
            JNO,
            JB,
            JNB,
            JZ,
            JNZ,
            JBE,
            JA,
            JS,
            JNS,
            JPE,
            JPO,
            JL,
            JGE,
            JLE,
            JG,
            Sar,
            Shr,
            Shl,
            Rcr,
            Rcl,
            Ror,
            Rol,
            Not,
            Negate,
            Multiply,
            IMultiply,
            Divide,
            IDivide,
            EmulatorSpecial
        }

        private static readonly InstructionType[] typeLookup =
        {
            /*0x00*/ InstructionType.Add, InstructionType.Add, InstructionType.Add, InstructionType.Add, InstructionType.Add, InstructionType.Add, InstructionType.Push, InstructionType.Pop,
            /*0x08*/ InstructionType.Or, InstructionType.Or, InstructionType.Or, InstructionType.Or, InstructionType.Or, InstructionType.Or, InstructionType.Push, InstructionType.EmulatorSpecial,
            /*0x10*/ InstructionType.Adc, InstructionType.Adc, InstructionType.Adc, InstructionType.Adc, InstructionType.Adc, InstructionType.Adc, InstructionType.Push, InstructionType.Pop,
            /*0x18*/ InstructionType.Sbb, InstructionType.Sbb, InstructionType.Sbb, InstructionType.Sbb, InstructionType.Sbb, InstructionType.Sbb, InstructionType.Push, InstructionType.Pop,
            /*0x20*/ InstructionType.And, InstructionType.And, InstructionType.And, InstructionType.And, InstructionType.And, InstructionType.And, InstructionType.Prefix, InstructionType.Daa,
            /*0x28*/ InstructionType.Subtract, InstructionType.Subtract, InstructionType.Subtract, InstructionType.Subtract, InstructionType.Subtract, InstructionType.Subtract, InstructionType.Prefix, InstructionType.Das,
            /*0x30*/ InstructionType.Xor, InstructionType.Xor, InstructionType.Xor, InstructionType.Xor, InstructionType.Xor, InstructionType.Xor, InstructionType.Prefix, InstructionType.Aaa,
            /*0x38*/ InstructionType.Compare, InstructionType.Compare, InstructionType.Compare, InstructionType.Compare, InstructionType.Compare, InstructionType.Compare, InstructionType.Prefix, InstructionType.Aas,
            /*0x40*/ InstructionType.Increment, InstructionType.Increment, InstructionType.Increment, InstructionType.Increment, InstructionType.Increment, InstructionType.Increment, InstructionType.Increment, InstructionType.Increment,
            /*0x48*/ InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement, InstructionType.Decrement,
            /*0x50*/ InstructionType.Push, InstructionType.Push, InstructionType.Push, InstructionType.Push, InstructionType.Push, InstructionType.Push, InstructionType.Push, InstructionType.Push,
            /*0x58*/ InstructionType.Pop, InstructionType.Pop, InstructionType.Pop, InstructionType.Pop, InstructionType.Pop, InstructionType.Pop, InstructionType.Pop, InstructionType.Pop,
            /*0x60*/ InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid,
            /*0x68*/ InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid,
            /*0x70*/ InstructionType.JO, InstructionType.JNO, InstructionType.JB, InstructionType.JNB, InstructionType.JZ, InstructionType.JNZ, InstructionType.JBE, InstructionType.JA,
            /*0x78*/ InstructionType.JS, InstructionType.JNS, InstructionType.JPE, InstructionType.JPO, InstructionType.JL, InstructionType.JGE, InstructionType.JLE, InstructionType.JG,
            /*0x80*/ InstructionType.Group, InstructionType.Group, InstructionType.Group, InstructionType.Group, InstructionType.Test, InstructionType.Test, InstructionType.Xchg, InstructionType.Xchg,
            /*0x88*/ InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Lea, InstructionType.Move, InstructionType.Pop,
            /*0x90*/ InstructionType.Nop, InstructionType.Xchg, InstructionType.Xchg, InstructionType.Xchg, InstructionType.Xchg, InstructionType.Xchg, InstructionType.Xchg, InstructionType.Xchg,
            /*0x98*/ InstructionType.Cbw, InstructionType.Cwd, InstructionType.CallFar, InstructionType.Wait, InstructionType.Push, InstructionType.Pop, InstructionType.Sahf, InstructionType.Lahf,
            /*0xA0*/ InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Movs, InstructionType.Movs, InstructionType.Cmps, InstructionType.Cmps,
            /*0xA8*/ InstructionType.Test, InstructionType.Test, InstructionType.Stos, InstructionType.Stos, InstructionType.Lods, InstructionType.Lods, InstructionType.Scas, InstructionType.Scas,
            /*0xB0*/ InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move,
            /*0xB8*/ InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move, InstructionType.Move,
            /*0xC0*/ InstructionType.Group, InstructionType.Group, InstructionType.ReturnNear, InstructionType.ReturnNear, InstructionType.Les, InstructionType.Lds, InstructionType.Move, InstructionType.Move,
            /*0xC8*/ InstructionType.Invalid, InstructionType.Invalid, InstructionType.ReturnFar, InstructionType.ReturnFar, InstructionType.Int, InstructionType.Int, InstructionType.Into, InstructionType.ReturnInterrupt,
            /*0xD0*/ InstructionType.Group, InstructionType.Group, InstructionType.Group, InstructionType.Group, InstructionType.Aam, InstructionType.Aad, InstructionType.Invalid, InstructionType.Xlat,
            /*0xD8*/ InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid,
            /*0xE0*/ InstructionType.Loopnz, InstructionType.Loopz, InstructionType.Loop, InstructionType.Jcxz, InstructionType.In, InstructionType.In, InstructionType.Out, InstructionType.Out,
            /*0xE8*/ InstructionType.CallNearRelative, InstructionType.Jump, InstructionType.FarJump, InstructionType.Jump, InstructionType.In, InstructionType.In, InstructionType.Out, InstructionType.Out,
            /*0xF0*/ InstructionType.Prefix, InstructionType.Invalid, InstructionType.Prefix, InstructionType.Prefix, InstructionType.Hlt, InstructionType.Cmc, InstructionType.Group, InstructionType.Group,
            /*0xF8*/ InstructionType.Clc, InstructionType.Stc, InstructionType.Cli, InstructionType.Sti, InstructionType.Cld, InstructionType.Std, InstructionType.Group, InstructionType.Group
        };

        private static readonly InstructionType[] opcodeExtension80 =
        {
            InstructionType.Add, InstructionType.Or, InstructionType.Adc, InstructionType.Sbb, InstructionType.And, InstructionType.Subtract, InstructionType.Xor, InstructionType.Compare
        };

        private static readonly InstructionType[] opcodeExtensionC0 =
        {
            InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Shl, InstructionType.Shr, InstructionType.Invalid, InstructionType.Invalid
        };

        private static readonly InstructionType[] opcodeExtensionC1 =
        {
            InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Shl, InstructionType.Shr, InstructionType.Invalid, InstructionType.Invalid
        };

        private static readonly InstructionType[] opcodeExtensionD0 =
        {
            InstructionType.Rol, InstructionType.Ror, InstructionType.Rcl, InstructionType.Rcr, InstructionType.Shl, InstructionType.Shr, InstructionType.Invalid, InstructionType.Sar
        };

        private static readonly InstructionType[] opcodeExtensionF6 =
        {
            InstructionType.Test, InstructionType.Invalid, InstructionType.Not, InstructionType.Negate, InstructionType.Multiply, InstructionType.IMultiply, InstructionType.Divide, InstructionType.IDivide
        };

        private static readonly InstructionType[] opcodeExtensionFE =
        {
            InstructionType.Increment, InstructionType.Decrement, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid
        };

        private static readonly InstructionType[] opcodeExtensionFF =
        {
            InstructionType.Increment, InstructionType.Decrement, InstructionType.CallNear, InstructionType.CallFar, InstructionType.Jump, InstructionType.FarJump, InstructionType.Push, InstructionType.Invalid
        };

        private static readonly OpcodeFlag[] opcodeFlag =
        {
            /*0x00*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x10*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x20*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x30*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x40*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x50*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x60*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x70*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0x80*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM | OpcodeFlag.Signed, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM,
            /*0x90*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xA0*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xB0*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xC0*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xD0*/ OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xE0*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None,
            /*0xF0*/ OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.None, OpcodeFlag.HasRM, OpcodeFlag.HasRM
        };

        private static readonly byte[] opcodeSize =
        {
            /*0x00*/ 1, 2, 1, 2, 1, 2, 2, 2, 1, 2, 1, 2, 1, 2, 2, 2,
            /*0x10*/ 1, 2, 1, 2, 1, 2, 2, 2, 1, 2, 1, 2, 1, 2, 2, 2,
            /*0x20*/ 1, 2, 1, 2, 1, 2, 2, 2, 1, 2, 1, 2, 1, 2, 2, 2,
            /*0x30*/ 1, 2, 1, 2, 1, 2, 2, 2, 1, 2, 1, 2, 1, 2, 2, 2,
            /*0x40*/ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            /*0x50*/ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            /*0x60*/ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            /*0x70*/ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            /*0x80*/ 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 2, 2, 2, 2,
            /*0x90*/ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            /*0xA0*/ 2, 2, 1, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 2,
            /*0xB0*/ 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
            /*0xC0*/ 1, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2,
            /*0xD0*/ 1, 2, 1, 2, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2,
            /*0xE0*/ 1, 1, 1, 1, 1, 2, 1, 2, 2, 2, 2, 1, 1, 2, 1, 2,
            /*0xF0*/ 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2
        };

        private const int ARG_A = 0xFFF0;
        private const int ARG_EB = 0xFFF1;
        private const int ARG_EW = 0xFFF2;
        private const int ARG_GB = 0xFFF3;
        private const int ARG_GW = 0xFFF4;
        private const int ARG_IB = 0xFFF5;
        private const int ARG_IW = 0xFFF6;
        private const int ARG_JB = 0xFFF7;
        private const int ARG_JW = 0xFFF8;
        private const int ARG_M = 0xFFF9;
        private const int ARG_OB = 0xFFFA;
        private const int ARG_OW = 0xFFFB;
        private const int ARG_S = 0xFFFC;
        private const int ARG_1 = 0xFFFD;
        private const int ARG_3 = 0xFFFE;
        private const int ARG_BYTE_REGISTER = -6;
        private const int ARG_DEREFERENCE = -5;
        private const int ARG_FAR_MEMORY = -4;
        private const int ARG_MEMORY = -3;
        private const int ARG_CONSTANT = -2;
        private const int ARG_NONE = -1;

        private static readonly int[] argument1Map =
        {
            /*0x00*/ ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, ES, ES, ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, CS, ARG_IB,
            /*0x10*/ ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, SS, SS, ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, DS, DS,
            /*0x20*/ ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, ARG_NONE, ARG_NONE, ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, ARG_NONE, ARG_NONE,
            /*0x30*/ ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, ARG_NONE, ARG_NONE, ARG_EB, ARG_EW, ARG_GB, ARG_GW, AL, AX, ARG_NONE, ARG_NONE,
            /*0x40*/ AX, CX, DX, BX, SP, BP, SI, DI, AX, CX, DX, BX, SP, BP, SI, DI,
            /*0x50*/ AX, CX, DX, BX, SP, BP, SI, DI, AX, CX, DX, BX, SP, BP, SI, DI,
            /*0x60*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0x70*/ ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB, ARG_JB,
            /*0x80*/ ARG_EB, ARG_EW, ARG_EB, ARG_EW, ARG_GB, ARG_GW, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_GB, ARG_GW, ARG_EW, ARG_GW, ARG_S, ARG_EW,
            /*0x90*/ ARG_NONE, CX, DX, BX, SP, BP, SI, DI, ARG_NONE, ARG_NONE, ARG_A, ARG_NONE, FLAGS, FLAGS, ARG_NONE, ARG_NONE,
            /*0xA0*/ AL, AX, ARG_OB, ARG_OW, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, AL, AX, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xB0*/ AL, CL,DL,BL,AH, CH,DH,BH,AX, CX, DX, BX, SP, BP, SI, DI,
            /*0xC0*/ ARG_EB, ARG_EW, ARG_IW, ARG_NONE, ARG_GW, ARG_GW, ARG_EB, ARG_EW, ARG_NONE, ARG_NONE, ARG_IW, ARG_NONE, ARG_3, ARG_IB, ARG_NONE, ARG_NONE,
            /*0xD0*/ ARG_EB, ARG_EW, ARG_EB, ARG_EW, ARG_IB, ARG_IB, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xE0*/ ARG_JB, ARG_JB, ARG_JB, ARG_JB, AL, AX, ARG_IB, ARG_IB, ARG_JW, ARG_JW, ARG_A, ARG_JB, AL, AX, DX, DX,
            /*0xF0*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_EB, ARG_EW, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_EB, ARG_EW
        };

        private static readonly int[] argument2Map =
        {
            /*0x00*/ ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE,
            /*0x10*/ ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE,
            /*0x20*/ ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE,
            /*0x30*/ ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE,
            /*0x40*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0x50*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0x60*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0x70*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0x80*/ ARG_IB, ARG_IW, ARG_IB, ARG_IB, ARG_EB, ARG_EW, ARG_EB, ARG_EW, ARG_GB, ARG_GW, ARG_EB, ARG_EW, ARG_S, ARG_M, ARG_EW, ARG_EW,
            /*0x90*/ ARG_NONE, AX, AX, AX, AX, AX, AX, AX, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xA0*/ ARG_OB, ARG_OW, AL, AX, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xB0*/ ARG_IB,ARG_IB,ARG_IB,ARG_IB,ARG_IB,ARG_IB,ARG_IB,ARG_IB,ARG_IW,ARG_IW,ARG_IW,ARG_IW,ARG_IW,ARG_IW,ARG_IW,ARG_IW,
            /*0xC0*/ ARG_IB, ARG_IB, ARG_NONE, ARG_NONE, ARG_M, ARG_M, ARG_IB, ARG_IW, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xD0*/ ARG_1,ARG_1, CL, CL, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE,
            /*0xE0*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_IB, ARG_IB, AL, AX, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, DX, DX, AL, AX,
            /*0xF0*/ ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE, ARG_NONE
        };

        private static readonly bool[] parityLookup =
        {
            /*0x00*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0x10*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0x20*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0x30*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0x40*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0x50*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0x60*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0x70*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0x80*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0x90*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0xA0*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0xB0*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0xC0*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
            /*0xD0*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0xE0*/ false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
            /*0xF0*/ true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true
        };

        private struct Instruction
        {
            public byte Prefix;
            public byte OperationSize;
            public InstructionType Type;
            public OpcodeFlag Flag;

            public int Argument1;
            public int Argument1Value;
            public int Argument1Displacement;
            public int Argument2;
            public int Argument2Value;
            public int Argument2Displacement;
        }

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

        public const int CODE_SEGMENT = CS;
        public const int INSTRUCTION_POINTER = IP;

        private readonly IPageController[] pages;
        private readonly IDrive[] drives = new IDrive[0x100];
        private readonly ushort[] registers;

        public Cpu8086(Stream biosFile, uint memorySize)
        {
            Debug.Assert(typeLookup.Length == 256);
            Debug.Assert(opcodeFlag.Length == 256);
            Debug.Assert(opcodeSize.Length == 256);
            Debug.Assert(argument1Map.Length == 256);
            Debug.Assert(argument2Map.Length == 256);
            Debug.Assert(parityLookup.Length == 256);

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
            var instruction = DecodeInstruction();
            instructionText += OutputInstruction(instruction);

            if (registers[CS] != 0xF000)
                Console.WriteLine(instructionText);

            switch (instruction.Type)
            {
                case InstructionType.Clc:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << CF));
                    break;
                case InstructionType.Cld:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << DF));
                    break;
                case InstructionType.Cli:
                    registers[FLAGS] = (ushort)(registers[FLAGS] & ~(1 << IF));
                    break;
                case InstructionType.Stc:
                    registers[FLAGS] = (ushort)(registers[FLAGS] | (1 << CF));
                    break;
                case InstructionType.Sti:
                    registers[FLAGS] = (ushort)(registers[FLAGS] | (1 << IF));
                    break;
                case InstructionType.JA:
                    if (((registers[FLAGS] >> CF) & 1) == 0 && ((registers[FLAGS] >> ZF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JB:
                    if (((registers[FLAGS] >> CF) & 1) != 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JNB:
                    if (((registers[FLAGS] >> CF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JL:
                    if (((registers[FLAGS] >> SF) & 1) != ((registers[FLAGS] >> OF) & 1))
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JGE:
                    if (((registers[FLAGS] >> SF) & 1) == ((registers[FLAGS] >> OF) & 1))
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JNZ:
                    if (((registers[FLAGS] >> ZF) & 1) == 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.JZ:
                    if (((registers[FLAGS] >> ZF) & 1) != 0)
                        registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.Jump:
                    Debug.Assert(instruction.Argument1 == ARG_CONSTANT);
                    registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.FarJump:
                    ProcessFarJump(instruction);
                    break;
                case InstructionType.Move:
                    ProcessMov(instruction);
                    break;
                case InstructionType.Lea:
                    ProcessLea(instruction);
                    break;
                case InstructionType.Lds:
                    ProcessLoadFarPointer(DS, instruction);
                    break;
                case InstructionType.Les:
                    ProcessLoadFarPointer(ES, instruction);
                    break;
                case InstructionType.Out:
                    var port = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);
                    var value = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);
                    if (instruction.OperationSize == 1)
                        WriteU8((uint)(IO_PORT_OFFSET + port), (byte)value);
                    else WriteU16((uint)(IO_PORT_OFFSET + port), value);
                    break;
                case InstructionType.Pop:
                    registers[instruction.Argument1] = Pop();
                    break;
                case InstructionType.Push:
                    if (instruction.Argument1 == SP)
                    {
                        // 8086 has a bug where it pushes SP after it has been modified
                        registers[SP] -= 2;
                        WriteU16(SegmentToAddress(registers[SS], registers[SP]), registers[SP]);
                    }
                    else Push(registers[instruction.Argument1]);
                    break;
                case InstructionType.Cmps:
                case InstructionType.Lods:
                case InstructionType.Movs:
                case InstructionType.Stos:
                    ProcessStringOperation(instruction);
                    break;
                case InstructionType.Divide:
                    ProcessDivide(instruction);
                    break;
                case InstructionType.Multiply:
                    ProcessMultiply(instruction);
                    break;
                case InstructionType.Adc:
                case InstructionType.Add:
                case InstructionType.And:
                case InstructionType.Compare:
                case InstructionType.Or:
                case InstructionType.Ror:
                case InstructionType.Sar:
                case InstructionType.Shl:
                case InstructionType.Shr:
                case InstructionType.Subtract:
                case InstructionType.Test:
                case InstructionType.Xor:
                    ProcessArithmetic(instruction);
                    break;
                case InstructionType.Decrement:
                case InstructionType.Increment:
                    ProcessUnaryArithmetic(instruction);
                    break;
                case InstructionType.CallNearRelative:
                    Debug.Assert(instruction.Argument1 == ARG_CONSTANT);
                    Push(registers[IP]);
                    registers[IP] = (ushort)(registers[IP] + instruction.Argument1Value);
                    break;
                case InstructionType.ReturnNear:
                    registers[IP] = Pop();
                    if (instruction.Argument1 == ARG_CONSTANT)
                        registers[SP] = (ushort)(registers[SP] + instruction.Argument1Value);
                    else Debug.Assert(instruction.Argument1 == ARG_NONE);
                    break;
                case InstructionType.ReturnInterrupt:
                    registers[IP] = Pop();
                    registers[CS] = Pop();
                    registers[FLAGS] = Pop();
                    break;
                case InstructionType.Int:
                    Debug.Assert(instruction.Argument1 == ARG_CONSTANT);

                    if (instruction.Argument1Value == 3)
                        Debugger.Break();

                    Push(registers[FLAGS]);
                    Push(registers[CS]);
                    Push(registers[IP]);
                    registers[CS] = ReadU16((uint)instruction.Argument1Value * 4 + 2);
                    registers[IP] = ReadU16((uint)instruction.Argument1Value * 4);
                    break;
                case InstructionType.Xchg:
                    ProcessExchange(instruction);
                    break;
                case InstructionType.Cbw:
                    registers[AX] = (ushort)(sbyte)GetByteRegister(AL);
                    break;
                case InstructionType.EmulatorSpecial:
                    Debug.Assert(instruction.Argument1 == ARG_CONSTANT);
                    switch (instruction.Argument1Value)
                    {
                        case 0x02:
                            ProcessReadDrive();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case InstructionType.Hlt:
                    // Goes back one instruction so it halts again if process instruction is called
                    --registers[IP];
                    return false;
                default:
                    throw new NotImplementedException();
            }
            return true;
        }
        private void ProcessFarJump(Instruction instruction)
        {
            switch (instruction.Argument1)
            {
                case ARG_FAR_MEMORY:
                    registers[CS] = (ushort)((uint)instruction.Argument1Value >> 16);
                    registers[IP] = (ushort)(instruction.Argument1Value & 0xFFFF);
                    break;

                case ARG_MEMORY:
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
        private void ProcessLoadFarPointer(int segmentRegister, Instruction instruction)
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
        private void ProcessDivide(Instruction instruction)
        {
            uint value1;
            if (instruction.OperationSize == 1)
                value1 = registers[AX];
            else value1 = (uint)registers[DX] << 16 | registers[AX];
            var value2 = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);

            if (instruction.OperationSize == 1)
                value2 &= 0xFF;

            var quotient = value1 / value2;
            if ((instruction.OperationSize == 1 && quotient > 0xFF) || (instruction.OperationSize == 2 && quotient > 0xFFFF))
                throw new NotImplementedException();

            var remainder = value1 % value2;

            if (instruction.OperationSize == 1)
                registers[AX] = (ushort)((byte)quotient | ((remainder & 0xFF) << 8));
            else
            {
                registers[AX] = (ushort)quotient;
                registers[DX] = (ushort)remainder;
            }
        }
        private void ProcessMultiply(Instruction instruction)
        {
            var value1 = registers[AX];
            var value2 = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);

            if (instruction.OperationSize == 1)
            {
                value1 &= 0xFF;
                value2 &= 0xFF;
            }
            var result = (uint)value1 * value2;

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.OperationSize == 1)
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
            if (instruction.OperationSize > 1)
                registers[DX] = (ushort)(result >> 16);
        }
        private void ProcessExchange(Instruction instruction)
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

                case ARG_BYTE_REGISTER:
                    SetByteRegister(instruction.Argument1Value, (byte)ProcessExchangeSecond(instruction, GetByteRegister(instruction.Argument1Value)));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private ushort ProcessExchangeSecond(Instruction instruction, ushort value)
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

                case ARG_BYTE_REGISTER:
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
        private void ProcessStringOperation(Instruction instruction)
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
                    if (instruction.Type == InstructionType.Cmps || instruction.Type == InstructionType.Scas)
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
        private void ProcessOneStringOperation(Instruction instruction)
        {
            switch (instruction.Type)
            {
                case InstructionType.Cmps:
                    ProcessCmps(instruction);
                    break;
                case InstructionType.Lods:
                    ProcessLods(instruction);
                    break;
                case InstructionType.Movs:
                    ProcessMovs(instruction);
                    break;
                case InstructionType.Stos:
                    ProcessStos(instruction);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void ProcessCmps(Instruction instruction)
        {
            ushort value1;
            ushort value2;
            if (instruction.OperationSize == 1)
            {
                value1 = ReadU8(SegmentToAddress(registers[DS], registers[SI]));
                value2 = ReadU8(SegmentToAddress(registers[ES], registers[DI]));
            }
            else
            {
                value1 = ReadU16(SegmentToAddress(registers[DS], registers[SI]));
                value2 = ReadU16(SegmentToAddress(registers[ES], registers[DI]));
            }
            var result = value1 - value2;

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.OperationSize == 1)
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
                registers[DI] += instruction.OperationSize;
                registers[SI] += instruction.OperationSize;
            }
            else
            {
                registers[DI] -= instruction.OperationSize;
                registers[SI] -= instruction.OperationSize;
            }
        }
        private void ProcessStos(Instruction instruction)
        {
            if (instruction.OperationSize == 1)
                WriteU8(SegmentToAddress(registers[ES], registers[DI]), (byte)(registers[AX] & 0xFF));
            else WriteU16(SegmentToAddress(registers[ES], registers[DI]), registers[AX]);

            if ((registers[FLAGS] & (1 << DF)) == 0)
                registers[DI] += instruction.OperationSize;
            else registers[DI] -= instruction.OperationSize;
        }
        private void ProcessLods(Instruction instruction)
        {
            int prefix = instruction.Prefix;
            if (prefix == 0) prefix = DS;
            var sourceAddress = SegmentToAddress(registers[prefix], registers[SI]);
            if (instruction.OperationSize == 1)
            {
                var value = ReadU8(sourceAddress);
                SetByteRegister(AL, value);
            }
            else
            {
                var value = ReadU16(sourceAddress);
                registers[AX] = value;
            }

            if ((registers[FLAGS] & (1 << DF)) == 0)
            {
                registers[DI] += instruction.OperationSize;
                registers[SI] += instruction.OperationSize;
            }
            else
            {
                registers[DI] -= instruction.OperationSize;
                registers[SI] -= instruction.OperationSize;
            }
        }
        private void ProcessMovs(Instruction instruction)
        {
            var sourceAddress = SegmentToAddress(registers[DS], registers[SI]);
            var destAddress = SegmentToAddress(registers[ES], registers[DI]);
            if (instruction.OperationSize == 1)
            {
                var value = ReadU8(sourceAddress);
                WriteU8(destAddress, value);
            }
            else
            {
                var value = ReadU16(sourceAddress);
                WriteU16(destAddress, value);
            }

            if ((registers[FLAGS] & (1 << DF)) == 0)
            {
                registers[DI] += instruction.OperationSize;
                registers[SI] += instruction.OperationSize;
            }
            else
            {
                registers[DI] -= instruction.OperationSize;
                registers[SI] -= instruction.OperationSize;
            }
        }
        private void ProcessUnaryArithmetic(Instruction instruction)
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
                    if (instruction.Type == InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);
                    registers[instruction.Argument1] = (ushort)result;
                    break;

                case ARG_BYTE_REGISTER:
                    value = GetByteRegister(instruction.Argument1Value);

                    if (instruction.Type == InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);

                    SetByteRegister(instruction.Argument1Value, (byte)result);
                    break;

                case ARG_MEMORY:
                    var segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    var address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    value = instruction.OperationSize == 1 ? ReadU8(address) : ReadU16(address);
                    if (instruction.Type == InstructionType.Decrement)
                        result = (ushort)(value - 1u);
                    else result = (ushort)(value + 1);
                    if (instruction.OperationSize == 1)
                        WriteU8(address, (byte)result);
                    else WriteU16(address, (ushort)result);
                    break;

                default:
                    throw new NotImplementedException();
            }

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.OperationSize == 1)
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
        private void ProcessArithmetic(Instruction instruction)
        {
            ushort value1;
            var value2 = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

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

                case ARG_BYTE_REGISTER:
                    value1 = GetByteRegister(instruction.Argument1Value);
                    break;

                case ARG_DEREFERENCE:
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
                    value1 = instruction.OperationSize == 1 ? ReadU8(address) : ReadU16(address);
                    break;

                case ARG_MEMORY:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    value1 = instruction.OperationSize == 1 ? ReadU8(address) : ReadU16(address);
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (instruction.OperationSize == 1)
            {
                value1 &= 0xFF;
                value2 &= 0xFF;
            }

            int result;
            if (instruction.Flag.HasFlag(OpcodeFlag.Signed))
            {
                var value3 = (sbyte)(byte)value2;
                switch (instruction.Type)
                {
                    case InstructionType.Adc:
                        result = value1 + value3 + ((registers[FLAGS] >> CF) & 1);
                        break;

                    case InstructionType.Add:
                        result = value1 + value3;
                        break;

                    case InstructionType.Compare:
                    case InstructionType.Subtract:
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
                    case InstructionType.Adc:
                        result = value1 + value2 + ((registers[FLAGS] >> CF) & 1);
                        break;

                    case InstructionType.Add:
                        result = value1 + value2;
                        break;

                    case InstructionType.And:
                    case InstructionType.Test:
                        result = value1 & value2;
                        break;

                    case InstructionType.Compare:
                    case InstructionType.Subtract:
                        result = value1 - value2;
                        break;

                    case InstructionType.Or:
                        result = value1 | value2;
                        break;

                    case InstructionType.Ror:
                        if (instruction.OperationSize == 1)
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

                    case InstructionType.Shl:
                        result = value1 << value2;
                        break;

                    case InstructionType.Shr:
                        result = value1 >> value2;
                        break;

                    case InstructionType.Xor:
                        result = value1 ^ value2;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            ushort truncResult;
            bool carry;
            bool sign;
            if (instruction.OperationSize == 1)
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

            if (instruction.Type != InstructionType.Compare && instruction.Type != InstructionType.Test)
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

                    case ARG_BYTE_REGISTER:
                        SetByteRegister(instruction.Argument1Value, (byte)truncResult);
                        break;

                    case ARG_DEREFERENCE:
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
                        if (instruction.OperationSize == 1)
                            WriteU8(address, (byte)truncResult);
                        else WriteU16(address, truncResult);
                        break;

                    case ARG_MEMORY:
                        segment = instruction.Prefix;
                        if (segment == 0) segment = DS;
                        address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                        if (instruction.OperationSize == 1)
                            WriteU8(address, (byte)truncResult);
                        else WriteU16(address, truncResult);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private void ProcessLea(Instruction instruction)
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
        private void ProcessMov(Instruction instruction)
        {
            var value = GetInstructionValue(instruction.OperationSize, instruction.Prefix, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

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

                case ARG_BYTE_REGISTER:
                    SetByteRegister(instruction.Argument1Value, (byte)value);
                    break;

                case ARG_DEREFERENCE:
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
                    if (instruction.OperationSize == 1)
                        WriteU8(address, (byte)value);
                    else WriteU16(address, value);
                    break;

                case ARG_MEMORY:
                    segment = instruction.Prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instruction.Argument1Value);
                    if (instruction.OperationSize == 1)
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
                case ARG_DEREFERENCE:
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

                case ARG_MEMORY:
                    return (ushort)instructionValue;

                default:
                    throw new NotImplementedException();
            }
        }
        private ushort GetInstructionValue(byte operationSize, byte prefix, int instruction, int instructionValue, int instructionDisplacement)
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

                case ARG_BYTE_REGISTER:
                    return GetByteRegister(instructionValue);

                case ARG_DEREFERENCE:
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
                    return operationSize == 1 ? ReadU8(address) : ReadU16(address);

                case ARG_MEMORY:
                    segment = prefix;
                    if (segment == 0) segment = DS;
                    address = SegmentToAddress(registers[segment], (ushort)instructionValue);
                    return operationSize == 1 ? ReadU8(address) : ReadU16(address);

                case ARG_CONSTANT:
                    return (ushort)instructionValue;

                default:
                    throw new NotImplementedException();
            }
        }

        private static string OutputInstruction(Instruction instruction)
        {
            var output = instruction.Type.ToString();
            var arg1 = OutputArgument(instruction.Prefix, instruction.OperationSize, instruction.Argument1, instruction.Argument1Value, instruction.Argument1Displacement);
            var arg2 = OutputArgument(instruction.Prefix, instruction.OperationSize, instruction.Argument2, instruction.Argument2Value, instruction.Argument2Displacement);

            if (arg1 == null)
                return output;
            return arg2 == null ? $"{output} {arg1}" : $"{output} {arg1}, {arg2}";
        }

        private static string OutputArgument(int prefix, int size, int argument, int argumentValue, int argumentDisplacement)
        {
            if (argument == ARG_NONE)
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

                case ARG_BYTE_REGISTER:
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
                case ARG_DEREFERENCE:
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
                case ARG_MEMORY:
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
                case ARG_FAR_MEMORY:
                    var segment = (uint)argumentValue >> 16;
                    var address = argumentValue & 0xFFFF;
                    return $"[{segment:X4}:{address:X4}]";
                case ARG_CONSTANT:
                    return size == 1 ? $"{argumentValue:X2}" : $"{argumentValue:X4}";
                default:
                    throw new NotImplementedException();
            }
        }

        private Instruction DecodeInstruction()
        {
            Instruction instruction;

            var opcode = GetInstructionByte();
            instruction.Type = typeLookup[opcode];

            if (instruction.Type == InstructionType.Prefix)
            {
                switch (opcode)
                {
                    case 0x26:
                        instruction.Prefix = ES;
                        break;
                    case 0x2E:
                        instruction.Prefix = CS;
                        break;
                    case 0x36:
                        instruction.Prefix = SS;
                        break;
                    case 0x3E:
                        instruction.Prefix = DS;
                        break;
                    case 0xF0:
                    case 0xF2:
                    case 0xF3:
                        instruction.Prefix = opcode;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                opcode = GetInstructionByte();
                instruction.Type = typeLookup[opcode];

                Debug.Assert(instruction.Type != InstructionType.Prefix);
            }
            else instruction.Prefix = 0;
            if (instruction.Type == InstructionType.EmulatorSpecial)
            {
                var opcode2 = GetInstructionByte();
                Debug.Assert(opcode2 == 0x0F);
            }
            Debug.Assert(instruction.Type != InstructionType.Invalid);

            var argument1Type = argument1Map[opcode];
            var argument2Type = argument2Map[opcode];

            instruction.Flag = opcodeFlag[opcode];
            byte rm = 0xFF;
            if (instruction.Flag.HasFlag(OpcodeFlag.HasRM))
                rm = GetInstructionByte();

            if (instruction.Type == InstructionType.Group)
            {
                instruction.Type = ConvertFromGroup(opcode, rm);
                Debug.Assert(instruction.Type != InstructionType.Invalid);
                var reg = (byte)((rm >> 3) & 7);
                if (opcode == 0xF6 && reg == 0)
                    throw new NotImplementedException();
                else if (opcode == 0xF7 && reg == 0)
                    throw new NotImplementedException();
                else if (opcode == 0xFF && (reg == 3 || reg == 5))
                    argument1Type = ARG_M;
            }

            ParseArgument(out instruction.Argument1, out instruction.Argument1Value, out instruction.Argument1Displacement, argument1Type, rm);
            ParseArgument(out instruction.Argument2, out instruction.Argument2Value, out instruction.Argument2Displacement, argument2Type, rm);
            instruction.OperationSize = opcodeSize[opcode];
            if (opcode == 0xA4 || opcode == 0xA6 || opcode == 0xAA || opcode == 0xAC || opcode == 0xAE)
                instruction.OperationSize = 1;

            return instruction;
        }
        private byte GetInstructionByte()
        {
            var value = ReadU8(SegmentToAddress(registers[CS], registers[IP]));
            registers[IP] += 1;
            return value;
        }
        private ushort GetInstructionWord()
        {
            var value = ReadU16(SegmentToAddress(registers[CS], registers[IP]));
            registers[IP] += 2;
            return value;
        }
        private void ParseArgument(out int argument, out int argumentValue, out int argumentDisplacement, int argumentType, byte modrm)
        {
            var mod = (byte)((modrm >> 6) & 7);
            var reg = (byte)((modrm >> 3) & 7);
            var rm = (byte)(modrm & 7);

            switch (argumentType)
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
                case FLAGS:
                    argument = argumentType;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                case AL:
                case CL:
                case DL:
                case BL:
                case AH:
                case CH:
                case DH:
                case BH:
                    argument = ARG_BYTE_REGISTER;
                    argumentValue = argumentType;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_1:
                    argument = ARG_CONSTANT;
                    argumentValue = 1;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_3:
                    argument = ARG_CONSTANT;
                    argumentValue = 3;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_IB:
                    argument = ARG_CONSTANT;
                    argumentValue = GetInstructionByte();
                    argumentDisplacement = ARG_NONE;
                    break;
                case ARG_IW:
                    argument = ARG_CONSTANT;
                    argumentValue = GetInstructionWord();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_JB:
                    argument = ARG_CONSTANT;
                    argumentValue = (sbyte)GetInstructionByte();
                    argumentDisplacement = ARG_NONE;
                    break;
                case ARG_JW:
                    argument = ARG_CONSTANT;
                    argumentValue = GetInstructionWord();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_S:
                    Debug.Assert(reg < 4);
                    argument = reg + ES;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_GB:
                    argument = ARG_BYTE_REGISTER;
                    argumentValue = AL + reg;
                    argumentDisplacement = ARG_NONE;
                    break;
                case ARG_GW:
                    argument = reg;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_OB:
                case ARG_OW:
                    argument = ARG_MEMORY;
                    argumentValue = GetInstructionWord();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_A:
                    argument = ARG_FAR_MEMORY;
                    var address = GetInstructionWord();
                    var segment = GetInstructionWord();
                    argumentValue = (int)(((uint)segment << 16) | address);
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_EB:
                case ARG_EW:
                case ARG_M:
                    switch (mod)
                    {
                        case 0:
                            if (rm == 6)
                            {
                                argument = ARG_MEMORY;
                                argumentValue = GetInstructionWord();
                                argumentDisplacement = ARG_NONE;
                            }
                            else
                            {
                                argument = ARG_DEREFERENCE;
                                argumentValue = rm;
                                argumentDisplacement = 0;
                            }
                            break;
                        case 1:
                            argument = ARG_DEREFERENCE;
                            argumentValue = rm;
                            argumentDisplacement = (sbyte)GetInstructionByte();
                            break;
                        case 2:
                            argument = ARG_DEREFERENCE;
                            argumentValue = rm;
                            argumentDisplacement = GetInstructionWord();
                            break;
                        case 3:
                            Debug.Assert(argumentType != ARG_M);
                            if (argumentType == ARG_EB)
                            {
                                argument = ARG_BYTE_REGISTER;
                                argumentValue = AL + rm;
                            }
                            else
                            {
                                argument = rm;
                                argumentValue = ARG_NONE;
                            }
                            argumentDisplacement = ARG_NONE;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case ARG_NONE:
                    argument = ARG_NONE;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        private static InstructionType ConvertFromGroup(byte opcode, byte modrm)
        {
            var reg = (byte)((modrm >> 3) & 7);
            switch (opcode)
            {
                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                    return opcodeExtension80[reg];
                case 0xC0:
                    return opcodeExtensionC0[reg];
                case 0xC1:
                    return opcodeExtensionC1[reg];
                case 0xD0:
                case 0xD1:
                case 0xD2:
                case 0xD3:
                    return opcodeExtensionD0[reg];
                case 0xF6:
                case 0xF7:
                    return opcodeExtensionF6[reg];
                case 0xFE:
                    return opcodeExtensionFE[reg];
                case 0xFF:
                    return opcodeExtensionFF[reg];
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
    }
}
