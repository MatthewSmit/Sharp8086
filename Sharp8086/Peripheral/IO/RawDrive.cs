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
using System.IO;
using JetBrains.Annotations;

namespace Sharp8086.Peripheral.IO
{
    [PublicAPI]
    public sealed class RawDrive : IDrive
    {
        private bool readWrite;
        [NotNull] private readonly byte[] data;

        [PublicAPI]
        public RawDrive([NotNull] Stream backing, bool readWrite, bool isFloppyDrive, byte sectors, ushort cylinders, byte heads)
        {
            if (backing == null)
                throw new ArgumentNullException(nameof(backing));
            if (!backing.CanRead)
                throw new ArgumentException();
            if (isFloppyDrive && heads != 1 && heads != 2)
                throw new ArgumentOutOfRangeException(nameof(heads));

            this.readWrite = readWrite;
            IsFloppyDrive = isFloppyDrive;
            NumberSectors = sectors;
            NumberCylinders = cylinders;
            NumberHeads = heads;

            data = new byte[512 * sectors * cylinders * 2];
            if (backing.Length != data.Length)
                throw new InvalidDataException();
            backing.Read(data, 0, data.Length);
        }

        public byte[] Read(uint offset, uint size)
        {
            var buffer = new byte[size];
            Array.Copy(data, offset, buffer, 0, size);
            return buffer;
        }

        public bool IsFloppyDrive { get; }

        public byte NumberSectors { get; }
        public byte NumberHeads { get; }
        public ushort NumberCylinders { get; }
    }
}