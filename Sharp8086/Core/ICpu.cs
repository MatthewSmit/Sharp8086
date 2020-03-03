namespace Sharp8086.Core
{
    /// <summary>
    /// A virtual CPU.
    /// </summary>
    public interface ICpu
    {
        /// <summary>
        /// Executes <paramref name="amount"/> of instructions, or until the virtual CPU halts.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns>Returns false if the CPU halts.</returns>
        bool ProcessInstructions(int amount);

        /// <summary>
        /// Executes one instruction.
        /// </summary>
        /// <returns>Returns false if the CPU halts.</returns>
        bool ProcessSingleInstruction();

        /// <summary>
        /// Attaches a device to the virtual CPU.
        /// </summary>
        /// <param name="device">The device to attach</param>
        void AttachDevice(IDevice device);

        /// <summary>
        /// Reads from the virtual memory and copies it to a new array.
        /// </summary>
        /// <param name="address">The start address to start from.</param>
        /// <param name="size">The amount of memory to read.</param>
        /// <returns>The array of memory.</returns>
        byte[] ReadBytes(uint address, uint size);

        /// <summary>
        /// Copies the memory from <paramref name="value"/> to the CPU's virtual memory.
        /// </summary>
        /// <param name="address">The start address to write to.</param>
        /// <param name="value">The array of memory to copy.</param>
        void WriteBytes(uint address, byte[] value);
    }
}