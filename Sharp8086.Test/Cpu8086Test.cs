using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharp8086.Core;
using Sharp8086.CPU;

namespace Sharp8086.Test
{
    public sealed class Cpu8086Test
    {
        [Test]
        public void TestAdd() => RunTest("add");
        [Test]
        public void TestBcdcnv() => RunTest("bcdcnv");
        [Test]
        public void TestBitwise() => RunTest("bitwise");
        [Test]
        public void TestCmpneg() => RunTest("cmpneg");
        [Test]
        public void TestControl() => RunTest("control");
        [Test]
        public void TestDatatrnf() => RunTest("datatrnf");
        [Test]
        public void TestDiv() => RunTest("div");
        [Test]
        public void TestInterrupt() => RunTest("interrupt");
        [Test]
        public void TestJumpMove() => RunTest("jmpmov");
        [Test]
        public void TestJump1() => RunTest("jump1");
        [Test]
        public void TestJump2() => RunTest("jump2");
        [Test]
        public void TestMultiply() => RunTest("mul");
        [Test]
        public void TestRep() => RunTest("rep");
        [Test]
        public void TestRotate() => RunTest("rotate");
        [Test]
        public void TestSegpr() => RunTest("segpr");
        [Test]
        public void TestShifts() => RunTest("shifts");
        [Test]
        public void TestStrings() => RunTest("strings");
        [Test]
        public void TestSub() => RunTest("sub");

        private static void RunTest(string testFile)
        {
            ICpu cpu;
            using (var file = File.OpenRead("CpuBinaries/" + testFile))
                cpu = new Cpu8086(file, 1024 * 1024);
            cpu.WriteBytes(0, new byte[0x100]);

            if (cpu.ProcessInstructions(1000))
                throw new InvalidOperationException("Test case did not complete in the required amount of time.");

            using (var file = File.OpenRead($"CpuBinaries/{testFile}Result"))
            {
                var goodData = new byte[file.Length];
                if (file.Read(goodData, 0, goodData.Length) != goodData.Length)
                    throw new InvalidDataException();

                var testData = cpu.ReadBytes(0, (uint)goodData.Length);

                var result = CompareArrays(goodData, testData);
                if (result.Count > 0)
                {
                    var comparison = result.Select(i => new Tuple<int, byte, byte>(i, goodData[i], testData[i])).ToList();
                    throw new InvalidOperationException("Test case did not produce valid output.");
                }
            }
        }

        private static List<int> CompareArrays<T>(IReadOnlyList<T> data1, IReadOnlyList<T> data2)
        {
            Assert.AreEqual(data1.Count, data2.Count);

            var list = new List<int>();
            for (var i = 0; i < data1.Count; i++)
                if (!Equals(data1[i], data2[i]))
                    list.Add(i);
            return list;
        }
    }
}
