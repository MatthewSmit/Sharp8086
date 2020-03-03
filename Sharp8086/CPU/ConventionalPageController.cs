using Sharp8086.Core;

namespace Sharp8086.CPU
{
    internal sealed class ConventionPageController : IPageController
    {
        private readonly byte[] memory;

        public ConventionPageController(byte[] memory)
        {
            this.memory = memory;
        }

        public byte ReadU8(uint address) => memory[address];
        public void WriteU8(uint address, byte value) => memory[address] = value;
    }
}