using Godot;
using System;
using System.Diagnostics;

public partial class Projectile : Area2D
{
	public Vector2 Direction = Vector2.Zero;
	public float Speed = 50; //px/sec
	public bool UpdateFacing = true;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
		
		if( UpdateFacing )
		{
			GlobalRotationDegrees = Direction.Angle(); 
		}

	}

	public void BodyEnteredArea(Node2D other)
	{
		GD.Print("Other Body Entered " + other.Name);
		if((other.IsInGroup("enemy_team") && IsInGroup("player_team")) || 
			(other.IsInGroup("player_team") & IsInGroup("enemy_team")))
		{
			// damage other or destroy other if it's another bullet
			if(other is IDamagable dmg)
			{
				dmg.TakeDamage(1);
			}

			QueueFree();
		}
		else if ((other is CollisionObject2D collisionObj && collisionObj.GetCollisionLayerValue(1)) || other is TileMapLayer)
		{
			// we hit a collision object, destroy self
			QueueFree();
		}
		else if (other is Projectile otherProjectile)
		{
			// we hit another projectile, so we should destroy. Other should trigger this on itself or.
			QueueFree();
		}
	}

	public void DestroySelf()
	{
		QueueFree();
	}
}
