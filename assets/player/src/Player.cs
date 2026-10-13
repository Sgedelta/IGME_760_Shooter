using Godot;
using Godot.Collections;
using System;
using SCG = System.Collections.Generic;

public partial class Player : CharacterBody2D, IDamagable, IMover, ICarrier
{
    [Signal] public delegate void OnPickupEventHandler();

    [Export] public float Speed { get; private set; } = 25.0f;

    [Export] public int Health { get; private set; } = 3;

    public bool CanPickup {  get; private set; }

    public IPickupable Carrying { get; private set; }


    [Export] public float FlagThrowStr = 15;
    public Vector2 DropVel { get { return Velocity + GetLocalMousePosition().Normalized() * FlagThrowStr; } }

    // xy are mouse offset, where x is in the direction of the mouse's location and y is the right vector of that. Z is an additional rotation to apply after, in degrees.
    [Export] public Vector3 CarryOffsetAndRot = new Vector3();
    public Vector2 CarryPos
    {
        get
        {
            // grab mouse loc
            Vector2 add = GetLocalMousePosition().Normalized();
            Vector2 right = add.Rotated(MathF.PI / 2);
            // put xy into those terms, with X being in line with mouse pos and y being right of it (90 cw). add the rotation z
            add = (add * CarryOffsetAndRot.X + right * CarryOffsetAndRot.Y).Rotated(CarryOffsetAndRot.Z);

            // add to pos
            return GlobalPosition + add;
        }
    }

    [Export] public Vector4 ReloadOffsets = new Vector4(-.5f, .5f, 0, 0);

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();

    private Weapon _weapon;

    private TextureProgressBar _bulletsLeft;
    private ProgressBar _reloadProgress;

    private Sprite2D _visual;

    private SCG.HashSet<Flag> _touchingFlags;

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
        _weapon = GetNode<Weapon>("Weapon");
        _weapon.WeaponTeam = Team.PLAYER;

        _visual = GetNode<Sprite2D>("Visual");
        _bulletsLeft = GetNode<TextureProgressBar>("%BulletsBar");
        _reloadProgress = GetNode<ProgressBar>("%ReloadBar");

        SetupProgressBars();

        _weapon.OnFire += (v) => { _bulletsLeft.Value = v; };
        _weapon.OnReloadStarted += (t) =>
        {
            _bulletsLeft.Visible = false;
            _reloadProgress.Visible = true;
            _reloadProgress.Value = 0;
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(_reloadProgress, "value", 1, t).From(0);
        };
        _weapon.OnReloadFinished += () =>
        {
            _bulletsLeft.Value = _weapon.MagazineSize;
            _reloadProgress.Visible = false;
            _bulletsLeft.Visible = true;
        };

        _touchingFlags = new SCG.HashSet<Flag>();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        MoveInDir(direction);

        _weapon.AimDir = GetLocalMousePosition().Normalized();

    }

    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("fire"))
        {
            _weapon.TryFire();
        }
        if(@event.IsActionPressed("reload"))
        {
            _weapon.Reload();
        }
        if(@event.IsActionPressed("interact_generic"))
        {
            foreach(Flag f in _touchingFlags)
            {
                // for every flag we are touching, try picking it up. If that fails, try dropping it
                if (TryPickup(f))
                {
                    // stop checking if we pick a flag up
                    break;
                }
                else if (TryDrop(f))
                {
                    // stop checking if we drop a flag
                    break;
                }

            }
        }
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
            velocity = velocity.MoveToward(Vector2.Zero, Mathf.Max(Speed/15, velocity.Length() / 10));
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void SetupProgressBars()
    {
        //size
        SizeControlToSprite(_bulletsLeft);
        SizeControlToSprite(_reloadProgress);

        //set positions
        Vector2 texSize = _visual.Texture.GetSize();
        _bulletsLeft.Position = new Vector2(texSize.X * ReloadOffsets.X + ReloadOffsets.Z, texSize.Y * ReloadOffsets.Y + ReloadOffsets.W);
        _reloadProgress.Position = new Vector2(texSize.X * ReloadOffsets.X + ReloadOffsets.Z, texSize.Y * ReloadOffsets.Y + ReloadOffsets.W);

        //set values
        _bulletsLeft.MaxValue = _weapon.MagazineSize;
        _bulletsLeft.Value = _weapon.MagazineSize; //starts loaded

        _reloadProgress.Visible = false;

    }

    private void SizeControlToSprite(Control c)
    {
        if(_visual.Texture == null)
        {
            return;
        }

        float size = _visual.Texture.GetSize().X;

        c.CustomMinimumSize = new Vector2(size, c.CustomMinimumSize.Y);
        c.CustomMaximumSize = new Vector2(size, c.CustomMinimumSize.Y);
        c.Size = new Vector2(size, c.Size.Y);
    }

    public bool TryPickup(IPickupable item)
    {
        // we can only pick up flags
        if (item is not Flag f)
        {
            return false;
        }

        // now, see if we are touching it. We cannot pickup flags we are not touching
        if(!_touchingFlags.Contains(f))
        {
            return false;
        }

        // now, check the group. We can only pick up flags of the enemy team
        if(!f.IsInGroup("enemy_team"))
        {
            return false;
        }

        // this is a flag, we are touching it, and it's an enemy flag. try the pickup and return the result
        if(f.Pickup(this))
        {
            Carrying = f;
            EmitSignal(SignalName.OnPickup);
            return true;
        }
        return false;
    }

    public bool TryDrop(IPickupable item)
    {
        // we can only drop items we are carrying
        if(item.Carrier != this)
        {
            return false;
        }

        // we can drop basically anything, so just try it. don't need to check carrying because that's done on the item
        if(item.Drop())
        {
            Carrying = null;
            return true;
        }
        return false;
    }

    public void RegisterFlagTouched(Flag f)
    {
        _touchingFlags.Add(f);
    }

    public void RegisterFlagLeft(Flag f)
    {
        _touchingFlags.Remove(f);
    }

    public Callable ConnectToOnPickup(Action action)
    {
        Callable call = Callable.From(action);
        Connect(SignalName.OnPickup, call);
        return call;
    }

    public void DisconnectFromOnPickup(Callable call)
    {
        Disconnect(SignalName.OnPickup, call);
    }
}
