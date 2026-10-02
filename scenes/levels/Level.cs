using Godot;

public partial class Level : Node2D
{
	[Signal]
	public delegate void GoalReachedEventHandler(Player player);

	private readonly Texture2D[] _SurfaceTextures = 
	[
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_color_trees.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_color_hills.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_color_desert.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_color_mushrooms.tres"),
	];
	private readonly Texture2D[] _GroundTextures = 
	[
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_solid_grass.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_solid_grass.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_color_sand.tres"),
		ResourceLoader.Load<Texture2D>("res://objects/levels/sprites/background/background_solid_dirt.tres"),
	];

	public enum BackgroundEnum {
		TREES = 0,
		HILLS = 1,
		DESERT = 2,
		MUSHROOMS = 3
	}

	[Export]
	public string DisplayName = "Untitled Level";
	[Export]
	public int WorldNumber = 0;
	[Export]
	public int LevelNumber = 0;
	private BackgroundEnum _background = BackgroundEnum.HILLS;
	[Export]
	public BackgroundEnum Background
	{
		get
		{
			return _background;
		}

		set
		{
			_background = value;
			GetNode<TextureRect>("Parallax2D/Hills").Texture = _SurfaceTextures[(int)_background];
			GetNode<TextureRect>("Parallax2D/Surface").Texture = _GroundTextures[(int)_background];
		}
	}

	public Vector2 GetSpawnPosition()
	{
		return GetNode<Marker2D>("Spawn").GlobalPosition;
	}

	public void OnGoalBodyEntered(Node2D body)
	{
		if (body is Player localPlayer)
		{
			EmitSignal("GoalReached",localPlayer);
		}
	}
}
