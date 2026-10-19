using Godot;
using System;
using System.Diagnostics;

public partial class Enemy : CharacterBody2D, IDamagable, IMover
{
    Level level;
    [Export] public float Speed { get; private set; } = 25.0f;

    public int Health { get; private set; } = 5;

    [Export] private Vector2 _initialTargetPos = new Vector2();

    public Vector2 TargetGlobalPos
    {
        get { return _navAgent.TargetPosition; }
        set { _navAgent.TargetPosition = value;}
    }

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();
    public float TimePaused = 10;
    private bool Respawning = false;

    private NavigationAgent2D _navAgent;


    public override void _Ready()
    {
        level = GetTree().Root.GetNode<Level>("Level");
        OnKilled += OnEnemyKilled;

        _navAgent = GetNode<NavigationAgent2D>("NavAgent");



        Callable.From(FirstFrameSetup).CallDeferred();
    }
    public void OnEnemyKilled()
    {
        GD.Print("Enemy Died");
        RespawnPoint respawn = level.GetRespawnPoint(Team.ENEMY);
        this.Position = respawn.Position;
        Respawning = true;
        TimePaused = 10;
        this.Modulate = new Color(1, 0, 0, 0.7f);

        Health = 5;
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

            EmitSignal(SignalName.OnDamageTaken, damage);

            if (Health <= 0)
            {
                EmitSignal(SignalName.OnKilled);
            }
        }
    }
}
