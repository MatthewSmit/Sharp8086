#region License
// The MIT License (MIT)
// 
// Copyright (c) 2016 Digital Singularity
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.IO;
using SDL2;
using Sharp8086.Core;
using Sharp8086.CPU;
using Sharp8086.Peripheral.Graphics;
using Sharp8086.Peripheral.IO;
using Sharp8086.Test;

namespace Sharp8086.Emulator
{
    internal static class Program
    {
        private static ICpu cpu;

        private static void Main()
        {
            new Cpu8086Test().TestJump2();
            return;

            InitCpu();

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
                    SDL.SDL_Event evt;
                    while (SDL.SDL_PollEvent(out evt) != 0)
                    {
                        if (evt.type == SDL.SDL_EventType.SDL_QUIT)
                            quit = true;
                    }

                    const int instructionsToProcess = 100;
                    for (var i = instructionsToProcess; i >= 0; i--)
                        if (!cpu.ProcessInstruction())
                            break;

                    graphics.Draw();
                    SDL.SDL_RenderPresent(renderer);
                }
            }

            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
        private static void InitCpu()
        {
            using (var file = File.OpenRead("bios"))
                cpu = new Cpu8086(file, 1024 * 1024);

            using (var disk = File.OpenRead("TestDisks/Dos6.22.img"))
                cpu.AttachDevice(new RawDrive(disk, false, true, 512, 18, 80, 2));
        }
    }
}
