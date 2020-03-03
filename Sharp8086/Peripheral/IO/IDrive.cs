using Sharp8086.Core;

namespace Sharp8086.Peripheral.IO
{
    public interface IDrive : IDevice
    {
        byte[] Read(uint offset, uint size);

        byte FloppyType { get; }

        byte NumberSectors { get; }

        byte NumberHeads { get; }

        ushort NumberCylinders { get; }
    }
}