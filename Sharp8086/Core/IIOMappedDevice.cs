using System.Collections.Generic;

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
        IEnumerable<ushort> MappedPorts { get; }
    }
}