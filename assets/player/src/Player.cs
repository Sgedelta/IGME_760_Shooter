using Godot;
using System;

public partial class Player : CharacterBody2D, IDamagable, IMover
{
    [Export] public float Speed { get; private set; } = 25.0f;

    [Export] public int Health { get; private set; } = 3;

    [Export] public Vector4 ReloadOffsets = new Vector4(-.5f, .5f, 0, 0);

    [Signal]
    public delegate void OnDamageTakenEventHandler(int damageTaken);

    [Signal]
    public delegate void OnKilledEventHandler();

    private Weapon _weapon;

    private TextureProgressBar _bulletsLeft;
    private ProgressBar _reloadProgress;

    private Sprite2D _visual;

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
}
