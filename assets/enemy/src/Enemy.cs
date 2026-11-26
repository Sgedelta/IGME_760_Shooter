using Godot;
using System;
using Godot.Collections;
using SCG = System.Collections.Generic;
using System.Linq;

public partial class Enemy : CharacterBody2D, IDamagable, IMover, ICarrier
{
    Level level;

    [Signal] public delegate void OnPickupEventHandler();
    [Signal] public delegate void OnRequestCoverLocationEventHandler();
    [Signal] public delegate void OnRequestFlagLocationEventHandler(Team flagTeam);
    [Signal] public delegate void OnRequestFlagStandLocationEventHandler(Team flagStandTeam);
    [Signal] public delegate void RequestFlagLocationUpdateEventHandler();

    [Export] public float Speed { get; private set; } = 25.0f;

    public int Health { get; private set; } = 5;
    public float HealthPercent { get { return (float)Health / (float)_MaxHealth; } }
    private int _MaxHealth;

    [Export] public float RegenSpeed { get; private set; } = 7;

    [Export] public float TimeUntilRegen { get; private set; } = 15;

    [Export] private Vector2 _initialTargetPos = new Vector2();

    public Vector2 TargetGlobalPos
    {
        get { return _navAgent.TargetPosition; }
        set {
            if (_navAgent.TargetPosition != value)
            {
                GD.Print("Target Pos Updated");
                _navAgent.TargetPosition = value;
            }
        }
    }

    public bool CanPickup { get; private set; }

    public IPickupable Carrying { get; private set; }
    public bool IsCarrying { get { return Carrying != null; } }

    [Export] public float FlagThrowStr = 15;
    public Vector2 DropVel { get { return Velocity + _weapon.AimDir * FlagThrowStr; } }

    // xy are mouse offset, where x is in the direction of the mouse's location and y is the right vector of that. Z is an additional rotation to apply after, in degrees.
    [Export] public Vector3 CarryOffsetAndRot = new Vector3(0, 75, 0);
    public Vector2 CarryPos
    {
        get
        {
            // grab mouse loc
            Vector2 add = _weapon.AimDir;
            Vector2 right = add.Rotated(MathF.PI / 2);
            // put xy into those terms, with X being in line with mouse pos and y being right of it (90 cw). add the rotation z
            add = (add * CarryOffsetAndRot.X + right * CarryOffsetAndRot.Y).Rotated(CarryOffsetAndRot.Z);

            // add to pos
            return GlobalPosition + add;
        }
    }

    [Export] public bool AUTO_RELOAD = true;

    [Export(PropertyHint.Range, "0.0f, 180.0f, 0.1f, radians_as_degrees")] public float AimVariance;
    [Export(PropertyHint.Range, "0.0f, 360.0f, 0.1f, radians_as_degrees")] public float RotationSpeedPerSec = Mathf.DegToRad(90.0f);

    [Export(PropertyHint.Range, "0.0f, 90.0f, 0.1f, radians_as_degrees")] public float ConeVisionAngle = Mathf.DegToRad(15.0f);
    [Export] public float PassiveVisionRadius = 600.0f;
    private RayCast2D _visionCast;
    private Area2D _passiveSenseArea;
    private Area2D _visionArea;
    private const float VISION_CAST_OFFSET = 60f;

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();
    public float TimePaused = 10;
    private bool Respawning = false;

    private NavigationAgent2D _navAgent;

    private Weapon _weapon;

    private RandomNumberGenerator _rng;
    private Tween _aimTween;

    private SCG.HashSet<Flag> _touchingFlags;


    private Timer _damageTimer;
    private Timer _regenTimer;

    public Vector2 LastKnownPlayerPosition = new Vector2();

    public Node GoapBridge;

    private Timer _reaimTimer;
    private Timer _rehideTimer;
    private Timer _reseekTimer; 
    private bool _aimingToShoot;

    public Array<Player> LastSensedPlayers = new();

    public float DistanceToNearestPlayerFlag; // set from level
    public float DistanceToNearestEnemyFlag; // set from level
    public Vector2 NearestEnemyFlagGlobalPos; // set from level;

    public bool HasKilledPlayer = false;

    [Export] private Node2D DebugNextPathPosMarker;

