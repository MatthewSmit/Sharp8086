using System;
using System.IO;

namespace Sharp8086.Peripheral.IO
{
    public sealed class RawDrive : IDrive
    {
        private readonly byte[] data;
        private bool readWrite;
        private bool isFloppy;

        public RawDrive(Stream backing, bool readWrite, bool isFloppyDrive, byte sectors, ushort cylinders, byte heads)
        {
            if (backing == null)
                throw new ArgumentNullException(nameof(backing));
            if (!backing.CanRead)
                throw new ArgumentException();
            if (isFloppyDrive && heads != 1 && heads != 2)
                throw new ArgumentOutOfRangeException(nameof(heads));

            this.readWrite = readWrite;
            isFloppy = isFloppyDrive;
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

        public byte FloppyType
        {
            get
            {
                if (!isFloppy)
                    return 0;
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
        public byte NumberHeads { get; }
        public ushort NumberCylinders { get; }
    }
}