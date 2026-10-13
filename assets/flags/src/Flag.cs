using Godot;
using System;

public partial class Flag : RigidBody2D, IPickupable
{
    public ICarrier Carrier { get; private set; }

    [Export] private float _DragSpeed;

    public bool BeingCarried
    {
        get; private set;
        //option that will always work
        /*
        get {
            return IsInstanceValid((Carrier as GodotObject));
        } */
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        if (BeingCarried)
        {
            // set position to the position we need to be at 
            GlobalPosition = Carrier.CarryPos;
        }
        
	}

    public bool Pickup(ICarrier by)
    {
        // if this is being carried, we can't pick it up
        if(BeingCarried)
        {
            return false;
        }

        // make sure the thing picking us up is safe
        if(!IsInstanceValid((by as GodotObject)))
        {
            return false;
        }

        // all good, get picked up
        Carrier = by;
        BeingCarried = true;
        Freeze = true;
        return true;
    }

    public bool Drop()
    {
        // if this is not being carried, we can't drop it
        if (!BeingCarried)
        {
            return false;
        }

        // otherwise, drop it. Grab some internal values first though.

        Vector2 dropVel = Carrier.DropVel;

        Carrier = null;
        BeingCarried = false;
        Freeze = false;

        ApplyImpulse(dropVel);

        return true;
    }

    public void OnBodyEnter(Node2D body)
    {
        if(body is Player p)
        {
            p.RegisterFlagTouched(this);
        }
    }

    public void OnBodyExit(Node2D body)
    {
        if (body is Player p)
        {
            p.RegisterFlagLeft(this);
        }
    }
}
