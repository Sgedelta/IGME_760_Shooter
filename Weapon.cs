using Godot;
using System;

public partial class Weapon : Node2D
{
	public Vector2 AimDir = Vector2.Zero;
	public float DisplayOrbitRadius = 10f;
	public float FireOrbitRadius = 15f;

	[Export]
	public PackedScene ProjectileScene;

	public Team WeaponTeam;

	[Export] public int MagazineSize = 15;
	public int LoadedBullets { get; private set; }

	[Signal] public delegate void OnFireEventHandler();
	[Signal] public delegate void OnReloadStartedEventHandler();
	[Signal] public delegate void OnReloadFinishedEventHandler();

	private bool _CanFire = true;
	private bool _Reloading = false;

	private Node2D _FiringPoint;
	private Node2D _DisplayNode;
	private Timer _FireTimer;
	private Timer _ReloadTimer;

	private RandomNumberGenerator _RNG;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_FiringPoint = GetNode<Node2D>("%FiringPoint");
		_DisplayNode = GetNode<Sprite2D>("%Display");
		_FireTimer = GetNode<Timer>("%FireTimer");
        _ReloadTimer = GetNode<Timer>("%ReloadTimer");

		_FireTimer.Timeout += () => { _CanFire = !_Reloading; };
		_ReloadTimer.Timeout += () => { 
			LoadedBullets = MagazineSize; 
			_CanFire = true; 
			_Reloading = false; 
			EmitSignal(SignalName.OnReloadFinished);
			GD.Print("Reload Done");
		};

        LoadedBullets = MagazineSize;
		_RNG = new RandomNumberGenerator();
	}


	public override void _PhysicsProcess(double delta)
	{
		// Position firing point and display node using local position
		_FiringPoint.Position = AimDir * FireOrbitRadius;
		_DisplayNode.Position = AimDir * DisplayOrbitRadius;
	}

	public void TryFire()
	{
		if( !_CanFire || LoadedBullets <= 0 )
		{
			return;
		}

		_CanFire = false;
		LoadedBullets -= 1;
		GD.Print($"Fired! Bullets Left: {LoadedBullets}");
		_FireTimer.Start();

		Projectile firedProjectile = ProjectileScene.Instantiate<Projectile>();

		firedProjectile.GlobalPosition = _FiringPoint.GlobalPosition;
		firedProjectile.Direction = AimDir;

		switch(WeaponTeam)
		{
			case Team.PLAYER:
				firedProjectile.AddToGroup("player_team");
				break;
			case Team.ENEMY:
				firedProjectile.AddToGroup("enemy_team");
				break;
		}

		AddChild(firedProjectile);

	}

	public void Reload()
	{
		_CanFire = false;
		_Reloading = true;
		EmitSignal(SignalName.OnReloadStarted);
		_ReloadTimer.Start();
		GD.Print("Reloading..");
	}
}
