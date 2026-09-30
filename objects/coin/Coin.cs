using Godot;
using System;

public partial class Coin : Area2D
{
	private bool _collected = false;
	[Export]
	public bool Collected
	{
		get
		{
			return _collected;
		}
		set
		{
			_collected = value;
			Visible = !value;
			SetDeferred("process_mode",(int)(value?ProcessModeEnum.Disabled:ProcessModeEnum.Inherit));
		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("gold");
	}

	public void OnBodyEntered(Node2D body)
	{
		if (!IsMultiplayerAuthority()) return;

		if (!Visible) return;
		if (body is Player localPlayer)
		{
			localPlayer.CollectCoins(1);
			Collected = true;
		}
	}
}
