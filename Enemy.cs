using Godot;
using System;

public partial class Enemy : CharacterBody2D, IDamagable
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	public int Health { get; private set; } = 5;

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();

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

    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
