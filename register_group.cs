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

	public void SetGroup(byte[] group) {
		//  https://stackoverflow.com/a/7288935
		uint[] _group = new uint[group.Length / 4];
		Buffer.BlockCopy(group, 0, _group, 0, group.Length);
		//
		RegisterGroup = _group;
		Resize(_group.Length);
		//loop through group, set register texts
		for (int i = 0; i < _group.Length; i++) {
			(RegisterParent.GetChild(i) as register_display).SetData(_group[i],DataRepresentation.Hex);
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
