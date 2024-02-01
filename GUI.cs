using Godot;
using System;

public enum Mode {
	Coding,
	Running
}

public enum DataRepresentation {
	Text,
	Assembly,
	Hex,
	Binary,
	Decimal
}

public partial class GUI : MarginContainer
{
	private GridContainer LeftRegisters = null, RightRegisters = null, CurrentInstruction = null;
	private PanelContainer PlayerButtons = null;
	private HBoxContainer InputChoiceButtons = null;
	private RichTextLabel TextOutput = null;
	public LineEdit TextInput = null;
	public TextEdit CompileInput = null;
	private register_group InstructionSetDisplay = null;

	private Color error_color = new Color("#eb3e31");
	private Mode current_state = Mode.Coding;
	private DataRepresentation current_input = DataRepresentation.Assembly, register_display = DataRepresentation.Hex;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LeftRegisters         = (GridContainer) GetNode("CenterContainer/HBoxContainer/Registers/MarginContainer/VBoxContainer/HBoxContainer/PanelContainer/MarginContainer/RegisterContainer");
		RightRegisters        = (GridContainer) GetNode("CenterContainer/HBoxContainer/Registers/MarginContainer/VBoxContainer/HBoxContainer/PanelContainer2/MarginContainer/RegisterContainer2");
		PlayerButtons         = (PanelContainer)GetNode("CenterContainer/HBoxContainer/PanelContainer/MarginContainer/Input/PlayerButtons");
		InputChoiceButtons    = (HBoxContainer) GetNode("CenterContainer/HBoxContainer/PanelContainer/MarginContainer/Input/InputPanel/MarginContainer/FormatChoiceBox");
		CurrentInstruction    = (GridContainer) GetNode("CenterContainer/HBoxContainer/OpOutBox/CurrentInstructionBox/MarginContainer/CurrentInstructionContainer");
		TextOutput            = (RichTextLabel) GetNode("CenterContainer/HBoxContainer/OpOutBox/VBoxContainer/PanelContainer/MarginContainer/VBoxContainer/Output");
		TextInput             = (LineEdit)      GetNode("CenterContainer/HBoxContainer/OpOutBox/VBoxContainer/PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/LineEdit");
		CompileInput          = (TextEdit)      GetNode("CenterContainer/HBoxContainer/PanelContainer/MarginContainer/Input/InputEditor");
		InstructionSetDisplay = (register_group)GetNode("CenterContainer/HBoxContainer/PanelContainer/MarginContainer/Input/InstructionSet");
	}

	public void OnCompileButtonPressed() {
		//connected to compile button
		//Either Compiles the assembly inputted or turns the hex uinto a good format
		//Sends the data to Main, and Updates the GUI
		//Turns the input screen to be the instruction set
		//hides all the buttons, and a new button to cancel appears

		current_state = Mode.Running;
		(PlayerButtons.GetChild(0) as Control).Show();
		(PlayerButtons.GetChild(1) as Control).Hide();
		(PlayerButtons.GetChild(2) as Control).Show();
		InputChoiceButtons.Hide();
	}

	public void OnCodeButtonPressed() {
		//Reset all the GUI
		//flush the instruction set and registers
		//prevent the user from uinteracting with all that

		current_state = Mode.Coding;
		(PlayerButtons.GetChild(0) as Control).Hide();
		(PlayerButtons.GetChild(1) as Control).Show();
		(PlayerButtons.GetChild(2) as Control).Hide();
		InputChoiceButtons.Show();

		SwapToInput();
	}

	public void DisplayRegisters(uint[] registers)
	{
		for (int i = 0; i < 16; i++) {
			(LeftRegisters. GetChild(2 * i + 1) as register_display).SetData(registers[i   ],register_display);
			(RightRegisters.GetChild(2 * i + 1) as register_display).SetData(registers[i+16],register_display);
		}
	}

	public void UpdateStepDisplay(ValueTuple<uint,string,string> stepinfo) {
		(CurrentInstruction.GetChild(3) as register_display).SetText("0x" + stepinfo.Item1.ToString("X8"));
		(CurrentInstruction.GetChild(5) as register_display).SetText(stepinfo.Item2);
		(CurrentInstruction.GetChild(7) as register_display).SetText(stepinfo.Item3);
	}

	public void OnRegisterDisplayChosen(uint index) {
		switch (index) {
			case 0: //Hex
				register_display = DataRepresentation.Hex;
				break;
			case 1: //Binary
				register_display = DataRepresentation.Binary;
				break;
			case 2: //Decimal
				register_display = DataRepresentation.Decimal;
				break;
			default: // throw error, reminder to update this part of the code
				break;
		}

		for (int i = 0; i < 16; i++) {
			(LeftRegisters. GetChild(2 * i - 1) as register_display).SetFormat(register_display);
			(RightRegisters.GetChild(2 * i - 1) as register_display).SetFormat(register_display);
		}
	}

	public void AddToOutput(string text) {
		TextOutput.AppendText(text);
	}

	public void ClearOutput() {
		TextOutput.Clear();
	}

	public void HighlightLine(int line) {
		CompileInput.SetLineBackgroundColor(line, error_color);
	}

	public void ClearHighlight(int line) {
		CompileInput.SetLineBackgroundColor(line, new Color(error_color,0f));
	}

	public void OnInputEditted(int from, int to) {
		for (int i = from; i <= to; i++) {
			ClearHighlight(i);
		}
	}

	public void SwapToInstructionSet(byte[] instructions) {
		InstructionSetDisplay.Show();
		CompileInput.Hide();
		InstructionSetDisplay.SetGroup(instructions);
	}
	public void SwapToInput() {
		InstructionSetDisplay.Hide();
		CompileInput.Show();
	}

	public void OnAsmInputButtonPressed() {
		SetInputMode(DataRepresentation.Assembly);
	}
	public void OnHexInputButtonPressed() {
		SetInputMode(DataRepresentation.Hex);
	}
	public void OnBinInputButtonPressed() {
		SetInputMode(DataRepresentation.Binary);
	}

	private void SetInputMode(DataRepresentation mode) {
		(InputChoiceButtons.GetChild(0) as Button).ButtonPressed = (mode == DataRepresentation.Assembly);
		(InputChoiceButtons.GetChild(1) as Button).ButtonPressed = (mode == DataRepresentation.Hex);
		(InputChoiceButtons.GetChild(2) as Button).ButtonPressed = (mode == DataRepresentation.Binary);
		current_input = mode;
	}
}
