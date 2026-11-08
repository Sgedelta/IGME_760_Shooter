using Godot;
using Godot.Collections;
using System;

public partial class Flag : RigidBody2D, IPickupable
{
    [Signal] public delegate void OnFlagReturnEventHandler();
    [Signal] public delegate void OnFlagPickedUpEventHandler();

    public ICarrier Carrier { get; private set; }

    public Team team;

    [Export] private float _DragSpeed;
    [Export] private float _ReturnTime = 8;
    [Export] private float _ScalarPerEnemy = 1.5f;

    private TextureProgressBar _returnProgressBar;
    private Sprite2D _returnAreaDisplay;

    private Tween _countdownTween;

    private int _otherTeamInReturnCount = 0;
    private int _ourTeamInReturnCount = 0;

    public bool OnStand = true;

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
        _returnProgressBar = GetNode<TextureProgressBar>("ReturnProgress");
        _returnAreaDisplay = GetNode<Sprite2D>("%ReturnAreaDisplay");

        if(IsInGroup("enemy_team"))
        {
            team = Team.ENEMY;
        }
        else if (IsInGroup("player_team"))
        {
            team = Team.PLAYER;
        }

        InternalReset();
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
        OnStand = false;
        Carrier = by;
        BeingCarried = true;
        Freeze = true;
        CheckReturnTween(); 
        _returnProgressBar.Visible = false;
        _returnAreaDisplay.Visible = false;
        _returnProgressBar.Value = 360;
        EmitSignal(SignalName.OnFlagPickedUp);
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
        CheckReturnTween();
        _returnProgressBar.Visible = true;
        _returnAreaDisplay.Visible = true;
        ApplyImpulse(dropVel);

        return true;
    }

    public void CheckReturnTween()
    {
        // no matter what we are doing, kill the old tween if it's running.
        if(IsInstanceValid(_countdownTween) && _countdownTween.IsRunning())
        {
            _countdownTween.Kill();
        }

        // no countdown
        if(OnStand)
        {

            return;
        }

        // if any of the enemy team is in the area, no countdown
        if(BeingCarried || _otherTeamInReturnCount > 0)
        {
            return;
        }

        // otherwise, return countdown progresses.
        float timeLeft = ((float)_returnProgressBar.Value / 360.0f) * _ReturnTime / (_ourTeamInReturnCount > 0 ? _ScalarPerEnemy * _ourTeamInReturnCount : 1);
        _countdownTween = CreateTween();
        _countdownTween.TweenProperty(_returnProgressBar, "value", 0, timeLeft);
        _countdownTween.Finished += ReturnHitZero;
    }

    private void ReturnHitZero()
    {
        EmitSignal(SignalName.OnFlagReturn);
    }

    public void InternalReset()
    {
        OnStand = true;
        _returnProgressBar.Visible = false;
        _returnAreaDisplay.Visible = false;
        _returnProgressBar.Value = 360;
        CheckReturnTween(); 
    }

    public void OnBodyEnter(Node2D body)
    {
        if(body is Player p)
        {
            p.RegisterFlagTouched(this);
        }
        else if (body is Enemy e)
        {
            e.RegisterFlagTouched(this);
        }
    }

    public void OnBodyExit(Node2D body)
    {
        if (body is Player p)
        {
            p.RegisterFlagLeft(this);
        }
        else if (body is Enemy e)
        {
            e.RegisterFlagLeft(this);
        }
    }

    public void OnReturnAreaEnter(Node2D other)
    {
        if(other is Player p)
        {
            if (team == Team.PLAYER)
            {
                _ourTeamInReturnCount += 1;
            }
            else if(team == Team.ENEMY)
            {
                _otherTeamInReturnCount += 1;
            }
        }
        else if (other is Enemy e)
        {
            if (team == Team.ENEMY)
            {
                _ourTeamInReturnCount += 1;
            }
            else if (team == Team.PLAYER)
            {
                _otherTeamInReturnCount += 1;
            }
        }
        GD.Print("after enter of ", other.Name, ": ", _ourTeamInReturnCount, ", ", _otherTeamInReturnCount);
        CheckReturnTween();
    }

    public void OnReturnAreaExit(Node2D other)
    {
        if (other is Player p)
        {
            if (team == Team.PLAYER)
            {
                _ourTeamInReturnCount -= 1;
            }
            else if (team == Team.ENEMY)
            {
                _otherTeamInReturnCount -= 1;
            }
        }
        else if (other is Enemy e)
        {
            if (team == Team.ENEMY)
            {
                _ourTeamInReturnCount -= 1;
            }
            else if (team == Team.PLAYER)
            {
                _otherTeamInReturnCount -= 1;
            }
        }
        GD.Print("after exit of ", other.Name, ": ", _ourTeamInReturnCount, ", ", _otherTeamInReturnCount);
        CheckReturnTween();
    }


}
