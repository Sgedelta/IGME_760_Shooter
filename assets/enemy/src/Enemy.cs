using Godot;
using System;

public partial class Enemy : CharacterBody2D, IDamagable, IMover
{
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

    private NavigationAgent2D _navAgent;

    public override void _Ready()
    {
        _navAgent = GetNode<NavigationAgent2D>("NavAgent");



        Callable.From(FirstFrameSetup).CallDeferred();
    }

    public override void _PhysicsProcess(double delta)
	{
		
        // movement 
        if(!_navAgent.IsNavigationFinished())
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
        GD.Print($"{Name}: Ow! took {damage} damage");
        Health -= damage;

        EmitSignal(SignalName.OnDamageTaken, damage);

        if (Health < 0)
        {
            EmitSignal(SignalName.OnKilled);
        }
    }

}
