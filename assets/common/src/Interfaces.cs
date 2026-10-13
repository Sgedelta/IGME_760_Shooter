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

	public void MoveInDir(Vector2 dir);
}

public interface IPickupable
{
	public ICarrier Carrier { get; }

	public bool BeingCarried { get;  }

	public bool Pickup(ICarrier by);

	public bool Drop();
}

public interface ICarrier
{

	public bool CanPickup { get; }

	public IPickupable Carrying { get; }

	public bool TryPickup(IPickupable item);

	public bool TryDrop(IPickupable item);

	public Callable ConnectToOnPickup(Action action);
	public void DisconnectFromOnPickup(Callable call);

	public Vector2 DropVel { get; }

	public Vector2 CarryPos { get; }
}
