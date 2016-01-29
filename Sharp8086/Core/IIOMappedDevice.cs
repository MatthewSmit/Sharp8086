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
using JetBrains.Annotations;

namespace Sharp8086.Core
{
    /// <summary>
    /// A device mapped to the IO space
    /// </summary>
    public interface IIOMappedDevice : IDevice
    {
        /// <summary>
        /// Reads a byte from the IO address space that this device owns.
        /// </summary>
        /// <param name="port">Port of the IO address space</param>
        /// <returns>Value read from the device</returns>
        byte ReadU8(ushort port);
        /// <summary>
        /// Writes a byte to the IO address space that this device owns.
        /// </summary>
        /// <param name="port">Port of the IO address space</param>
        /// <param name="value">Value to write to the device</param>
        void WriteU8(ushort port, byte value);

        /// <summary>
        /// Returns an IEnumerable of all the IO address ports that this device owns.
        /// </summary>
        [NotNull]
        IEnumerable<ushort> MappedPorts { [Pure] get; }
    }
}