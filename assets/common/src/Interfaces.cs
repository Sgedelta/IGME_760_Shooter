using Godot;
using System;


public interface IDamagable
{
	public int Health { get; }

	public void TakeDamage(int damage);

}

public interface IMover
{
	public float Speed { get; }

	public void MoveInDir(Vector2 dir)
	{

	}
}
