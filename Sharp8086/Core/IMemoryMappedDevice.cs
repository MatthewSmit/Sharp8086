using System;
using System.Collections.Generic;

namespace Sharp8086.Core
{
    /// <summary>
    /// A device mapped to memory address space
    /// </summary>
    public interface IMemoryMappedDevice : IDevice, IPageController
    {
        /// <summary>
        /// Returns an IEnumerable of all the address pages that this device owns. Each tuple contains the start page, and the amount of pages after it.
        /// </summary>
        IEnumerable<Tuple<uint, uint>> MappedMemory { get; }
    }
}