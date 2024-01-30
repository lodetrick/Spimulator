using Godot;
using System;

public partial class register_group : PanelContainer
{
	private uint[] RegisterGroup;
	private VBoxContainer RegisterParent = null;
	private int length;
	PackedScene register_scene;
	
	public override void _Ready()
	{
		register_scene = GD.Load<PackedScene>("res://register_display.tscn");
		RegisterParent = (VBoxContainer)GetNode("MarginContainer/ScrollContainer/VBoxContainer");
	}

	public void SetGroup(uint[] group) {
		RegisterGroup = group;
		Resize(group.Length);
		//loop through group, set register texts
		for (int i = 0; i < group.Length; i++) {
			(RegisterParent.GetChild(i) as register_display).SetData(group[i],DataRepresentation.Hex);
		}
	}

	public void Resize(int newl) {
		if (newl > length) {
			for (int i = length; i < newl; i++) {
				var instance = register_scene.Instantiate();
				RegisterParent.AddChild(instance);
			}
		}
		else if (newl < length) {
			for (int i = length - 1; i >= newl; i--) {
				RegisterParent.GetChild(i).QueueFree();
			}
		}
		length = newl;
	}
}
