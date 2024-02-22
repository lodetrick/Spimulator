using Godot;
using System;

public partial class register_display : PanelContainer
{
	[Export]
	public String text = "0xFFFF0572";
	public uint data = 0xFFFF0572;
	private DataRepresentation format = DataRepresentation.Text;
	private Label label;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		label = GetNode("MarginContainer/Label") as Label;
		label.Text = text;
	}

	public void SetText(string text) {
		this.text = text;
		label.Text = text;
	}

	public void UpdateTextFromData() {
		label.Set("theme_override_font_sizes/font_size",format == DataRepresentation.Binary ? 10 : 16);

		switch (format) {
			case DataRepresentation.Text:
				break;
			case DataRepresentation.Hex:
				text = "0x" + data.ToString("X8");
				break;
			case DataRepresentation.Binary:
				text = "0b" + data.ToString("B32");
				break;
			case DataRepresentation.Decimal:
				text = ((int)data).ToString("D");
				break;
			default:
				break;//throw error
		}
		label.Text = text;
	}

	public void SetData(uint data) {
		this.data = data;
		//update text
		UpdateTextFromData();
	}

	public void SetData(uint data, DataRepresentation format) {
		this.data = data;
		this.format = format;
		//update text
		UpdateTextFromData();
	}

	public void SetFormat(DataRepresentation format) {
		this.format = format;
		// update text
		UpdateTextFromData();
	}
}