using Godot;
using System;

public partial class Player : CharacterBody2D, IDamagable
{
	[Export] public float Speed = 25.0f;

    [Export] public int Health { get; private set; } = 3;

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();

    private Weapon _Weapon;

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

    public override void _Ready()
    {
        _Weapon = GetNode<Weapon>("Weapon");
        _Weapon.WeaponTeam = Team.PLAYER;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (direction != Vector2.Zero)
        {
            velocity = direction * Speed;
        }
        else
        {
            velocity = velocity.MoveToward(Vector2.Zero, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        _Weapon.AimDir = GetLocalMousePosition().Normalized();

    }

    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("fire"))
        {
            _Weapon.TryFire();
        }
        if(@event.IsActionPressed("reload"))
        {
            _Weapon.Reload();
        }
    }
}
