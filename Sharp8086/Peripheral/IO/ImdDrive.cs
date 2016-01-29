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
using JetBrains.Annotations;

namespace Sharp8086.Peripheral.IO
{
    /// <summary>
    /// Reads disks in the ImageDisk format, does not support writing.
    /// </summary>
    [PublicAPI]
    public sealed class ImdDrive : IDrive
    {
        [NotNull] private readonly byte[] data;

        [PublicAPI]
        public ImdDrive([NotNull] Stream backing)
        {
            if (backing == null)
                throw new ArgumentNullException(nameof(backing));
            if (!backing.CanRead || !backing.CanSeek)
                throw new ArgumentException();

            var br = new BinaryReader(backing);

            var imdHeader = br.ReadBytes(31);

            // Verify header and skip comment
            if (imdHeader[0] != 'I' ||
                imdHeader[1] != 'M' ||
                imdHeader[2] != 'D' ||
                imdHeader[29] != '\r' ||
                imdHeader[30] != '\n')
                throw new InvalidDataException();
            while (br.ReadByte() != 0x1A)
            {
            }

            // Backup position and scan imd format for sizes
            var position = backing.Position;

            ushort numberCylinders;
            byte numberSectors;
            GetImdSizes(br, out numberCylinders, out numberSectors);

            NumberCylinders = numberCylinders;
            NumberSectors = numberSectors;

            backing.Position = position;
            data = new byte[512 * NumberSectors * 2 * NumberCylinders];

            // Read data from imd format
            while (backing.Position < backing.Length)
            {
                var mode = br.ReadBytes(5);

                var offset = mode[1] * 2 * NumberSectors * 512;
                offset += mode[2] * NumberSectors * 512;

                var numberingMap = br.ReadBytes(NumberSectors);

                for (var i = 0; i < NumberSectors; i++)
                {
                    var sector = numberingMap[i] - 1;
                    var type = br.ReadByte();
                    switch (type)
                    {
                        case 0x01:
                            br.Read(data, offset + sector * 512, 512);
                            break;
                        case 0x02:
                            var value = br.ReadByte();
                            for (var j = 0; j < 512; j++)
                                data[offset + sector * 512 + j] = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public byte[] Read(uint offset, uint size)
        {
            var buffer = new byte[size];
            Array.Copy(data, offset, buffer, 0, size);
            return buffer;
        }

        private static void GetImdSizes([NotNull] BinaryReader br, out ushort numberCylinders, out byte numberSectors)
        {
            numberCylinders = 0;
            numberSectors = 0xFF;

            var backing = br.BaseStream;
            while (backing.Position < backing.Length)
            {
                var mode = br.ReadBytes(5);

                Debug.Assert(mode[0] <= 5);
                Debug.Assert(mode[2] == 0 || mode[2] == 1);
                Debug.Assert(mode[4] <= 6);

                if (mode[2] == 0)
                {
                    Debug.Assert(mode[1] == numberCylinders);
                    numberCylinders++;
                }
                else Debug.Assert(mode[1] == numberCylinders - 1);

                if (numberSectors == 0xFF) numberSectors = mode[3];
                else Debug.Assert(numberSectors == mode[3]);
                Debug.Assert(mode[4] == 2);

                br.ReadBytes(numberSectors);

                for (var i = 0; i < numberSectors; i++)
                {
                    var type = br.ReadByte();
                    switch (type)
                    {
                        case 0x01:
                            backing.Position += 512;
                            break;
                        case 0x02:
                            backing.Position++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public byte FloppyType
        {
            get
            {
                switch (data.Length)
                {
                    case 360 * 1024:
                        return 1;
                    case 1200 * 1024:
                        return 2;
                    case 720 * 1024:
                        return 3;
                    case 1440 * 1024:
                        return 4;
                    default:
                        return 1;
                }
            }
        }
        public byte NumberSectors { get; }
        public byte NumberHeads => 2;
        public ushort NumberCylinders { get; }
    }
}