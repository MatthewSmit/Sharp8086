; The MIT License (MIT)
; 
; Copyright (c) 2016 Digital Singularity
; 
; Permission is hereby granted, free of charge, to any person obtaining a copy
; of this software and associated documentation files (the "Software"), to deal
; in the Software without restriction, including without limitation the rights
; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
; copies of the Software, and to permit persons to whom the Software is
; furnished to do so, subject to the following conditions:
; 
; The above copyright notice and this permission notice shall be included in all
; copies or substantial portions of the Software.
; 
; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
; SOFTWARE.

cpu	8086

%macro emulatorCall 1
	db 0x0F
	db 0x0F
	db %1
%endmacro

%macro functionEnter 1
	push bp
	mov bp, sp
	sub sp, %1 ; Allocate temporary storage
%endmacro

%macro functionExit 1
	add sp, %1 ; Free temporary storage
	pop bp
%endmacro

%define emulatorSetup emulatorCall 0x01
%define emulatorDisk emulatorCall 0x02
%define emulatorClock emulatorCall 0x03

data:
	cursorX db 0
	cursorY db 0

BiosEntry:
	; Set up stack to F000:F000

	mov	sp, 0xF000
	mov	ss, sp

	cld

; Fill the bios data area
	mov ax, biosData
	emulatorSetup
	mov ax, 0xF000
	mov ds, ax
	mov	ax, 0x40
	mov	es, ax
	mov	di, 0
	mov	si, biosData
	mov	cx, 256
	rep movsw
	
; Set up the IVT.
; Zero IVT table

	mov	ax, 0
	mov	es, ax
	mov	di, 0
	mov	cx, 512
	rep	stosw

; Load the pointers to our interrupt handlers

	mov	di, 0
	mov ax, 0xF000
	mov ds, ax
	mov	si, interruptTable
	mov	cx, [interruptSize]
	rep	movsb

; Read boot sector from FDD, and load it into 0:7C00

	mov	ax, 0
	mov	es, ax
	mov	bx, 0x7c00

	mov	ax, 0x0201
	mov	cx, 0x0001
	mov dx, 0x0000
	
	int	0x13

; Jump to boot sector

	jmp	0:0x7c00

; Interrupt handlers

; int 0x00, Divide by Zero
int0:
	iret

int1:
int2:
int3:
int4:
int5:
int6:
int7:
int8:
int9:
inta:
intb:
intc:
intd:
inte:
intf:
int 3
iret

; int 0x10, Video
int10:
	cmp ah, 0x0E
	je TeletypeOutput
	int 3
	iret

TeletypeOutput:
	; AL = character to write
	; BH = page number
	; BL = foreground color (graphics modes only)
	
	functionEnter 4
	push ds
	
	cmp al, 0x0A
	je TeletypeOutputNewLine
	
	cmp al, 0x0D
	je TeletypeOutputCarriageReturn
	
	mov [ss:bp - 1], al
	mov [ss:bp - 4], bx
	
	mov ax, 0xB800
	mov ds, ax
	
	; bx = (cursorY * 40 + cursorX) * 2
	xor ax, ax
	mov al, [cs:cursorY]
	mov bl, 40
	mul bl
	add al, [cs:cursorX]
	mov bl, 2
	mul bl
	mov bx, ax
	
	mov al, [ss:bp - 1]
	mov [bx + 1], al
	
	inc byte [cs:cursorX]
	cmp byte [cs:cursorX], 40
	
	jne TeletypeOutputEnd
	mov byte [cs:cursorX], 0
	inc byte [cs:cursorY]
	
	cmp byte [cs:cursorY], 25
	jne TeletypeOutputEnd
	
	int 3
	
TeletypeOutputCarriageReturn:
	mov byte [cs:cursorX], 0
	jmp TeletypeOutputEnd
	
TeletypeOutputNewLine:
	inc byte [cs:cursorY]
	
	cmp byte [cs:cursorY], 25
	jne TeletypeOutputEnd
	
	int 3

TeletypeOutputEnd:
	
	pop ds
	functionExit 4
	
	iret

; int 0x11, Equipement list
int11:
	mov ax, [cs:equipment]
	iret

; int 0x12, Get Memory
int12:
	mov ax, [cs:memorySize]
	iret

; int 0x13, Disk IO
int13:
	cmp ah, 0x00
	je SectorDriveReset
	cmp ah, 0x02
	je SectorRead
	cmp ah, 0x08
	je SectorDriveInformation
	cmp ah, 0x15
	je SectorDiskInformation
	int 3
	iret
	
SectorDriveReset:
	jmp ReachStackClearCarry
	
