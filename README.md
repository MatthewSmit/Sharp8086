# Sharp8086

A cpu emulator written in C#. Support for 8086 and 80186 is currently a work in progress.

Idealy written to allow for easy extension for new or custom peripherals.

Due to a lack of protected mode support (80386), currently the only supported bios is a hand written one with special opcodes to talk to the emulator.

## Roadmap
##### Near future
- Get emulator up to a stage where it can boot various OS's, such as MSDos and FreeDOS.
- Add configuration settings to change amount of debug output.
- More peripheral support, such as VGA, SoundBlaster, Mouse, and Serial
##### Far Future
- Get test case for instructions to assert validity of instruction emulation.
- Inbuilt debugger, possibly gdb and/or custom debugger.
- Code optimizations, such as caching the decoded opcode

## Rational
I've always liked C#, programming in it usually feels cleaner then in other languages. Figuring out how the 8086 works is interesting, and I would like to show that C# is just as capable at emulating old systems. It would also be nice to figure out certain emulated functions that get called regularly and compile them to cil at runtime, similar to the java hotspot runtime.