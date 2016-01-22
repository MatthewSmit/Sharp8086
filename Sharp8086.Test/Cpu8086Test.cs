using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp8086.Core;
using Sharp8086.CPU;

namespace Sharp8086.Test
{
    [TestClass]
    public sealed class Cpu8086Test
    {
        [TestMethod]
        public void TestAdd()
        {
            ICpu cpu;
            using (var file = File.OpenRead("add"))
                cpu = new Cpu8086(file, 1024 * 1024);
            cpu.WriteBytes(0, new byte[0x100]);

            while (cpu.ProcessInstruction())
            {
            }

            using (var file = File.OpenRead("addResult"))
            {
                var goodData = new byte[file.Length];
                if (file.Read(goodData, 0, goodData.Length) != goodData.Length)
                    throw new InvalidDataException();

                var testData = cpu.ReadBytes(0, (uint)goodData.Length);

                var result = CompareArrays(goodData, testData);
                if (result != -1)
                    throw new InvalidOperationException();
            }
        }

        private static int CompareArrays<T>(T[] data1, T[] data2)
        {
            if (data1.Length != data2.Length)
                return 0;
            for (var i = 0; i < data1.Length; i++)
                if (!Equals(data1[i], data2[i]))
                    return i;
            return -1;
        }
    }
}
