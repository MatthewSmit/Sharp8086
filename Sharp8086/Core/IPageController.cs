namespace Sharp8086.Core
{
    public interface IPageController
    {
        byte ReadU8(uint address);

        void WriteU8(uint address, byte value);
    }
}