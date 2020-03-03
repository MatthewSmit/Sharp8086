using System;
using Sharp8086.Core;

namespace Sharp8086.CPU
{
    internal sealed class IOPageController : IPageController
    {
        private readonly IIOMappedDevice[] devices = new IIOMappedDevice[0x10000];
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
            get => devices[port];
            set => devices[port] = value;
        }
    }
}