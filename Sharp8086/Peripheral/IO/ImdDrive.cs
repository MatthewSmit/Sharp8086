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
using System.IO;

namespace Sharp8086.Peripheral.IO
{
    public sealed class ImdDrive : IDrive
    {
        private static readonly int[] sectorSizeMap =
        {
            0x80,
            0x100,
            0x200,
            0x400,
            0x800,
            0x1000,
            0x2000
        };

        private readonly byte[] data;

        public ImdDrive(Stream backing)
        {
            var br = new BinaryReader(backing);

            var imdHeader = br.ReadBytes(31);

            Debug.Assert(imdHeader[0] == 'I' &&
                         imdHeader[1] == 'M' &&
                         imdHeader[2] == 'D' &&
                         imdHeader[29] == '\r' &&
                         imdHeader[30] == '\n');

            while (br.ReadByte() != 0x1A)
            {
            }

            var position = backing.Position;
            while (backing.Position < backing.Length)
            {
                var mode = br.ReadBytes(5);

                Debug.Assert(mode[0] <= 5);
                Debug.Assert(mode[2] == 0 || mode[2] == 1);
                Debug.Assert(mode[4] <= 6);

                if (mode[2] == 0)
                {
                    Debug.Assert(mode[1] == NumberCylinders);
                    NumberCylinders++;
                }
                else Debug.Assert(mode[1] == NumberCylinders - 1);

                if (NumberSectors == -1) NumberSectors = mode[3];
                else Debug.Assert(NumberSectors == mode[3]);
                if (SectorSize == -1) SectorSize = sectorSizeMap[mode[4]];
                else Debug.Assert(SectorSize == sectorSizeMap[mode[4]]);

                br.ReadBytes(NumberSectors);

                for (var i = 0; i < NumberSectors; i++)
                {
                    var type = br.ReadByte();
                    switch (type)
                    {
                        case 0x01:
                            backing.Position += SectorSize;
                            break;
                        case 0x02:
                            backing.Position++;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            backing.Position = position;
            data = new byte[SectorSize * NumberSectors * 2 * NumberCylinders];

            while (backing.Position < backing.Length)
            {
                var mode = br.ReadBytes(5);

                var offset = mode[1] * 2 * NumberSectors * SectorSize;
                offset += mode[2] * NumberSectors * SectorSize;

                var numberingMap = br.ReadBytes(NumberSectors);

                for (var i = 0; i < NumberSectors; i++)
                {
                    var sector = numberingMap[i] - 1;
                    var type = br.ReadByte();
                    switch (type)
                    {
                        case 0x01:
                            br.Read(data, offset + sector * SectorSize, SectorSize);
                            break;
                        case 0x02:
                            var value = br.ReadByte();
                            for (var j = 0; j < SectorSize; j++)
                                data[offset + sector * SectorSize + j] = value;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public byte[] Read(int offset, int size)
        {
            var buffer = new byte[size];
            Array.Copy(data, offset, buffer, 0, size);
            return buffer;
        }

        public bool IsHardDrive => false;

        public int SectorSize { get; } = -1;
        public int NumberSectors { get; } = -1;
        public int NumberHeads => 2;
        public int NumberCylinders { get; }
    }
}