    public override void _Ready()
    {
        Respawning = false;

        level = GetTree().Root.GetNode<Level>("Level");
        OnKilled += OnEnemyKilled;

        _navAgent = GetNode<NavigationAgent2D>("NavAgent");
        GoapBridge = GetNode<Node>("C# Bridge");

        _weapon = GetNode<Weapon>("Weapon");
        _weapon.WeaponTeam = Team.ENEMY;
        _weapon.AimDir = Vector2.Right;

        _passiveSenseArea  = GetNode<Area2D>("TruePassiveRadius");
        _visionArea  = GetNode<Area2D>("VisionRadius");

        _visionCast = GetNode<RayCast2D>("VisionCast");

        _MaxHealth = Health;
        _touchingFlags = new();

        _rng = new RandomNumberGenerator();

        _reaimTimer = new Timer();
        _reaimTimer.WaitTime = .2f;
        _reaimTimer.OneShot = true;
        AddChild(_reaimTimer);
        _rehideTimer = new Timer();
        _rehideTimer.WaitTime = .5f;
        _rehideTimer.OneShot = true;
        AddChild(_rehideTimer);
        _reseekTimer = new Timer();
        _reseekTimer.WaitTime = .15f;
        _reseekTimer.OneShot = true;
        AddChild(_reseekTimer);

        Callable.From(FirstFrameSetup).CallDeferred();

        // regen timer stuff
        _damageTimer = new Timer();
        _damageTimer.OneShot = true;
        _regenTimer = new Timer();
        _regenTimer.OneShot = false; // think this defaults to this but making sure
        AddChild(_damageTimer);
        AddChild(_regenTimer);
        _damageTimer.Timeout += () =>
        {
            _regenTimer.Start(TimeUntilRegen);
        };
        _regenTimer.Timeout += () =>
        {

            Health = Mathf.Clamp(Health + 1, 0, _MaxHealth);
        };

        TargetGlobalPos = GlobalPosition;
    }
    public void OnEnemyKilled()
    {
        GD.Print("Enemy Died");
        RespawnPoint respawn = level.GetRespawnPoint(Team.ENEMY);
        this.Position = respawn.Position;
        Respawning = true;
        TimePaused = 10;
        this.Modulate = new Color(1, 0, 0, 0.7f);

        Health = _MaxHealth;
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
                this.Modulate = new Color(1, 1, 1, 1f);
            }
            return;
        }
        //FireAtPosition(Vector2.Zero, false);
        
        var seenPlayers = GetSensedPlayers(_weapon.AimDir);
        LastSensedPlayers = seenPlayers;
        if(seenPlayers.Count() > 0)
        {
            LastKnownPlayerPosition = seenPlayers.First().GlobalPosition;
            FireAtPosition(LastKnownPlayerPosition, true);
            ((Node)((Node)GoapBridge.Get("agent")).Call("get_world_state")).Call("set_state", StateKeys.FOUND_PLAYER, true);
        }
        else
        {
            ((Node)((Node)GoapBridge.Get("agent")).Call("get_world_state")).Call("set_state", StateKeys.FOUND_PLAYER, false);
        }

        //if(Carrying != null)
        //{
        //    ((Node)((Node)GoapBridge.Get("agent")).Call("get_world_state")).Call("set_state", StateKeys.HAS_FLAG, true);
        //}

        if( (!IsInstanceValid(_aimTween) || (IsInstanceValid(_aimTween) && !_aimingToShoot)) && !_navAgent.IsNavigationFinished())
        {
            LookAtPosition(_navAgent.GetNextPathPosition(), 0.0f, false);
        }
    }

    public override void _PhysicsProcess(double delta)
	{
      
        // done here to ensure internal update
        Vector2 next = _navAgent.GetNextPathPosition();

        DebugNextPathPosMarker.GlobalPosition = next;

        // movement 
        if (!(_navAgent.IsNavigationFinished()) && !Respawning)
        {
            Vector2 direction = GlobalPosition.DirectionTo(next);

            MoveInDir(direction);
        }
	}

    public void SeekArbitraryGlobalPos(Vector2 pos)
    {
        TargetGlobalPos = pos;
    }

    public void SeekPlayerFlag()
    {
        if(!_reseekTimer.IsStopped())
        {
            return;
        }

        _reseekTimer.Start();
        EmitSignal(SignalName.OnRequestFlagLocation, (int)Team.PLAYER);
    }

    public void SeekOurFlag()
    {
        if (!_reseekTimer.IsStopped())
        {
            return;
        }

        _reseekTimer.Start();
        EmitSignal(SignalName.OnRequestFlagLocation, (int)Team.ENEMY);
    }

    public void SeekCover()
    {
        if(!_rehideTimer.IsStopped())
        {

            return;
        }

        EmitSignal(SignalName.OnRequestCoverLocation);
        _rehideTimer.Start();
    }

    public void SeekLastKnownPlayerPos()
    {
        if (!_reseekTimer.IsStopped())
        {
            return;
        }

        _reseekTimer.Start();
        SeekArbitraryGlobalPos(LastKnownPlayerPosition);
    }

    public void SeekPlayerFlagReturn()
    {
        if (!_reseekTimer.IsStopped())
        {
            return;
        }

        _reseekTimer.Start();
        EmitSignal(SignalName.OnRequestFlagStandLocation, (int)Team.PLAYER);

    }

    public void SeekOurFlagReturn()
    {
        if (!_reseekTimer.IsStopped())
        {
            return;
        }

        _reseekTimer.Start();
        EmitSignal(SignalName.OnRequestFlagStandLocation, (int)Team.ENEMY);

    }

    public int GetLastSeenPlayerCount()
    {
        return LastSensedPlayers.Count();
    }

    public bool CheckKilledPlayer()
    {
        if(!HasKilledPlayer)
        {
            return false;
        }
        HasKilledPlayer = false;
        return true;
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

            _regenTimer.Stop();
            _damageTimer.Start(TimeUntilRegen);

            EmitSignal(SignalName.OnDamageTaken, damage);

            if (Health <= 0)
            {
                if (Carrying != null)
                {
                    TryDrop(Carrying);
                }

                _aimingToShoot = false;

                EmitSignal(SignalName.OnKilled);
               
            }
        }


    }

    public bool LookAtPosition(Vector2 targetGlobalPos, float rotationChange, bool overrideCurrent)
    {
        if (!overrideCurrent && !_reaimTimer.IsStopped())
        {
            
            return false;
        }
        else
        {
            
        }

        // check if we can fire / if we need to override
        if (IsInstanceValid(_aimTween) && _aimTween.IsRunning())
        {
            if (overrideCurrent)
            {
                _aimTween.Kill();
                _aimingToShoot = false;
            }
            else
            {
                return false;
            }
        }

        // localize position
        Vector2 dir = targetGlobalPos - GlobalPosition;
        dir = dir.Normalized();

        // rotate dir
        dir = dir.Rotated(rotationChange);

        // do the thing.
        _aimTween = CreateTween();

        // aim
        float time = Mathf.Abs(_weapon.AimDir.AngleTo(dir)) / RotationSpeedPerSec;
        _aimTween.TweenProperty(_weapon, "AimDir", dir, time);
        _reaimTimer.Start();
        return true;
    }

    public void FireAtPosition(Vector2 targetGlobalPos, bool overrideCurrent)
    {
        // check if this makes sense to even do, because reload
        if (AUTO_RELOAD && _weapon.LoadedBullets <= 0 && !_weapon.Reloading)
        {
            _weapon.Reload();
            return;
        }
        
        if (!LookAtPosition(targetGlobalPos, _rng.RandfRange(-AimVariance, AimVariance), overrideCurrent))
        {
            return;
        }

        // fire
        _aimingToShoot = true;
        _aimTween.Finished += _weapon.TryFire;
        _aimTween.Finished += () => { _aimingToShoot = false; };
    }

    public bool TryPickup(IPickupable item)
    {
        // we can only pick up flags
        if (item is not Flag f)
        {
            return false;
        }

        // now, see if we are touching it. We cannot pickup flags we are not touching
        // if (!_touchingFlags.Contains(f))
        // {
        //     return false;
        // }

        // now, check the group. We can only pick up flags of the enemy team
        if (!f.IsInGroup("player_team"))
        {
            return false;
        }

        // this is a flag, we are touching it, and it's an enemy flag. try the pickup and return the result
        if (f.Pickup(this))
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
        if (item.Carrier != this)
        {
            return false;
        }

        // we can drop basically anything, so just try it. don't need to check carrying because that's done on the item
        if (item.Drop())
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


    public Array<Player> GetSensedPlayers(Vector2 lookDirNormalized)
    {
        // get passive characters
        Array<Player> ret = new();

        // passive sense does not need a cast, get all the players in that area
        ret.AddRange(_passiveSenseArea.GetOverlappingBodies().Where(obj => obj is Player && !ret.Contains(obj)).Cast<Player>());

        // circular vision needs a cast, and if it's too long it needs a check of lookDir
        var overlaps = _visionArea.GetOverlappingBodies().Where(obj => obj is Player && !ret.Contains(obj));

        foreach(Player p in overlaps)
        {
            for(int i = 0; i < 4; i++)
            {
                Vector2 offset = new Vector2(VISION_CAST_OFFSET, 0).Rotated(i * Mathf.Pi / 2);
                // position in local coords + an offset
                Vector2 target = p.GlobalPosition - Position + offset;

                // if it's too long, we need to check the angle - we can do this even before we raycast
                if(target.Length() > PassiveVisionRadius && Mathf.Abs(target.AngleTo(lookDirNormalized)) > ConeVisionAngle)
                {
                    continue;
                }

                // run cast
                _visionCast.TargetPosition = target;
                _visionCast.ForceRaycastUpdate();

                var collider = _visionCast.GetCollider();
                // we already know p is a player that is not in the radius
                if (collider == p)
                {
                    ret.Add(p);
                }
            }
            
            
        }


        return ret;
    }

    public void OnPickupAreaEntered(Node2D body)
    {
        if (body is Flag f && f.team == Team.PLAYER)
        {
            TryPickup(f);
        }
    }


    public void ForceAgentRecalc()
    {
        _navAgent.TargetPosition = _navAgent.TargetPosition;
        _navAgent.GetNextPathPosition();
    }


    public float GetPathDistToGlobalPoint(Vector2 globalPoint)
    {
        var path = NavigationServer2D.MapGetPath(GetWorld2D().NavigationMap, GlobalPosition, globalPoint, true);
        float dist = 0;
        for (int i = 1; i < path.Count()-1; i++)
        {
            dist += path[i - 1].DistanceTo(path[i]);
        }

        return dist;
    }

    public void UpdateFlagLocations()
    {
        EmitSignal(SignalName.RequestFlagLocationUpdate);
    }
}
