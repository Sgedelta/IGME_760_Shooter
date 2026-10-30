using Godot;
using System;
using System.Diagnostics;

public partial class Enemy : CharacterBody2D, IDamagable, IMover
{
    Level level;
    [Export] public float Speed { get; private set; } = 25.0f;

    public int Health { get; private set; } = 5;
    private int _MaxHealth;

    [Export] public float RegenSpeed { get; private set; } = 7;

    [Export] public float TimeUntilRegen { get; private set; } = 15;

    [Export] private Vector2 _initialTargetPos = new Vector2();

    public Vector2 TargetGlobalPos
    {
        get { return _navAgent.TargetPosition; }
        set { _navAgent.TargetPosition = value;}
    }

    [Export] public bool AUTO_RELOAD = true;

    [Export(PropertyHint.Range, "0.0f, 180.0f, 0.1f, radians_as_degrees")] public float AimVariance;
    [Export(PropertyHint.Range, "0.0f, 360.0f, 0.1f, radians_as_degrees")] public float RotationSpeedPerSec;


    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();
    public float TimePaused = 10;
    private bool Respawning = false;

    private NavigationAgent2D _navAgent;

    private Weapon _weapon;

    private RandomNumberGenerator _rng;
    private Tween _aimTween;

    private Timer _damageTimer;
    private Timer _regenTimer;

    public override void _Ready()
    {
        level = GetTree().Root.GetNode<Level>("Level");
        OnKilled += OnEnemyKilled;

        _navAgent = GetNode<NavigationAgent2D>("NavAgent");

        _weapon = GetNode<Weapon>("Weapon");
        _weapon.WeaponTeam = Team.ENEMY;

        _MaxHealth = Health;

        _rng = new RandomNumberGenerator();

        Callable.From(FirstFrameSetup).CallDeferred();

        // regen timer stuff
        _damageTimer = new Timer();
        _damageTimer.OneShot = true;
        _regenTimer = new Timer();
        _regenTimer.OneShot = false; // think this defaults to this but making sure
        AddChild(_damageTimer);
        AddChild(_regenTimer);
        _damageTimer.Timeout += () =>
        {
            _regenTimer.Start(TimeUntilRegen);
        };
        _regenTimer.Timeout += () =>
        {

            Health = Mathf.Clamp(Health + 1, 0, _MaxHealth);
        };
    }
    public void OnEnemyKilled()
    {
        GD.Print("Enemy Died");
        RespawnPoint respawn = level.GetRespawnPoint(Team.ENEMY);
        this.Position = respawn.Position;
        Respawning = true;
        TimePaused = 10;
        this.Modulate = new Color(1, 0, 0, 0.7f);

        Health = _MaxHealth;
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Respawning)
        {
            TimePaused -= (float)delta;
            if (TimePaused <= 0)
            {
                Respawning = false;
                this.Modulate = new Color(1, 0, 0, 1f);
            }
        }
        FireAtPosition(Vector2.Zero, false);
    }

    public override void _PhysicsProcess(double delta)
	{
        // movement 
        if(!_navAgent.IsNavigationFinished() && !Respawning)
        {
            Vector2 direction = GlobalPosition.DirectionTo(_navAgent.GetNextPathPosition());

            MoveInDir(direction);
        }
	}

    private async void FirstFrameSetup()
    {
        // needed for nav server to sync
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        TargetGlobalPos = _initialTargetPos;
    }

    public void MoveInDir(Vector2 dir)
    {
        Vector2 velocity = Velocity;

        if (dir != Vector2.Zero)
        {
            velocity = dir * Speed;
        }
        else
        {
            velocity = velocity.MoveToward(Vector2.Zero, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void TakeDamage(int damage)
    {
        if (!Respawning)
        {
            GD.Print($"{Name}: Ow! took {damage} damage");
            Health -= damage;

            _regenTimer.Stop();
            _damageTimer.Start(TimeUntilRegen);

            EmitSignal(SignalName.OnDamageTaken, damage);

            if (Health <= 0)
            {
                EmitSignal(SignalName.OnKilled);
            }
        }


    }

    public bool LookAtPosition(Vector2 targetGlobalPos, float rotationChange, bool overrideCurrent)
    {
        // check if we can fire / if we need to override
        if (IsInstanceValid(_aimTween) && _aimTween.IsRunning())
        {
            if (overrideCurrent)
            {
                _aimTween.Kill();
            }
            else
            {
                return false;
            }
        }

        // localize position
        Vector2 dir = targetGlobalPos - GlobalPosition;
        dir = dir.Normalized();

        // rotate dir
        dir = dir.Rotated(rotationChange);

        // do the thing.
        _aimTween = CreateTween();

        // aim
        float time = Mathf.Abs(_weapon.AimDir.AngleTo(dir)) / RotationSpeedPerSec;
        _aimTween.TweenProperty(_weapon, "AimDir", dir, time);
        return true;
    }

    public void FireAtPosition(Vector2 targetGlobalPos, bool overrideCurrent)
    {
        // check if this makes sense to even do, because reload
        if (AUTO_RELOAD && _weapon.LoadedBullets <= 0 && !_weapon.Reloading)
        {
            _weapon.Reload();
            return;
        }

        if(!LookAtPosition(targetGlobalPos, _rng.RandfRange(-AimVariance, AimVariance), overrideCurrent))
        {
            return;
        }

        // fire
        _aimTween.Finished += _weapon.TryFire;
    }


}
