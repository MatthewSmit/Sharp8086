using System.Runtime.InteropServices;

namespace Sharp8086.CPU
{
    [StructLayout(LayoutKind.Explicit, Size = 0x100)]
    internal struct BiosData
    {
        [FieldOffset(0x00)] public ushort SerialPort1;
        [FieldOffset(0x02)] public ushort SerialPort2;
        [FieldOffset(0x04)] public ushort SerialPort3;
        [FieldOffset(0x06)] public ushort SerialPort4;
        [FieldOffset(0x08)] public ushort ParallelPort1;
        [FieldOffset(0x0A)] public ushort ParallelPort2;
        [FieldOffset(0x0C)] public ushort ParallelPort3;
        [FieldOffset(0x10)] public ushort Equipment;
        [FieldOffset(0x13)] public ushort MemorySize;
    }
}