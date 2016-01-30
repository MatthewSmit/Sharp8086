#region License
// // The MIT License (MIT)
// // 
// // Copyright (c) 2016 Digital Singularity
// // 
// // Permission is hereby granted, free of charge, to any person obtaining a copy
// // of this software and associated documentation files (the "Software"), to deal
// // in the Software without restriction, including without limitation the rights
// // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// // copies of the Software, and to permit persons to whom the Software is
// // furnished to do so, subject to the following conditions:
// // 
// // The above copyright notice and this permission notice shall be included in all
// // copies or substantial portions of the Software.
// // 
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// // SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using SDL2;
using Sharp8086.Core;
using Sharp8086.CPU;

namespace Sharp8086.Peripheral.Graphics
{
    public sealed class GraphicsDevice : IDisposable, IMemoryMappedDevice, IIOMappedDevice
    {
        private const int MEMORY_BASE = 0xB8000;
        private const int MEMORY_SIZE = 0x8000;

        private const ushort MDA_MODE_CONTROL = 0x03B8;
        private const ushort CGA_MODE_CONTROL = 0x03D8;

        private static readonly uint[] colours =
        {
            0x000000FF,
            0x0000AAFF,
            0x00AA00FF,
            0x00AAAAFF,
            0xAA0000FF,
            0xAA00AAFF,
            0xAA5500FF,
            0xAAAAAAFF,
            0x555555FF,
            0x5555FFFF,
            0x55FF55FF,
            0x55FFFFFF,
            0xFF5555FF,
            0xFF55FFFF,
            0xFFFF55FF,
            0xFFFFFFFF
        };

        private readonly IntPtr window;
        private readonly IntPtr renderer;
        private IntPtr texture;

        private readonly byte[] memory = new byte[MEMORY_SIZE];
        private bool dirty;

        private bool enabled = true;
        private bool graphicsMode;
        private int width = 40;
        private int height = 25;
        private int charWidth = 8;
        private int charHeight = 8;

        public GraphicsDevice(IntPtr window, IntPtr renderer)
        {
            this.window = window;
            this.renderer = renderer;

            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    memory[(y * width + x) * 2] = 0x0F;

            ChangeResolution();
        }

        public void Dispose()
        {
            if (texture != IntPtr.Zero)
                SDL.SDL_DestroyTexture(texture);
        }
        public void Draw()
        {
            if (dirty && enabled)
            {
                if (graphicsMode)
                    throw new NotImplementedException();
                else DrawText();
                dirty = false;
            }

            if (SDL.SDL_RenderClear(renderer) != 0)
                throw new InvalidOperationException();
            if (SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero) != 0)
                throw new InvalidOperationException();
        }
        private unsafe void DrawText()
        {
            int pitch;
            IntPtr pixels;
            if (SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixels, out pitch) != 0)
                throw new InvalidOperationException();

            var pixelPtr = (uint*)pixels;
            var stride = pitch / 4;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var code = memory[(y * width + x) * 2];
                    var chr = memory[(y * width + x) * 2 + 1];

                    var foreground = colours[code & 0x0F];
                    var background = colours[(code >> 4) & 0x0F];

                    var ptr = pixelPtr + y * charHeight * stride + x * charWidth;

                    for (var chrY = 0; chrY < charHeight; chrY++)
                    {
                        var yPtr = ptr + chrY * stride;
                        var fontChr = GraphicsFont.Font8X8[chr * 8 + chrY];
                        for (var chrX = 0; chrX < charWidth; chrX++)
                            yPtr[chrX] = ((fontChr >> (8 - chrX)) & 1) == 1 ? foreground : background;
                    }
                }
            }

            SDL.SDL_UnlockTexture(texture);
        }

        byte IIOMappedDevice.ReadU8(ushort port)
        {
            throw new NotImplementedException();
        }
        void IIOMappedDevice.WriteU8(ushort port, byte value)
        {
            switch (port)
            {
                case MDA_MODE_CONTROL:
                    break;
                case CGA_MODE_CONTROL:
                    ChangeCGAMode(value);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        byte IPageController.ReadU8(uint address) => memory[address - MEMORY_BASE];
        void IPageController.WriteU8(uint address, byte value)
        {
            dirty = true;
            memory[address - MEMORY_BASE] = value;
        }

        private void ChangeCGAMode(byte value)
        {
            enabled = ((value >> 3) & 1) == 1;
            if (((value >> 4) & 1) == 1)
            {
                graphicsMode = true;
                width = 640;
                height = 200;
            }
            else if (((value >> 1) & 1) == 1)
            {
                graphicsMode = true;
                width = 320;
                height = 200;
            }
            else
            {
                graphicsMode = false;
                width = ((value >> 0) & 1) == 1 ? 80 : 40;
                height = 25;
                charWidth = 8;
                charHeight = 8;
            }

            ChangeResolution();
        }
        private void ChangeResolution()
        {
            if (texture != IntPtr.Zero)
                SDL.SDL_DestroyTexture(texture);

            int realWidth;
            int realHeight;

            if (graphicsMode)
            {
                realWidth = width;
                realHeight = height;
            }
            else
            {
                realWidth = width * charWidth;
                realHeight = height * charHeight;
            }

            SDL.SDL_SetWindowSize(window, realWidth, realHeight);
            SDL.SDL_SetWindowPosition(window, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);

            texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBX8888, SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, realWidth, realHeight);
            if (texture == IntPtr.Zero)
                throw new InvalidOperationException();

            dirty = true;
        }

        public IEnumerable<Tuple<uint, uint>> MappedMemory => new[]
        {
            new Tuple<uint, uint>(MEMORY_BASE >> Cpu8086.PAGE_SHIFT, MEMORY_SIZE >> Cpu8086.PAGE_SHIFT)
        };
        public IEnumerable<ushort> MappedPorts => new ushort[]
        {
            0x03B0, 0x03B1, 0x03B2, 0x03B3, 0x03B4, 0x03B5, 0x03B6, 0x03B7, MDA_MODE_CONTROL, 0x03B9, 0x03BA, 0x03BB,
            0x03D0, 0x03D1, 0x03D2, 0x03D3, 0x03D4, 0x03D5, 0x03D6, 0x03D7, CGA_MODE_CONTROL, 0x03D9, 0x03DA, 0x03DB, 0x03DC, 0x03DD, 0x03DE, 0x03DF
        };
    }
}