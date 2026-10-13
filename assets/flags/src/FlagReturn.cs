using Godot;
using System;
using System.Collections.Generic;

public partial class FlagReturn : Area2D
{
	[Signal] public delegate void OnFlagScoredEventHandler(int scoringTeamEnum, Flag scoredFlag);

	Dictionary<ICarrier, Callable> _touchingCarriers;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _touchingCarriers = new Dictionary<ICarrier, Callable>();

    }


	public void CheckFlagScore()
	{
		foreach (ICarrier c in _touchingCarriers.Keys)
		{
			if(c.Carrying is Flag f)
			{
				GD.Print(f.Name);
				if(IsInGroup("enemy_team") &&  f.IsInGroup("player_team"))
				{
					EmitSignal(SignalName.OnFlagScored, (int)Team.ENEMY, f);
				}
				if(IsInGroup("player_team") && f.IsInGroup("enemy_team"))
				{
					EmitSignal(SignalName.OnFlagScored, (int)Team.PLAYER, f);
					GD.Print("player scored");
				}
			}
		}
	}

	public void OnBodyEntered(Node2D body)
	{
		
		if(body is ICarrier carrier)
		{
            GD.Print(body.Name, " is a carrier");
            _touchingCarriers.Add(carrier, carrier.ConnectToOnPickup(CheckFlagScore));
		}
		CheckFlagScore();
	}

	public void OnBodyExited(Node2D body)
	{
        
        if (body is ICarrier carrier)
        {
			if(_touchingCarriers.ContainsKey(carrier))
            {
                Callable call = _touchingCarriers[carrier];
                _touchingCarriers.Remove(carrier);
                carrier.DisconnectFromOnPickup(call);
            }
        }
    }
}
