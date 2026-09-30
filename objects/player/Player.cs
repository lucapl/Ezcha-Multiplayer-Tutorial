using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Gravity = 1024.0f;
	[Export]
	public float Speed = 512.0f;
	[Export]
	public float JumpVelocity = 512.0f;
	[Export]
	public float GroundAccel = 1024.0f;
	[Export]
	public float AirAccel = 512.0f;
	[Export]
	public float Friction = 1024.0f;
	[Export]
	public float SlideFriction = 512.0f;
	[Export]
	public float AirDrag = 512.0f;
	[Export]
	public ulong HitCooldownMs = 250;

	private readonly Godot.SpriteFrames[] Characters = new Godot.SpriteFrames[]
	{
		ResourceLoader.Load<Godot.SpriteFrames>("res://objects/player/sprites/Character_Green.tres"),
		ResourceLoader.Load<Godot.SpriteFrames>("res://objects/player/sprites/Character_Purple.tres"),
		// ResourceLoader.Load<Godot.SpriteFrames>("res://objects/player/sprite/Character_Beige.tres"),
		// ResourceLoader.Load<Godot.SpriteFrames>("res://objects/player/sprite/Character_Pink.tres"),
		// ResourceLoader.Load<Godot.SpriteFrames>("res://objects/player/sprite/Character_Yellow.tres"),
	};

	private readonly string[] Animations =
	{
		"default",
		"walk",
		"jump",
		"fall",
		"duck",
		"hit"
	};
	public enum State
	{
		IDLE = 0,
		WALK = 1,
		JUMP = 2,
		FALL = 3,
		DUCK = 4,
		HIT = 5
	}

	private AnimatedSprite2D _AnimatedSprite2D;
	private Camera2D _Camera;

	private int _PeerId = 1;
	private bool _Local = true;
	[Export]
	public int CoinsCollected = 0;
	private ulong _HitTimeMs = 0;
	[Export]
	public int Character = 0;
	private Godot.Vector2 _Velocity = Godot.Vector2.Zero;
	private Godot.Vector2 _InputVector = Godot.Vector2.Zero;
	private Godot.CollisionShape2D _NormalCollision;
	private Godot.CollisionShape2D _DuckCollision;

	public void SetCharacter(int character)
	{
		Character = Mathf.Clamp(character, 0, Characters.Length - 1);
	}

	private State _currentState = State.IDLE;
	[Export]
	public State CurrentState{
		set {
			_currentState = value;
			_AnimatedSprite2D.Animation = Animations[(int)_currentState];
		}
		get
		{
			return _currentState;
		}
	}
	private float _direction = 1.0f;
	[Export]
	public float Direction
	{
		set
		{
			_direction = value>0.0f?1.0f:-1.0f;
			_AnimatedSprite2D.FlipH = _direction < 0.0f;
		}
		get
		{
			return _direction;
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		_PeerId = int.Parse(Name);
		GetNode<MultiplayerSynchronizer>("ClientSynchronizer").SetMultiplayerAuthority(_PeerId);
		//_Local = _PeerId == Multiplayer.GetUniqueId();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal=true,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Teleport(Vector2 newPosition)
	{
		Velocity = Vector2.Zero;
		GlobalPosition = newPosition;
		CurrentState = State.IDLE;
	}


	public override void _Ready()
	{
		_AnimatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_Camera = GetNode<Camera2D>("Camera2D");
		_NormalCollision = GetNode<CollisionShape2D>("NormalCollision");
		_DuckCollision = GetNode<CollisionShape2D>("DuckCollision");
		CurrentState = State.IDLE;
		if (_Local)
		{
			_Camera.MakeCurrent();
		}
	}
	private void _ProcessInput(double delta)
	{
		_InputVector = Input.GetVector("move_left","move_right","move_up","move_down");
	}
	private void _ProcessMovement(float delta)
	{
		_Velocity.Y += Gravity * delta;

		if (Mathf.IsZeroApprox(_InputVector.X) && !Mathf.IsZeroApprox(_Velocity.X))
		{
			float reduction = IsOnFloor()?Friction:AirDrag;
			_Velocity.X = Mathf.MoveToward(_Velocity.X,0.0f,reduction*delta);
		}

		switch(CurrentState)
		{
			case State.IDLE: 
				_StateIdle(delta);
				break;
			case State.WALK:
				_StateWalk(delta);
				break;
			case State.JUMP:
				_StateJump(delta);
				break;
			case State.DUCK:
				_StateDuck(delta);
				break;
			case State.FALL:
				_StateFall(delta);
				break;
			case State.HIT:
				_StateHit(delta);
				break;
		}

		Velocity = _Velocity;
		MoveAndSlide();
	}

	private void _StateIdle(float delta)
	{
		if(_CheckFall()) return;
		if(_CheckWalk()) return;
		if(_CheckJump()) return;
		if(_CheckDuck()) return;
		_GroundControls(delta);
	}

	private void _StateWalk(float delta)
	{
		if(_CheckFall()) return;
		if(_CheckJump()) return;
		if(_CheckDuck()) return;
		if(_CheckIdle()) return;
		_GroundControls(delta);
	}

	private void _StateJump(float delta)
	{
		if(_CheckFall()) return;
		if(_CheckDuck()) return;
		_AirControls(delta);
	}

	private void _StateFall(float delta)
	{
		if(_CheckDuck()) return;
		if(!IsOnFloor())
		{
			_AirControls(delta);
			return;
		}
		CurrentState = State.IDLE;
	}

	private void _StateDuck(float delta)
	{
		_Velocity.X = Mathf.MoveToward(_Velocity.X, 0.0f, SlideFriction * delta);
		if(!IsOnFloor()) return;
		if(_CheckDuck()) return;
		CurrentState = State.IDLE;
	}

	private void _StateHit(float delta)
	{
		ulong nowMs = Time.GetTicksMsec();
		if (nowMs - _HitTimeMs < HitCooldownMs) return;
		CurrentState = State.IDLE;
	}

	public void CollectCoins(int amount)
	{
		CoinsCollected += amount;
	}

	public void Jump()
	{
		if (CurrentState == State.JUMP) return;
		CurrentState = State.JUMP;
		_Velocity.Y = -JumpVelocity;
	}

	public void Duck()
	{
		if (CurrentState == State.DUCK) return;
		CurrentState = State.DUCK;
		_Velocity.Y = JumpVelocity;
	}

	public void HitBy(Node2D other)
	{
		if (CurrentState == State.HIT) return;
		_HitTimeMs = Time.GetTicksMsec();
		CurrentState = State.HIT;
		_Velocity = GlobalPosition.DirectionTo(other.GlobalPosition) * -Gravity;
		Direction = Mathf.Round(_Velocity.X);
	}

	private bool _CheckFall()
	{
		if(!IsOnFloor() && _Velocity.Y > 0.0f)
		{
			CurrentState = State.FALL;
			return true;
		}
		return false;
	}

	private bool _CheckJump()
	{
		if(_InputVector.Y < 0.0f && IsOnFloor())
		{
			Jump();
			return true;
		}
		return false;
	}

	private bool _CheckDuck()
	{
		if(_InputVector.Y > 0.0f)
		{
			Duck();
			_DuckCollision.Disabled = false;
			_NormalCollision.Disabled = true;
			return true;
		}
		_DuckCollision.Disabled = true;
		_NormalCollision.Disabled = false;
		return false;
	}

	private bool _CheckWalk()
	{
		if(!Mathf.IsZeroApprox(_InputVector.X))
		{
			CurrentState = State.WALK;
			return true;
		}
		return false;
	}

	private bool _CheckIdle()
	{
		if (Mathf.IsZeroApprox(_InputVector.X))
		{
			CurrentState = State.IDLE;
			return true;
		}
		return false;
	}

	private void _GroundControls(float delta)
	{
		if(!Mathf.IsZeroApprox(_InputVector.X))
		{
			Direction = Mathf.Round(_InputVector.X);
		}
		_Velocity.X = Mathf.MoveToward(_Velocity.X, Speed*_InputVector.X, GroundAccel*delta);
	}

	private void _AirControls(float delta)
	{
		if(!Mathf.IsZeroApprox(_InputVector.X))
		{
			Direction = Mathf.Round(_InputVector.X);
		}
		_Velocity.X = Mathf.MoveToward(_Velocity.X, Speed*_InputVector.X, AirAccel*delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_Local) return;

		_ProcessInput((float)delta);
		_ProcessMovement((float)delta);
	}
}