SectorRead:
	; AL = number of sectors to read (must be nonzero)
	; CH = low eight bits of cylinder number
	; CL = sector number 1-63 (bits 0-5)
	;      high two bits of cylinder (bits 6-7, hard disk only)
	; DH = head number
	; DL = drive number (bit 7 set for hard disk)
	; ES:BX -> data buffer
	
	functionEnter 10
	
	mov [ss:bp - 01], dl	; Drive number
	mov [ss:bp - 02], dh	; Head Number
	mov [ss:bp - 04], ch	; Cylinder number (lower 8 bytes)
	mov [ss:bp - 06], al	; Sectors to read
	mov [ss:bp - 08], es	; Destination Sector
	mov [ss:bp - 10], bx	; Destination Offset
	
	xor ax, ax
	mov al, cl
	and al, 0x3F	; Lower 6 bits only
	mov [ss:bp - 05], al
	
	xor ax, ax
	mov al, cl
	and al, 0xC0	; Upper 2 bits
cpu	186
	shr al, 6
cpu	8086
	mov [ss:bp - 03], al
	
	mov ah, 0x02
	emulatorDisk
	
	cmp ax, 0
	je SectorReadSuccess
	
	; Failure reading
	mov ah, al
	mov al, 0
	
	functionExit 10
	jmp ReachStackSetCarry
	
SectorReadSuccess:
	mov al, [ss:bp - 06]
	functionExit 10
	jmp ReachStackClearCarry
	
SectorDriveInformation:
	emulatorDisk
	cmp ah, 0x00
	je ReachStackClearCarry
	jmp ReachStackSetCarry
	
SectorDiskInformation:
	emulatorDisk
	jmp ReachStackClearCarry

; int 0x14, Serial Communication
int14:
	mov	ax, 0
	iret

; int 0x15, Configuration
int15:
	cmp ah, 0xC0
	je GetConfiguration
	
	int 3
	iret

GetConfiguration:
	jmp ReachStackSetCarry

; int 0x16, Keyboard
int16:
	cmp ah, 0x00
	je KeyboardGetKeystroke
	cmp ah, 0x01
	je KeyboardCheckPress
	
	int 3
	iret

KeyboardGetKeystroke:
	mov ah, 0
	mov al, 0
	iret

KeyboardCheckPress:
	jmp ReachStackSetZero

; int 0x17, Printer
int17:
	cmp ah, 0x01
	je PrinterInitialize
	int 3
	iret

PrinterInitialize:
	mov ah, 0x38 ; Printer not attached
	iret

int18:
int19:
int 3
iret

; int 0x1A, Clock
int1a:
	cmp ah, 0x00
	je ClockGetTime
	cmp ah, 0x02
	je ClockGetRTC
	cmp ah, 0x04
	je ClockGetDate
	int 3
	iret

ClockGetTime:
	emulatorClock
	iret

ClockGetRTC:
	emulatorClock
	jmp ReachStackClearCarry

ClockGetDate:
	emulatorClock
	jmp ReachStackClearCarry

int1b:
int1c:
int1d:
int1e:
int1f:
int 3
iret

; Commands to set/clear stack carry flag
ReachStackSetCarry:
	xchg bp, sp
	or word[bp + 4], 1
	xchg bp, sp
	iret

ReachStackClearCarry:
	xchg bp, sp
	and word[bp + 4], 0xFFFE
	xchg bp, sp
	iret

; Commands to set/clear stack zero flag
ReachStackSetZero:
	xchg bp, sp
	or word[bp + 4], 0x40
	xchg bp, sp
	iret

ReachStackClearZero:
	xchg bp, sp
	and word[bp + 4], 0xFFBF
	xchg bp, sp
	iret

; Interrupt vector table - to copy to 0:0

interruptTable	dw int0
				dw 0xf000
				dw int1
				dw 0xf000
				dw int2
				dw 0xf000
				dw int3
				dw 0xf000
				dw int4
				dw 0xf000
				dw int5
				dw 0xf000
				dw int6
				dw 0xf000
				dw int7
				dw 0xf000
				dw int8
				dw 0xf000
				dw int9
				dw 0xf000
				dw inta
				dw 0xf000
				dw intb
				dw 0xf000
				dw intc
				dw 0xf000
				dw intd
				dw 0xf000
				dw inte
				dw 0xf000
				dw intf
				dw 0xf000
				dw int10
				dw 0xf000
				dw int11
				dw 0xf000
				dw int12
				dw 0xf000
				dw int13
				dw 0xf000
				dw int14
				dw 0xf000
				dw int15
				dw 0xf000
				dw int16
				dw 0xf000
				dw int17
				dw 0xf000
				dw int18
				dw 0xf000
				dw int19
				dw 0xf000
				dw int1a
				dw 0xf000
				dw int1b
				dw 0xf000
				dw int1c
				dw 0xf000
				dw int1d
				dw 0xf000
				dw int1e
				dw 0xf000
				dw int1f

interruptSize dw $-interruptTable

biosData:
	times 8 dw 0
	equipment: dw 0
	db 0
	memorySize: dw 0
	times 0x100-$+biosData db 0x00

; Fills bios with 0xCC except reset vector
times 0xFFF0-$+data db 0xCC
jmp BiosEntry
times 0xFFFF-$+data db 0xCC
db 0xCC