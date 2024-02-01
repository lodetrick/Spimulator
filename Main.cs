using Godot;
using System;

public partial class Main : Node2D //this is the spimulator
{
	private GUI gui = null;
	private int ProgramCounter = -4;
	private bool ProgramActive = false;
	private bool AskingForInput = false;
	private byte[] InstructionSet;
	private uint[] Registers;
	private byte[] Memory;
	public override void _Ready()
	{
		gui = (GUI)GetNode("CanvasLayer/GUI");

		Registers = new uint[32];
		Array.Fill<uint>(Registers,0);
		gui.DisplayRegisters(Registers);
	}

	private void ResetSPIM() {
		ProgramCounter = -4;
		ProgramActive = true;
		AskingForInput = false;
		Array.Fill<uint>(Registers,0);
		gui.DisplayRegisters(Registers);
		gui.ClearOutput();
	}

	public ValueTuple<uint,string,string> Step()
	{
		//Increment Program Counter
		ProgramCounter += 4;
		//Find register pouinted to by program counter
		uint instr = BitConverter.ToUInt32(InstructionSet,ProgramCounter);
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
				break;
			case 0x00000021: // addu
				break;
			case 0x00000025: // or
				break;
			case 0x00000027: // nor
				break;
			case 0x24000000: // addiu
				return Addiu(instr,RT,RS,Immediate);
			case 0x30000000: // andi
				break;
			case 0x34000000: // ori
				return Ori(instr,RT,RS,Immediate);
			default: // error
				break;
		}
		return (instr,"syscall","syscall"); //error
	}

	public ValueTuple<uint,string,string> Addi(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] + immediate;
		return (instr,$"addi {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"{(int)Registers[rs]} + {(int)immediate} = {(int)Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Addiu(uint instr, uint rt, uint rs, uint immediate) {
		Registers[rt] = Registers[rs] + (immediate & 0x0000FFFF);
		return (instr,$"addiu {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {(int)immediate}",$"{Registers[rs]} + {immediate} = {Registers[rt]}");
	}

	public ValueTuple<uint,string,string> Add(uint instr, uint rd, uint rs, uint rt) {
		// returns the operation in string form to display on the GUI
		// ( normally, it would just return void, but this is good information to have )
		// also, have the first part be assembly, separated with \n
		// EX: Add(_,12,6) -> 12 + 6 = 18 , maybe with some bbcode formatting
		Registers[rd] = Registers[rs] + Registers[rt];

		return (instr,$"add {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} + {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Sub(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] - Registers[rt];
		return (instr,$"sub {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} - {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> And(uint instr, uint rd, uint rs, uint rt) {
		Registers[rd] = Registers[rs] & Registers[rt];
		return (instr,$"and {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} & {Registers[rt]} = {Registers[rd]}");
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
		Registers[rt] = Registers[rs] | immediate;
		return (instr,$"ori {Compiler.register_names[rt]}, {Compiler.register_names[rs]}, {immediate}",$"{Registers[rt]} | {immediate} = {Registers[rt]}");
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
				
				break;
			case 5: // user int input
				AskingForInput = true;
				break;
			case 10: // quit
				ProgramActive = false;
				break;
		}
	}

	public void OnStepButtonPressed() {
		if (ProgramActive) {
			gui.UpdateStepDisplay(Step());
			gui.DisplayRegisters(Registers);
		}	
	}

	public async void OnRunButtonPressed() {
		ResetSPIM();
		//while program not exited
		while (ProgramActive) {
			//step through the program
			Step();
			
			if (AskingForInput) {
				GD.Print("Waiting");
				await ToSignal(gui.TextInput, LineEdit.SignalName.TextSubmitted);
				AskingForInput = false;

				switch (Registers[2]) {
					case 5:
						Registers[2] = (uint)Compiler.NumFromText(gui.TextInput.Text);
						break;
				}

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

		for (int i = 0; i < InstructionSet.Length; i++) {
			GD.Print($"{InstructionSet[i].ToString("X8")} - {InstructionSet[i].ToString("B32")}");
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
