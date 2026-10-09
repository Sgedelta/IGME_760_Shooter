using Godot;
using System;

public partial class Weapon : Node2D
{
	public Vector2 AimDir = Vector2.Zero;
	public float DisplayOrbitRadius = 100f;
	public float FireOrbitRadius = 125f;

	[Export]
	public PackedScene ProjectileScene;

	public Team WeaponTeam;

	[Export] public int MagazineSize = 15;
	public int LoadedBullets { get; private set; }

	[Signal] public delegate void OnFireEventHandler(int roundsLeft);
	[Signal] public delegate void OnReloadStartedEventHandler(float reloadTime);
	[Signal] public delegate void OnReloadFinishedEventHandler();

	private bool _CanFire = true;
	private bool _Reloading = false;

	private Node2D _firingPoint;
	private Node2D _displayNode;
	private Timer _fireTimer;
	private Timer _reloadTimer;



	private RandomNumberGenerator _RNG;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_firingPoint = GetNode<Node2D>("%FiringPoint");
		_displayNode = GetNode<Sprite2D>("%Display");
		_fireTimer = GetNode<Timer>("%FireTimer");
        _reloadTimer = GetNode<Timer>("%ReloadTimer");

		_fireTimer.Timeout += () => { _CanFire = !_Reloading; };
		_reloadTimer.Timeout += () => { 
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
		_firingPoint.Position = AimDir * FireOrbitRadius;
		_displayNode.Position = AimDir * DisplayOrbitRadius;
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
		_fireTimer.Start();

		Projectile firedProjectile = ProjectileScene.Instantiate<Projectile>();

		firedProjectile.GlobalPosition = _firingPoint.GlobalPosition;
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
		EmitSignal(SignalName.OnFire, LoadedBullets);

	}

	public void Reload()
	{
		_CanFire = false;
		_Reloading = true;
		EmitSignal(SignalName.OnReloadStarted, _reloadTimer.WaitTime);
		_reloadTimer.Start();
		GD.Print("Reloading..");
	}
}
