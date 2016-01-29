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

using System.Collections.Generic;
using Sharp8086.Core;

namespace Sharp8086.CPU
{
    public sealed class PIC : IIOMappedDevice
    {
        private const ushort PIC1_COMMAND = 0x0020;
        private const ushort PIC1_DATA = 0x0021;
        private const ushort PIC2_COMMAND = 0x00A0;
        private const ushort PIC2_DATA = 0x00A1;

        public byte ReadU8(ushort port)
        {
            throw new System.NotImplementedException();
        }
        public void WriteU8(ushort port, byte value)
        {
            if (port == PIC1_COMMAND && value == 0x20)
                return;
            throw new System.NotImplementedException();
        }

        public IEnumerable<ushort> MappedPorts => new ushort[]
        {
            PIC1_COMMAND,
            PIC1_DATA,
            PIC2_COMMAND,
            PIC2_DATA
        };
    }
}