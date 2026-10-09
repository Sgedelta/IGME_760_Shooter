using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class Level : Node2D
{

	[Export] public Array<RespawnPoint> RespawnPoints = new Array<RespawnPoint>();

	private RandomNumberGenerator _rng;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_rng = new RandomNumberGenerator();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	/// <summary>
	/// returns a random respawn point
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public RespawnPoint GetRespawnPoint(Team t)
	{
		string teamString = t switch { Team.PLAYER => "player_team", Team.ENEMY => "enemy_team", _ => throw new ArgumentException("team is not valid") };
		var validPoints = RespawnPoints.Where((rp) => rp.IsInGroup(teamString));

		if(validPoints.Count() == 0)
		{
			throw new ArgumentException("no valid respawn points in group " + teamString);
		}

		return validPoints.ElementAt(_rng.RandiRange(0, validPoints.Count() - 1));
	}
}
