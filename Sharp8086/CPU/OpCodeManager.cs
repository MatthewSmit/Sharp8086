#region License
// // The MIT License (MIT)
// // 
// // Copyright (c) 2016 Digital Singularity
// // 
// // Permission is hereby granted, free of charge, to any person obtaining a copy
// // of this software and associated documentation files (the "Software"), to deal
// // in the Software without restriction, including without limitation the rights
// // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// // copies of the Software, and to permit persons to whom the Software is
// // furnished to do so, subject to the following conditions:
// // 
// // The above copyright notice and this permission notice shall be included in all
// // copies or substantial portions of the Software.
// // 
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// // SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Sharp8086.CPU
{
    internal static class OpCodeManager
    {
        public enum InstructionType
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
            JumpRelative,
            Jump,
            JumpFar,
            Group,
            Test,
            Xchg,
            Move,
            Lea,
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
            SignedMultiply,
            Divide,
            SignedDivide,
            EmulatorSpecial
        }

        [Flags]
        public enum OpCodeFlag : byte
        {
            None = 0,
            Size8 = 1 << 0,
            HasRM = 1 << 1,
            Signed = 1 << 2
        }

        private struct OpCode
        {
            public readonly InstructionType Type;
            public readonly OpCodeFlag Flag;
            public readonly int Argument1Type;
            public readonly int Argument2Type;

            public OpCode(InstructionType type, OpCodeFlag flag, int argument1Type, int argument2Type)
            {
                Type = type;
                Flag = flag;
                Argument1Type = argument1Type;
                Argument2Type = argument2Type;
            }
        }

        private static readonly OpCode[] opCodes =
        {
            /*0x00*/ new OpCode(InstructionType.Add,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_EB,     ARG_GB),
            /*0x01*/ new OpCode(InstructionType.Add,			    OpCodeFlag.HasRM,                       ARG_EW,     ARG_GW),
            /*0x02*/ new OpCode(InstructionType.Add,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_GB,     ARG_EB),
            /*0x03*/ new OpCode(InstructionType.Add,			    OpCodeFlag.HasRM,                       ARG_GW,     ARG_EW),
            /*0x04*/ new OpCode(InstructionType.Add,			    OpCodeFlag.Size8,                       ARG_AL,     ARG_IB),
            /*0x05*/ new OpCode(InstructionType.Add,			    OpCodeFlag.None,                        ARG_AX,     ARG_IW),
            /*0x06*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,                        ARG_ES,     ARG_NONE),
            /*0x07*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,                        ARG_ES,     ARG_NONE),
            /*0x08*/ new OpCode(InstructionType.Or,			        OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_EB,         ARG_GB),
            /*0x09*/ new OpCode(InstructionType.Or,			        OpCodeFlag.HasRM,                       ARG_EW,         ARG_GW),
            /*0x0A*/ new OpCode(InstructionType.Or,			        OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_GB,         ARG_EB),
            /*0x0B*/ new OpCode(InstructionType.Or,			        OpCodeFlag.HasRM,                       ARG_GW,         ARG_EW),
            /*0x0C*/ new OpCode(InstructionType.Or,			        OpCodeFlag.Size8,                       ARG_AL,     ARG_IB),
            /*0x0D*/ new OpCode(InstructionType.Or,			        OpCodeFlag.None,                        ARG_AX,     ARG_IW),
            /*0x0E*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,                        ARG_CS,     ARG_NONE),
            /*0x0F*/ new OpCode(InstructionType.EmulatorSpecial,	OpCodeFlag.None,                        ARG_IB,         ARG_NONE),
            /*0x10*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_EB,         ARG_GB),
            /*0x11*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.HasRM,                       ARG_EW,         ARG_GW),
            /*0x12*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_GB,         ARG_EB),
            /*0x13*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.HasRM,                       ARG_GW,         ARG_EW),
            /*0x14*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.Size8,                       ARG_AL,     ARG_IB),
            /*0x15*/ new OpCode(InstructionType.Adc,			    OpCodeFlag.None,                        ARG_AX,     ARG_IW),
            /*0x16*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,                        ARG_SS,     ARG_NONE),
            /*0x17*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,                        ARG_SS,     ARG_NONE),
            /*0x18*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_EB,         ARG_GB),
            /*0x19*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.HasRM,                       ARG_EW,         ARG_GW),
            /*0x1A*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_GB,         ARG_EB),
            /*0x1B*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.HasRM,                       ARG_GW,         ARG_EW),
            /*0x1C*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.Size8,                       ARG_AL,     ARG_IB),
            /*0x1D*/ new OpCode(InstructionType.Sbb,			    OpCodeFlag.None,                        ARG_AX,     ARG_IW),
            /*0x1E*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,                        ARG_DS,     ARG_NONE),
            /*0x1F*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,                        ARG_DS,     ARG_NONE),
            /*0x20*/ new OpCode(InstructionType.And,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_GB),
            /*0x21*/ new OpCode(InstructionType.And,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_GW),
            /*0x22*/ new OpCode(InstructionType.And,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x23*/ new OpCode(InstructionType.And,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x24*/ new OpCode(InstructionType.And,			    OpCodeFlag.Size8,					    ARG_AL,     ARG_IB),
            /*0x25*/ new OpCode(InstructionType.And,			    OpCodeFlag.None,					    ARG_AX,     ARG_IW),
            /*0x26*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x27*/ new OpCode(InstructionType.Daa,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x28*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_GB),
            /*0x29*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.HasRM,					    ARG_EW,         ARG_GW),
            /*0x2A*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x2B*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x2C*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.Size8,					    ARG_AL,     ARG_IB),
            /*0x2D*/ new OpCode(InstructionType.Subtract,			OpCodeFlag.None,					    ARG_AX,     ARG_IW),
            /*0x2E*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x2F*/ new OpCode(InstructionType.Das,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x30*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_GB),
            /*0x31*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_GW),
            /*0x32*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x33*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x34*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.Size8,					    ARG_AL,     ARG_IB),
            /*0x35*/ new OpCode(InstructionType.Xor,			    OpCodeFlag.None,					    ARG_AX,     ARG_IW),
            /*0x36*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x37*/ new OpCode(InstructionType.Aaa,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x38*/ new OpCode(InstructionType.Compare,			OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_GB),
            /*0x39*/ new OpCode(InstructionType.Compare,			OpCodeFlag.HasRM,					    ARG_EW,         ARG_GW),
            /*0x3A*/ new OpCode(InstructionType.Compare,			OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x3B*/ new OpCode(InstructionType.Compare,			OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x3C*/ new OpCode(InstructionType.Compare,			OpCodeFlag.Size8,					    ARG_AL,     ARG_IB),
            /*0x3D*/ new OpCode(InstructionType.Compare,			OpCodeFlag.None,					    ARG_AX,     ARG_IW),
            /*0x3E*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x3F*/ new OpCode(InstructionType.Aas,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x40*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_AX,     ARG_NONE),
            /*0x41*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_CX,     ARG_NONE),
            /*0x42*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_DX,     ARG_NONE),
            /*0x43*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_BX,     ARG_NONE),
            /*0x44*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_SP,     ARG_NONE),
            /*0x45*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_BP,     ARG_NONE),
            /*0x46*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_SI,     ARG_NONE),
            /*0x47*/ new OpCode(InstructionType.Increment,			OpCodeFlag.None,					    ARG_DI,     ARG_NONE),
            /*0x48*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_AX,     ARG_NONE),
            /*0x49*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_CX,     ARG_NONE),
            /*0x4A*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_DX,     ARG_NONE),
            /*0x4B*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_BX,     ARG_NONE),
            /*0x4C*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_SP,     ARG_NONE),
            /*0x4D*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_BP,     ARG_NONE),
            /*0x4E*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_SI,     ARG_NONE),
            /*0x4F*/ new OpCode(InstructionType.Decrement,			OpCodeFlag.None,					    ARG_DI,     ARG_NONE),
            /*0x50*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_AX,     ARG_NONE),
            /*0x51*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_CX,     ARG_NONE),
            /*0x52*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_DX,     ARG_NONE),
            /*0x53*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_BX,     ARG_NONE),
            /*0x54*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_SP,     ARG_NONE),
            /*0x55*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_BP,     ARG_NONE),
            /*0x56*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_SI,     ARG_NONE),
            /*0x57*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,					    ARG_DI,     ARG_NONE),
            /*0x58*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_AX,     ARG_NONE),
            /*0x59*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_CX,     ARG_NONE),
            /*0x5A*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_DX,     ARG_NONE),
            /*0x5B*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_BX,     ARG_NONE),
            /*0x5C*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_SP,     ARG_NONE),
            /*0x5D*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_BP,     ARG_NONE),
            /*0x5E*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_SI,     ARG_NONE),
            /*0x5F*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_DI,     ARG_NONE),
            /*0x60*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x61*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x62*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x63*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x64*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x65*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x66*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x67*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x68*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x69*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6A*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6B*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6C*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6D*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6E*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x6F*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x70*/ new OpCode(InstructionType.JO,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x71*/ new OpCode(InstructionType.JNO,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x72*/ new OpCode(InstructionType.JB,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x73*/ new OpCode(InstructionType.JNB,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x74*/ new OpCode(InstructionType.JZ,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x75*/ new OpCode(InstructionType.JNZ,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x76*/ new OpCode(InstructionType.JBE,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x77*/ new OpCode(InstructionType.JA,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x78*/ new OpCode(InstructionType.JS,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x79*/ new OpCode(InstructionType.JNS,		        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7A*/ new OpCode(InstructionType.JPE,		        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7B*/ new OpCode(InstructionType.JPO,		        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7C*/ new OpCode(InstructionType.JL,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7D*/ new OpCode(InstructionType.JGE,		        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7E*/ new OpCode(InstructionType.JLE,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x7F*/ new OpCode(InstructionType.JG,			        OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0x80*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_IB),
            /*0x81*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_IW),
            /*0x82*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_IB),
            /*0x83*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM | OpCodeFlag.Signed,	ARG_EW,         ARG_IB),
            /*0x84*/ new OpCode(InstructionType.Test,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x85*/ new OpCode(InstructionType.Test,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x86*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x87*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x88*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_GB),
            /*0x89*/ new OpCode(InstructionType.Move,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_GW),
            /*0x8A*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_GB,         ARG_EB),
            /*0x8B*/ new OpCode(InstructionType.Move,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_EW),
            /*0x8C*/ new OpCode(InstructionType.Move,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_S),
            /*0x8D*/ new OpCode(InstructionType.Lea,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_M),
            /*0x8E*/ new OpCode(InstructionType.Move,			    OpCodeFlag.HasRM,					    ARG_S,          ARG_EW),
            /*0x8F*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_NONE),
            /*0x90*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,                        ARG_AX,         ARG_AX),
            /*0x91*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_CX,         ARG_AX),
            /*0x92*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_DX,         ARG_AX),
            /*0x93*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_BX,         ARG_AX),
            /*0x94*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_SP,         ARG_AX),
            /*0x95*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_BP,         ARG_AX),
            /*0x96*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_SI,         ARG_AX),
            /*0x97*/ new OpCode(InstructionType.Xchg,			    OpCodeFlag.None,					    ARG_DI,         ARG_AX),
            /*0x98*/ new OpCode(InstructionType.Cbw,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x99*/ new OpCode(InstructionType.Cwd,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x9A*/ new OpCode(InstructionType.CallFar,			OpCodeFlag.None,					    ARG_A,          ARG_NONE),
            /*0x9B*/ new OpCode(InstructionType.Wait,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x9C*/ new OpCode(InstructionType.Push,			    OpCodeFlag.None,                        ARG_FLAGS,      ARG_NONE),
            /*0x9D*/ new OpCode(InstructionType.Pop,			    OpCodeFlag.None,					    ARG_FLAGS,      ARG_NONE),
            /*0x9E*/ new OpCode(InstructionType.Sahf,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0x9F*/ new OpCode(InstructionType.Lahf,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xA0*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_AL,         ARG_OB),
            /*0xA1*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_AX,         ARG_OW),
            /*0xA2*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_OB,         ARG_AL),
            /*0xA3*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_OW,         ARG_AX),
            /*0xA4*/ new OpCode(InstructionType.Movs,			    OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xA5*/ new OpCode(InstructionType.Movs,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xA6*/ new OpCode(InstructionType.Cmps,			    OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xA7*/ new OpCode(InstructionType.Cmps,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xA8*/ new OpCode(InstructionType.Test,			    OpCodeFlag.Size8,					    ARG_AL,         ARG_IB),
            /*0xA9*/ new OpCode(InstructionType.Test,			    OpCodeFlag.None,					    ARG_AX,         ARG_IW),
            /*0xAA*/ new OpCode(InstructionType.Stos,			    OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xAB*/ new OpCode(InstructionType.Stos,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xAC*/ new OpCode(InstructionType.Lods,			    OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xAD*/ new OpCode(InstructionType.Lods,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xAE*/ new OpCode(InstructionType.Scas,			    OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xAF*/ new OpCode(InstructionType.Scas,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xB0*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_AL,     ARG_IB),
            /*0xB1*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_CL,     ARG_IB),
            /*0xB2*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_DL,     ARG_IB),
            /*0xB3*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_BL,     ARG_IB),
            /*0xB4*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_AH,     ARG_IB),
            /*0xB5*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_CH,     ARG_IB),
            /*0xB6*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_DH,     ARG_IB),
            /*0xB7*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8,					    ARG_BH,     ARG_IB),
            /*0xB8*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_AX,     ARG_IW),
            /*0xB9*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_CX,     ARG_IW),
            /*0xBA*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_DX,     ARG_IW),
            /*0xBB*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_BX,     ARG_IW),
            /*0xBC*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_SP,     ARG_IW),
            /*0xBD*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_BP,     ARG_IW),
            /*0xBE*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_SI,     ARG_IW),
            /*0xBF*/ new OpCode(InstructionType.Move,			    OpCodeFlag.None,					    ARG_DI,     ARG_IW),
            /*0xC0*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_IB),
            /*0xC1*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_IB),
            /*0xC2*/ new OpCode(InstructionType.ReturnNear,			OpCodeFlag.None,					    ARG_IW,         ARG_NONE),
            /*0xC3*/ new OpCode(InstructionType.ReturnNear,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xC4*/ new OpCode(InstructionType.Les,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_M),
            /*0xC5*/ new OpCode(InstructionType.Lds,			    OpCodeFlag.HasRM,					    ARG_GW,         ARG_M),
            /*0xC6*/ new OpCode(InstructionType.Move,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_IB),
            /*0xC7*/ new OpCode(InstructionType.Move,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_IW),
            /*0xC8*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xC9*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xCA*/ new OpCode(InstructionType.ReturnFar,			OpCodeFlag.None,					    ARG_IW,         ARG_NONE),
            /*0xCB*/ new OpCode(InstructionType.ReturnFar,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xCC*/ new OpCode(InstructionType.Int,			    OpCodeFlag.None,					    ARG_3,          ARG_NONE),
            /*0xCD*/ new OpCode(InstructionType.Int,			    OpCodeFlag.Size8,					    ARG_IB,         ARG_NONE),
            /*0xCE*/ new OpCode(InstructionType.Into,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xCF*/ new OpCode(InstructionType.ReturnInterrupt,	OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xD0*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_1),
            /*0xD1*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_1),
            /*0xD2*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,	ARG_EB,         ARG_CL),
            /*0xD3*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_CL),
            /*0xD4*/ new OpCode(InstructionType.Aam,			    OpCodeFlag.Size8,					    ARG_IB,         ARG_NONE),
            /*0xD5*/ new OpCode(InstructionType.Aad,			    OpCodeFlag.Size8,					    ARG_IB,         ARG_NONE),
            /*0xD6*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xD7*/ new OpCode(InstructionType.Xlat,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xD8*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xD9*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xDA*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xDB*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xDC*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xDD*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.Size8,					    ARG_NONE,       ARG_NONE),
            /*0xDE*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xDF*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xE0*/ new OpCode(InstructionType.Loopnz,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0xE1*/ new OpCode(InstructionType.Loopz,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0xE2*/ new OpCode(InstructionType.Loop,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0xE3*/ new OpCode(InstructionType.Jcxz,			    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0xE4*/ new OpCode(InstructionType.In,			        OpCodeFlag.Size8,					    ARG_AL,         ARG_IB),
            /*0xE5*/ new OpCode(InstructionType.In,			        OpCodeFlag.None,					    ARG_AX,         ARG_IB),
            /*0xE6*/ new OpCode(InstructionType.Out,			    OpCodeFlag.Size8,					    ARG_IB,         ARG_AL),
            /*0xE7*/ new OpCode(InstructionType.Out,			    OpCodeFlag.None,					    ARG_IB,         ARG_AX),
            /*0xE8*/ new OpCode(InstructionType.CallNearRelative,	OpCodeFlag.None,					    ARG_JW,         ARG_NONE),
            /*0xE9*/ new OpCode(InstructionType.JumpRelative,	    OpCodeFlag.None,					    ARG_JW,         ARG_NONE),
            /*0xEA*/ new OpCode(InstructionType.JumpFar,			OpCodeFlag.None,					    ARG_A,          ARG_NONE),
            /*0xEB*/ new OpCode(InstructionType.JumpRelative,	    OpCodeFlag.Size8,					    ARG_JB,         ARG_NONE),
            /*0xEC*/ new OpCode(InstructionType.In,			        OpCodeFlag.Size8,					    ARG_AL,         ARG_DX),
            /*0xED*/ new OpCode(InstructionType.In,			        OpCodeFlag.None,					    ARG_AX,         ARG_DX),
            /*0xEE*/ new OpCode(InstructionType.Out,			    OpCodeFlag.Size8,					    ARG_DX,         ARG_AL),
            /*0xEF*/ new OpCode(InstructionType.Out,			    OpCodeFlag.None,					    ARG_DX,         ARG_AX),
            /*0xF0*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF1*/ new OpCode(InstructionType.Invalid,			OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF2*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF3*/ new OpCode(InstructionType.Prefix,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF4*/ new OpCode(InstructionType.Hlt,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF5*/ new OpCode(InstructionType.Cmc,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF6*/ new OpCode(InstructionType.Group,			    OpCodeFlag.Size8 | OpCodeFlag.HasRM,    ARG_EB,         ARG_NONE),
            /*0xF7*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_NONE),
            /*0xF8*/ new OpCode(InstructionType.Clc,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xF9*/ new OpCode(InstructionType.Stc,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xFA*/ new OpCode(InstructionType.Cli,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xFB*/ new OpCode(InstructionType.Sti,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xFC*/ new OpCode(InstructionType.Cld,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xFD*/ new OpCode(InstructionType.Std,			    OpCodeFlag.None,					    ARG_NONE,       ARG_NONE),
            /*0xFE*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EB,         ARG_NONE),
            /*0xFF*/ new OpCode(InstructionType.Group,			    OpCodeFlag.HasRM,					    ARG_EW,         ARG_NONE)
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
            InstructionType.Test, InstructionType.Invalid, InstructionType.Not, InstructionType.Negate, InstructionType.Multiply, InstructionType.SignedMultiply, InstructionType.Divide, InstructionType.SignedDivide
        };

        private static readonly InstructionType[] opcodeExtensionFe =
        {
            InstructionType.Increment, InstructionType.Decrement, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid, InstructionType.Invalid
        };

        private static readonly InstructionType[] opcodeExtensionFf =
        {
            InstructionType.Increment, InstructionType.Decrement, InstructionType.CallNear, InstructionType.CallFar, InstructionType.Jump, InstructionType.JumpFar, InstructionType.Push, InstructionType.Invalid
        };

        private const int ARG_AX = (int)Cpu8086.Register.AX;
        private const int ARG_CX = (int)Cpu8086.Register.CX;
        private const int ARG_DX = (int)Cpu8086.Register.DX;
        private const int ARG_BX = (int)Cpu8086.Register.BX;

        private const int ARG_SP = (int)Cpu8086.Register.SP;
        private const int ARG_BP = (int)Cpu8086.Register.BP;
        private const int ARG_SI = (int)Cpu8086.Register.SI;
        private const int ARG_DI = (int)Cpu8086.Register.DI;

        private const int ARG_ES = (int)Cpu8086.Register.ES;
        private const int ARG_CS = (int)Cpu8086.Register.CS;
        private const int ARG_SS = (int)Cpu8086.Register.SS;
        private const int ARG_DS = (int)Cpu8086.Register.DS;

        private const int ARG_IP = (int)Cpu8086.Register.IP;
        private const int ARG_FLAGS = unchecked((int)Cpu8086.Register.FLAGS);

        private const int ARG_AL = unchecked((int)Cpu8086.Register.AL);
        private const int ARG_CL = unchecked((int)Cpu8086.Register.CL);
        private const int ARG_DL = unchecked((int)Cpu8086.Register.DL);
        private const int ARG_BL = unchecked((int)Cpu8086.Register.BL);
        private const int ARG_AH = unchecked((int)Cpu8086.Register.AH);
        private const int ARG_CH = unchecked((int)Cpu8086.Register.CH);
        private const int ARG_DH = unchecked((int)Cpu8086.Register.DH);
        private const int ARG_BH = unchecked((int)Cpu8086.Register.BH);

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
        public const int ARG_BYTE_REGISTER = -6;
        public const int ARG_DEREFERENCE = -5;
        public const int ARG_FAR_MEMORY = -4;
        public const int ARG_MEMORY = -3;
        public const int ARG_CONSTANT = -2;
        public const int ARG_NONE = -1;

        public struct Instruction
        {
            public Cpu8086.Register SegmentPrefix;
            public byte OpcodePrefix;
            public InstructionType Type;
            public OpCodeFlag Flag;

            public int Argument1;
            public int Argument1Value;
            public int Argument1Displacement;
            public int Argument2;
            public int Argument2Value;
            public int Argument2Displacement;
        }

        public static Instruction Decode([NotNull] IInstructionFetcher fetcher)
        {
            Instruction instruction;

            var opcode = fetcher.FetchU8();
            instruction.Type = opCodes[opcode].Type;
            instruction.SegmentPrefix = Cpu8086.Register.Invalid;
            instruction.OpcodePrefix = 0;

            while (instruction.Type == InstructionType.Prefix)
            {
                switch (opcode)
                {
                    case 0x26:
                        instruction.SegmentPrefix = Cpu8086.Register.ES;
                        break;
                    case 0x2E:
                        instruction.SegmentPrefix = Cpu8086.Register.CS;
                        break;
                    case 0x36:
                        instruction.SegmentPrefix = Cpu8086.Register.SS;
                        break;
                    case 0x3E:
                        instruction.SegmentPrefix = Cpu8086.Register.DS;
                        break;
                    case 0xF0:
                    case 0xF2:
                    case 0xF3:
                        instruction.OpcodePrefix = opcode;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                opcode = fetcher.FetchU8();
                instruction.Type = opCodes[opcode].Type;
            }
            if (instruction.Type == InstructionType.EmulatorSpecial)
            {
                var opcode2 = fetcher.FetchU8();
                Debug.Assert(opcode2 == 0x0F);
            }
            Debug.Assert(instruction.Type != InstructionType.Invalid);

            var argument1Type = opCodes[opcode].Argument1Type;
            var argument2Type = opCodes[opcode].Argument2Type;

            instruction.Flag = opCodes[opcode].Flag;
            byte rm = 0xFF;
            if (instruction.Flag.HasFlag(OpCodeFlag.HasRM))
                rm = fetcher.FetchU8();

            if (instruction.Type == InstructionType.Group)
            {
                instruction.Type = ConvertFromGroup(opcode, rm);
                Debug.Assert(instruction.Type != InstructionType.Invalid);
                var reg = (byte)((rm >> 3) & 7);
                if (opcode == 0xF6 && reg == 0)
                    argument2Type = ARG_IB;
                else if (opcode == 0xF7 && reg == 0)
                    argument2Type = ARG_IW;
                else if (opcode == 0xFF && (reg == 3 || reg == 5))
                    argument1Type = ARG_M;
            }

            ParseArgument(fetcher, instruction.Flag, out instruction.Argument1, out instruction.Argument1Value, out instruction.Argument1Displacement, argument1Type, rm);
            ParseArgument(fetcher, instruction.Flag, out instruction.Argument2, out instruction.Argument2Value, out instruction.Argument2Displacement, argument2Type, rm);

            return instruction;
        }

        private static void ParseArgument([NotNull] IInstructionFetcher fetcher, OpCodeFlag flag, out int argument, out int argumentValue, out int argumentDisplacement, int argumentType, byte modrm)
        {
            var mod = (byte)((modrm >> 6) & 7);
            var reg = (byte)((modrm >> 3) & 7);
            var rm = (byte)(modrm & 7);

            switch (argumentType)
            {
                case ARG_AX:
                case ARG_CX:
                case ARG_DX:
                case ARG_BX:
                case ARG_SP:
                case ARG_BP:
                case ARG_SI:
                case ARG_DI:
                case ARG_IP:
                case ARG_CS:
                case ARG_DS:
                case ARG_ES:
                case ARG_SS:
                case ARG_FLAGS:
                    argument = argumentType;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_AL:
                case ARG_CL:
                case ARG_DL:
                case ARG_BL:
                case ARG_AH:
                case ARG_CH:
                case ARG_DH:
                case ARG_BH:
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
                    argumentValue = fetcher.FetchU8();
                    argumentDisplacement = ARG_NONE;
                    if (flag.HasFlag(OpCodeFlag.Signed))
                        argumentValue = (sbyte)(byte)argumentValue;
                    break;
                case ARG_IW:
                    argument = ARG_CONSTANT;
                    argumentValue = fetcher.FetchU16();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_JB:
                    argument = ARG_CONSTANT;
                    argumentValue = (sbyte)fetcher.FetchU8();
                    argumentDisplacement = ARG_NONE;
                    break;
                case ARG_JW:
                    argument = ARG_CONSTANT;
                    argumentValue = fetcher.FetchU16();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_S:
                    Debug.Assert(reg < 4);
                    argument = reg + ARG_ES;
                    argumentValue = ARG_NONE;
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_GB:
                    argument = ARG_BYTE_REGISTER;
                    argumentValue = ARG_AL + reg;
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
                    argumentValue = fetcher.FetchU16();
                    argumentDisplacement = ARG_NONE;
                    break;

                case ARG_A:
                    argument = ARG_FAR_MEMORY;
                    var address = fetcher.FetchU16();
                    var segment = fetcher.FetchU16();
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
                                argumentValue = fetcher.FetchU16();
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
                            argumentDisplacement = (sbyte)fetcher.FetchU8();
                            break;
                        case 2:
                            argument = ARG_DEREFERENCE;
                            argumentValue = rm;
                            argumentDisplacement = fetcher.FetchU16();
                            break;
                        case 3:
                            Debug.Assert(argumentType != ARG_M);
                            if (argumentType == ARG_EB)
                            {
                                argument = ARG_BYTE_REGISTER;
                                argumentValue = ARG_AL + rm;
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
                    return opcodeExtensionFe[reg];
                case 0xFF:
                    return opcodeExtensionFf[reg];
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
