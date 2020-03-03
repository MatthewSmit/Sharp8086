using System;
using System.Diagnostics;
using System.IO;

namespace Sharp8086.Peripheral.IO
{
    /// <summary>
    /// Reads disks in the ImageDisk format, does not support writing.
    /// </summary>
    public sealed class ImdDrive : IDrive
    {
        private readonly byte[] data;

        public ImdDrive(Stream backing)
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

        private static void GetImdSizes(BinaryReader br, out ushort numberCylinders, out byte numberSectors)
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