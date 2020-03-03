namespace Sharp8086.CPU
{
    internal static class EnumExtensions
    {
        public static bool Has(this Cpu8086.FlagsRegister flag, Cpu8086.FlagsRegister testFlag) =>
            (flag & testFlag) != 0;

        public static bool Has(this OpCodeManager.OpCodeFlag flag, OpCodeManager.OpCodeFlag testFlag) =>
            (flag & testFlag) != 0;
    }
}