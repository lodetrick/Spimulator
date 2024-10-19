using Godot;
using System;
using System.Text;

public partial class Main : Node2D //this is the spimulator
{
	private GUI gui = null;
	private uint ProgramCounter = 0u;
	private bool ProgramActive = false;
	private bool AskingForInput = false;
	private uint High, Low;
	private byte[] InstructionSet;
	private uint[] Registers;
	public static byte[] Memory; //use bitconverter when reading to and writing to memory
	public override void _Ready()
	{
		gui = (GUI)GetNode("CanvasLayer/GUI");

		Memory    = new byte[1024]; // 1 KB of Memory
		Registers = new uint[32];
		Array.Fill<uint>(Registers,0);
		gui.DisplayRegisters(Registers);
	}

	private void ResetSPIM() {
		ProgramCounter = 0u;
		ProgramActive = true;
		AskingForInput = false;
		Array.Fill<uint>(Registers,0);
		gui.DisplayRegisters(Registers);
		gui.ClearOutput();
	}

	public ValueTuple<uint,string,string> Step()
	{
		//Find register pouinted to by program counter
		if ((int)ProgramCounter >= InstructionSet.Length) {
			throw new CompileException($"Program Counter ({(int)ProgramCounter}) Outside of Range: (0 - {InstructionSet.Length})");
		}
		uint instr = BitConverter.ToUInt32(InstructionSet,(int)ProgramCounter);
		//Identify the Type (R, I, J) by the OpCode
		uint Opcode = GetOpcode(instr);
		uint RS = 0,RT = 0,RD = 0,Immediate = 0,Address = 0;
		int Shamt = 0;
		Command function;

		if (Opcode == 0) {
			// R-Type
			RS    = GetRS(instr);
			RT    = GetRT(instr);
			RD    = GetRD(instr);
			Shamt = GetShamt(instr);
			// Get Funct and pass in RS, RT, RD
			function = Compiler.CommandFromBinary(GetOpcodeAndFunct(instr));
		}
		else if (Opcode > 3){
			// I Type
			RS = GetRS(instr);
			RT = GetRT(instr);
			Immediate = GetImmediate(instr);

			function = Compiler.CommandFromBinary(instr & 0xFC000000);
		}
		else {
			// J Type
			Address = GetAddress(instr);
			
			function = Compiler.CommandFromBinary(instr & 0xFC000000);
		}
		
		ProgramCounter += 4;

		switch (function.binary) {
			case 0x20000000:  // addi
				return Addi(instr,RT,RS,Immediate);
			case 0x00000020:  // add
				return Add(instr,RD,RS,RT);
			case 0x00000022:  // sub
				return Sub(instr,RD,RS,RT);
			case 0x00000024:  // and
				return And(instr,RD,RS,RT);
			case 0x3C000000:  // lui
				return Lui(instr,RT,Immediate);
			case 0x00000000:  // sll
				return SLL(instr,RD,RT,Shamt);
			case 0x00000002:  // srl
				return SRL(instr,RD,RT,Shamt);
			case 0x00000003:  // sra
				return SRA(instr,RD,RT,Shamt);
			case 0x0000000C:  // syscall
				Syscall(instr);
				break;
			case 0x0000002A:  // slt
				return SLT(instr,RD,RS,RT);
			case 0x10000000: // beq
				return Beq(instr,RT,RS,Immediate);
			case 0x14000000: // bne
				return Bne(instr,RT,RS,Immediate);
			case 0x08000000: // j
				return J(instr, Address);
			case 0x0C000000: // jal
				return Jal(instr, Address);
			case 0x00000008: // jr
				return Jr(instr, RS);
			case 0x00000018: // mult
				return Mult(instr, RS, RT);
			case 0x00000012: // mflo
				return Mflo(instr, RD);
			case 0x00000010: // mfhi
				return Mfhi(instr, RD);
			case 0x00000021: // addu
				return Addu(instr,RD,RS,RT);
			case 0x00000025: // or
				return Or(instr,RD,RS,RT);
			case 0x00000027: // nor
				return Nor(instr,RD,RS,RT);
			case 0x24000000: // addiu
				return Addiu(instr,RT,RS,Immediate);
			case 0x8C000000: // lw
				return Lw(instr,RT,RS,Immediate);
			case 0xAC000000: // sw
				return Sw(instr,RT,RS,Immediate);
			case 0x30000000: // andi
				return Andi(instr,RT,RS,Immediate);
			case 0x34000000: // ori
				return Ori(instr,RT,RS,Immediate);
			default: // error
				throw new CompileException($"Unknown Binary Instruction: {instr}");
		}

		return (instr,"syscall","syscall"); //error
	}

