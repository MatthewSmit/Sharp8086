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

namespace Sharp8086.Peripheral.IO
{
    public sealed class RawDrive : IDrive
    {
        private bool readWrite;
        private readonly byte[] data;

        public RawDrive(Stream backing, bool readWrite, bool isHardDrive, int sectorSize, int sectors, int cylinders, int heads)
        {
            this.readWrite = readWrite;
            IsHardDrive = isHardDrive;
            SectorSize = sectorSize;
            NumberSectors = sectors;
            NumberCylinders = cylinders;
            NumberHeads = heads;

            data = new byte[512 * sectors * cylinders * 2];
            if (backing.Length != data.Length)
                throw new InvalidOperationException();
            backing.Read(data, 0, data.Length);
        }

        public byte[] Read(int offset, int size)
        {
            var buffer = new byte[size];
            Array.Copy(data, offset, buffer, 0, size);
            return buffer;
        }

        public bool IsHardDrive { get; }

        public int SectorSize { get; }
        public int NumberSectors { get; }
        public int NumberHeads { get; }
        public int NumberCylinders { get; }
    }
}