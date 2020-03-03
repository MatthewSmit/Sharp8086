using System;
using System.IO;
using SDL2;
using Sharp8086.Core;
using Sharp8086.CPU;
using Sharp8086.Peripheral.Graphics;
using Sharp8086.Peripheral.IO;

namespace Sharp8086.Emulator
{
    internal static class Program
    {
        private static ICpu cpu;

        private static void Main(string[] args)
        {
            // TODO: Better argument parsing

            InitCpu(args[0]);

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
                throw new InvalidOperationException();

            var window = SDL.SDL_CreateWindow("CpuEmu", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 64, 64, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (window == IntPtr.Zero)
                throw new InvalidOperationException();

            var renderer = SDL.SDL_CreateRenderer(window, -1, 0);
            if (renderer == IntPtr.Zero)
                throw new InvalidOperationException();

            using (var graphics = new GraphicsDevice(window, renderer))
            {
                cpu.AttachDevice(graphics);

                var quit = false;
                while (!quit)
                {
                    while (SDL.SDL_PollEvent(out var evt) != 0)
                    {
                        if (evt.type == SDL.SDL_EventType.SDL_QUIT)
                            quit = true;
                    }

                    cpu.ProcessInstructions(100);

                    graphics.Draw();
                    SDL.SDL_RenderPresent(renderer);
                }
            }

            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
        private static void InitCpu(string diskFile)
        {
            using var file = File.OpenRead("bios");
            using var disk = File.OpenRead(diskFile);

            cpu = new Cpu8086(file, 1024 * 1024);
            cpu.AttachDevice(new RawDrive(disk, false, true, 18, 80, 2));

            // TODO
            // using (var file = File.OpenRead(@"C:\Work\FakeSharp\FakeSharp\pcxtbios.bin"))
            //     //using (var file = File.OpenRead("bios"))
            //     cpu = new Cpu8086(file, 1024 * 1024);
            //
            // cpu.AttachDevice(new IntervalTimer());
            // cpu.AttachDevice(new InterruptController());
            //
            // //using (var disk = File.OpenRead("TestDisks/Dos1.25.imd"))
            // //    cpu.AttachDevice(new ImdDrive(disk));
            // //using (var disk = File.OpenRead("TestDisks/PC-DOS 1.10.imd"))
            // //    cpu.AttachDevice(new ImdDrive(disk));
            // //using (var disk = File.OpenRead("TestDisks/MS-DOS 6.22 Boot Disk.img"))
            // //    cpu.AttachDevice(new RawDrive(disk, false, true, 18, 80, 2));
            // //using (var disk = File.OpenRead("TestDisks/MS-DOS 5.0 Disk 1.img"))
            // //    cpu.AttachDevice(new RawDrive(disk, false, true, 9, 80, 2));
            // //using (var disk = File.OpenRead("TestDisks/MS-DOS 6.22 Disk 1.img"))
            // //    cpu.AttachDevice(new RawDrive(disk, false, true, 18, 80, 2));
            // //using (var disk = File.OpenRead("TestDisks/FreeDOS 1.0.img"))
            // using (var disk = File.OpenRead(@"D:\Work\nasm-2.11.08\8086tiny-master\fd.img"))
            //     cpu.AttachDevice(new RawDrive(disk, false, true, 18, 80, 2));
        }
    }
}