	public ValueTuple<uint,string,string> Addi(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] + immediate;
		return (instr,$"addi {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"{(int)Registers[rs]} + {(int)immediate} = {(int)Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Addiu(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] + immediate;
		return (instr,$"addiu {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"{Registers[rs]} + {immediate} = {Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Lw(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = BitConverter.ToUInt32(Memory,(int)(Registers[rs] + (int)immediate));
		return (instr,$"lw {Compiler.register_names[rt]}, {(int)immediate}({Compiler.register_names[rs]})",$"{Registers[rt]} = M[{Registers[rs]} + {(int)immediate}]");
	}

	public ValueTuple<uint,string,string> Sw(uint instr, uint rt, uint rs, uint immediate) {
		byte[] bytes = BitConverter.GetBytes(Registers[rt]); //4 bytes
		for (int i = 0; i < 4; i++) {
			Memory[Registers[rs] + immediate + i] = bytes[i];
		}
		return (instr,$"sw {Compiler.register_names[rt]}, {(int)immediate}({Compiler.register_names[rs]})",$"M[{Registers[rs]} + {(int)immediate}] = {Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Beq(uint instr, uint rt, uint rs, uint immediate) {
		if (Registers[rt] == Registers[rs]) {
			ProgramCounter += immediate << 2;
		}
		return (instr,$"beq {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"if ({Registers[rt]} == {Registers[rs]}) PC += 4 + {(int)immediate}");
	}

	public ValueTuple<uint,string,string> Bne(uint instr, uint rt, uint rs, uint immediate) {
		if (Registers[rt] != Registers[rs]) {
			ProgramCounter += immediate << 2;
		}
		return (instr,$"bne {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"if ({Registers[rt]} != {Registers[rs]}) PC += 4 + {(int)immediate}");
	}

	public ValueTuple<uint,string,string> Jr(uint instr, uint rs) {
		ProgramCounter = Registers[rs];
		return (instr,$"jr {Compiler.register_names[rs]}",$"PC = {Registers[rs]}");
	}

	public ValueTuple<uint,string,string> J(uint instr, uint address) {
		ProgramCounter = address;
		return (instr,$"j {address}",$"PC = {address}");
	}

	public ValueTuple<uint,string,string> Jal(uint instr, uint address) {
		Registers[31] = ProgramCounter;
		ProgramCounter = address;
		return (instr,$"jal {address}",$"$ra = PC, PC = {address}");
	}

	public ValueTuple<uint,string,string> Mult(uint instr, uint rt, uint rs) {
		Int64 total = ((Int64)Registers[rt]) * ((Int64)Registers[rs]);
		High = (UInt32)(total >> 32);
		Low  = (UInt32)(total & 0x00000000_FFFFFFFF);
		return (instr,$"mult {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{{High, Low}} = {Registers[rt]} * {Registers[rs]}");
	}

	public ValueTuple<uint,string,string> Mflo(uint instr, uint rd) {
		Registers[rd] = Low;
		return (instr,$"mflo {Compiler.register_names[rd]}",$"{Registers[rd]} = Low");
	}

	public ValueTuple<uint,string,string> Mfhi(uint instr, uint rd) {
		Registers[rd] = High;
		return (instr,$"mfhi {Compiler.register_names[rd]}",$"{Registers[rd]} = High");
	}

	public ValueTuple<uint,string,string> Add(uint instr, uint rd, uint rs, uint rt) {
		// returns the operation in string form to display on the GUI
		// ( normally, it would just return void, but this is good information to have )
		// also, have the first part be assembly, separated with \n
		// EX: Add(_,12,6) -> 12 + 6 = 18 , maybe with some bbcode formatting
		Registers[rd] = Registers[rs] + Registers[rt];
		if (Registers[rs] >> 31 == Registers[rt] >> 31 && Registers[rd] >> 31 != Registers[rt] >> 31) {
			throw new CompileException($"Signed Integer Addition for command \"add {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}\" is out of bounds");
		}
		return (instr,$"add {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} + {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Addu(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] + Registers[rt];
		return (instr,$"addu {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} + {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Sub(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] - Registers[rt];
		return (instr,$"sub {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} - {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> SLT(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = (int)Registers[rs] < (int)Registers[rt] ? 1u : 0u;
		return (instr,$"slt {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rd]} = {(int)Registers[rs]} < {(int)Registers[rt]} ? 1 : 0");
	}

	public ValueTuple<uint,string,string> And(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] & Registers[rt];
		return (instr,$"and {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} & {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Or(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] | Registers[rt];
		return (instr,$"or {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} | {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Nor(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = ~(Registers[rs] | Registers[rt]);
		return (instr,$"nor {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"~({Registers[rs]} | {Registers[rt]}) = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Lui(uint instr, uint rt, uint immediate) {
		Registers[rt] = immediate << 16;
		return (instr,$"lui {Compiler.register_names[rt]}, {immediate}",$"{Compiler.register_names[rt]} = {immediate} << 16");
	}

	public ValueTuple<uint,string,string> SLL(uint instr, uint rd, uint rt, int shamt) {
		Registers[rd] = Registers[rt] << shamt;
		return (instr,$"sll {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} << {shamt}");
	}

	public ValueTuple<uint,string,string> SRL(uint instr, uint rd, uint rt, int shamt) {
		Registers[rd] = Registers[rt] >>> shamt;
		return (instr,$"srl {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} >> {shamt}");
	}

	public ValueTuple<uint,string,string> SRA(uint instr, uint rd, uint rt, int shamt) {
		Registers[rd] = Registers[rt] >> shamt;
		return (instr,$"sra {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} >> {shamt}");
	}

	public ValueTuple<uint,string,string> Ori(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] | (immediate & 0x0000_FFFF);
		return (instr,$"ori {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {immediate}",$"{Registers[rt]} | {immediate} = {Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Andi(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] & (immediate & 0x0000_FFFF);
		return (instr,$"andi {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {immediate}",$"{Registers[rt]} & {immediate} = {Registers[rt]}");
	}

	public void Syscall(uint instr) {
		
		switch (Registers[2]) { // $v0
			case 1: // print int
				gui.AddToOutput(((int)(Registers[4])).ToString());
				break;
			case 4: // print null-terminated string
				//go to memory pointed to by $a0, add that char
				// while the char != 0, add the char
				// finally, print the string to output
				uint mem = Registers[4];
				while (Memory[mem] != 0) {
					gui.AddToOutput(((char)Memory[mem]).ToString());
					mem += 1;
				}
				break;
			case 5: // user int input
				AskingForInput = true;
				break;
			case 10: // quit
				ProgramActive = false;
				break;
			case 11: // print char
				gui.AddToOutput(((char)Memory[Registers[4]]).ToString());
				break;
		}
	}

	public async void OnStepButtonPressed() {
		if (AskingForInput) return;

		if (ProgramActive) {
			gui.UpdateStepDisplay(Step());

			if (AskingForInput) {
				GD.Print("Waiting");
				await ToSignal(gui.TextInput, LineEdit.SignalName.TextSubmitted);
				AskingForInput = false;

				switch (Registers[2]) {
					case 5:
						Registers[2] = (uint)Compiler.NumFromText(gui.TextInput.Text);
						break;
				}

				gui.AddToOutput(gui.TextInput.Text + "\n");
				gui.TextInput.Clear();
			}
			gui.DisplayRegisters(Registers);
		}	
	}

	public async void OnRunButtonPressed() {
		ResetSPIM();
		// issue: it doesn't reset the memory, so it doesn't work when running multiple times without recompiling

		//while program not exited
		while (ProgramActive) {
			//step through the program
			try {
				Step();
			}
			catch (CompileException e) {
				gui.AddToOutput($"Error: {e.Message}\n");
				break;
			}
			
			if (AskingForInput) {
				GD.Print("Waiting");
				await ToSignal(gui.TextInput, LineEdit.SignalName.TextSubmitted);
				AskingForInput = false;

				switch (Registers[2]) {
					case 5:
						Registers[2] = (uint)Compiler.NumFromText(gui.TextInput.Text);
						break;
				}
				
				gui.AddToOutput(gui.TextInput.Text + "\n");
				gui.TextInput.Clear();
			}
		}
		//update registers
		gui.DisplayRegisters(Registers);
	}

	public void OnCompileButtonPressed() {
		string assembly = gui.CompileInput.Text;
		ResetSPIM();
		
		try {
			InstructionSet = Compiler.Compile(assembly);
		}
		catch (CompileException e) {
			//highlight line i, print error code e to output
			gui.HighlightLine(e.Line());
			gui.AddToOutput($"Error: {e.InnerException.Message}\n");
			return;
		}
		
		//swap view to instruction set
		gui.SwapToInstructionSet(InstructionSet);

		for (int i = 0; i < InstructionSet.Length; i+=4) {
			GD.Print($"{BitConverter.ToUInt32(InstructionSet,i).ToString("X8")} - {BitConverter.ToUInt32(InstructionSet,i).ToString("B32")}");
		}
		GD.Print("------INSTRUCTION-OVER------\n");

		gui.OnCompileButtonPressed();
	}

	public static uint GetOpcode(uint instr) {
		return (instr >> 26) & 0b0011_1111;
	}

	public static uint GetRS(uint instr) {
		return (instr >> 21) & 0b0001_1111;
	}
	
	public static uint GetRT(uint instr) {
		return (instr >> 16) & 0b0001_1111;
	}

	public static uint GetRD(uint instr) {
		return (instr >> 11) & 0b0001_1111;
	}

	public static int GetShamt(uint instr) {
		return ((int)instr >> 6) & 0b0001_1111;
	}
	
	public static uint GetFunct(uint instr) {
		return instr & 0b0011_1111;
	}

	public static uint GetOpcodeAndFunct(uint instr) {
		return instr & 0xFC00003F;
	}

	public static uint GetImmediate(uint instr) {
		return (uint)(((int)instr << 16) >> 16);
	}

	public static uint GetAddress(uint instr) {
		return instr & 0x03FF_FFFF;
	}
}
