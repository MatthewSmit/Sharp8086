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
using JetBrains.Annotations;
using Sharp8086.Core;

namespace Sharp8086.CPU
{
    internal sealed class IOPageController : IPageController
    {
        [NotNull] private readonly IIOMappedDevice[] devices = new IIOMappedDevice[0x10000];
        private readonly uint portOffset;
        private readonly uint portSize;

        public IOPageController(uint portOffset, uint portSize)
        {
            this.portOffset = portOffset;
            this.portSize = portSize;
        }

        public byte ReadU8(uint address)
        {
            var port = address - portOffset;
            if (devices[port] != null)
                return devices[port].ReadU8((ushort)address);
            throw new NotImplementedException();
        }
        public void WriteU8(uint address, byte value)
        {
            var port = address - portOffset;
            if (devices[port] != null)
                devices[port].WriteU8((ushort)address, value);
            else throw new NotImplementedException();
        }

        public IIOMappedDevice this[ushort port]
        {
            [Pure] get { return devices[port]; }
            set { devices[port] = value; }
        }
    }
}