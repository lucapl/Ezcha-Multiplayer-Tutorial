using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float Speed = 256.0f;
	[Export]
	public float Gravity = 1024.0f;
	[Export]
	public float Acceleration = 512.0f;
	[Export]
	public float AirDrag = 512.0f;
	[Export]
	public float Height = 44.0f;
	[Export]
	public ulong TurnCooldownMs = 250;
	private ulong _TurnTimeMs;

	private AnimatedSprite2D _AnimatedSprite2D;
	private float _direction;
	[Export]
	public float Direction
	{
		get
		{
			return _direction;
		}
		set
		{
			_direction = value>0.0f?1.0f:-1.0f;
			_AnimatedSprite2D.FlipH = _direction>0.0f;
		}
	}
	private bool _destroyed;
	[Export]
	public bool Destroyed
	{
		get
		{
			return _destroyed;
		}
		set
		{
			_destroyed = value;
			Visible = !value;
			SetDeferred("process_mode",(int)(value?ProcessModeEnum.Disabled:ProcessModeEnum.Inherit));
		}
	}

	public override void _Ready()
	{
		_AnimatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Direction = 1.0f;
	}

	private Vector2 _Velocity;
	public override void _PhysicsProcess(double delta)
	{
		if(!IsMultiplayerAuthority()) return;
		float deltaf = (float)delta;

		_Velocity.Y += Gravity * deltaf;

		if(IsOnFloor())
		{
			_Velocity.X = Mathf.MoveToward(_Velocity.X,Speed*Direction, Acceleration*deltaf);
		}
		else
		{
			_Velocity.X = Mathf.MoveToward(_Velocity.X,0.0f, AirDrag*deltaf);
		}

		Velocity = _Velocity;
		MoveAndSlide();

		if(IsOnWall())
		{
			ulong now = Time.GetTicksMsec();
			if (now - _TurnTimeMs >= TurnCooldownMs)
			{
				_TurnTimeMs = now;
				Direction *= -1.0f;
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Destroy()
	{
		Destroyed = true;
	}
}
