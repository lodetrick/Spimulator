using Godot;
using System;

public partial class Main : Node2D //this is the spimulator
{
	private GUI gui = null;
	private uint ProgramCounter = 0;
	private uint[] InstructionSet;
	private uint[] Registers;
	public override void _Ready()
	{
		gui = (GUI)GetNode("CanvasLayer/GUI");

		Registers = new uint[32];
		Array.Fill<uint>(Registers,0);
	}

	public ValueTuple<uint,string,string> Step()
	{
		//Increment Program Counter
		ProgramCounter += 4;
		//Find register pouinted to by program counter
		uint instr = InstructionSet[ProgramCounter / 4];
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
				return Addi(RD,RS,Immediate,instr);
			case 0x00000020:  // add
				return Add(RD,RS,RT,instr);
			case 0x00000022:  // sub
				return Sub(RD,RS,RT,instr);
			case 0x00000024:  // and
				return And(RD,RS,RT,instr);
			case 0x3C000000:  // lui
				return Lui(RD,Immediate,instr);
			case 0x00000000:  // sll
				return SLL(RD,RT,Shamt,instr);
			case 0x00000002:  // srl
				return SRL(RD,RT,Shamt,instr);
			case 0x00000003:  // sra
				return SRA(RD,RT,Shamt,instr);
			case 0x0000000C:  // syscall
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
				break;
			case 0x30000000: // andi
				break;
			case 0x34000000: // ori
				break;
			default: // error
				break;
		}

		return (0,"",""); //error
	}

	public ValueTuple<uint,string,string> Addi(uint rd, uint rs, uint immediate, uint instr) {
		Registers[rd] = Registers[rs] + immediate;
		return (instr,$"addi {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {immediate}",$"{Registers[rs]} + {immediate} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Add(uint rd, uint rs, uint rt, uint instr) {
		// returns the operation in string form to display on the GUI
		// ( normally, it would just return void, but this is good information to have )
		// also, have the first part be assembly, separated with \n
		// EX: Add(_,12,6) -> 12 + 6 = 18 , maybe with some bbcode formatting
		Registers[rd] = Registers[rs] + Registers[rt];

		return (instr,$"add {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} + {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Sub(uint rd, uint rs, uint rt, uint instr) {
		Registers[rd] = Registers[rs] - Registers[rt];
		return (instr,$"sub {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} - {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> And(uint rd, uint rs, uint rt, uint instr) {
		Registers[rd] = Registers[rs] & Registers[rt];
		return (instr,$"and {Compiler.register_names[rd]}, {Compiler.register_names[rs]}, {Compiler.register_names[rt]}",$"{Registers[rs]} & {Registers[rt]} = {Registers[rd]}");
	}

	public ValueTuple<uint,string,string> Lui(uint rd, uint immediate, uint instr) {
		Registers[rd] = immediate << 16;
		return (instr,$"lui {Compiler.register_names[rd]}, {immediate}",$"{Compiler.register_names[rd]} = {immediate} << 16");
	}

	public ValueTuple<uint,string,string> SLL(uint rd, uint rt, int shamt, uint instr) {
		Registers[rd] = Registers[rt] << shamt;
		return (instr,$"sll {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} << {shamt}");
	}

	public ValueTuple<uint,string,string> SRL(uint rd, uint rt, int shamt, uint instr) {
		Registers[rd] = Registers[rt] >>> shamt;
		return (instr,$"srl {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} >> {shamt}");
	}

	public ValueTuple<uint,string,string> SRA(uint rd, uint rt, int shamt, uint instr) {
		Registers[rd] = Registers[rt] >> shamt;
		return (instr,$"sra {Compiler.register_names[rd]}, {Compiler.register_names[rt]}, {shamt}",$"{Registers[rd]} = {Registers[rt]} >> {shamt}");
	}

	public void OnStepButtonPressed() {
		gui.UpdateStepDisplay(Step());
		gui.DisplayRegisters(Registers);
	}

	public void OnCompileButtonPressed() {
		string assembly = (GetNode("CanvasLayer/GUI/CenterContainer/HBoxContainer/PanelContainer/MarginContainer/Input/InputEditor") as TextEdit).Text;
		InstructionSet = Compiler.Compile(assembly);

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
		return ((instr & 0x0000FFFF) << 16) >> 16;
	}

	public static uint GetAddress(uint instr) {
		return instr & 0x03FF_FFFF;
	}
}
