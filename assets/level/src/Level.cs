using Godot;
using Godot.Collections;
using System;
using System.Linq;
using SCG = System.Collections.Generic;

public partial class Level : Node2D
{

	Dictionary<Team, int> Scores;

	[Export] public int GoalScore = 3;

	[Export] public Array<Flag> Flags = new();
	private Dictionary<Flag, Vector2> _flagInitialPositions;
	[Export] public Array<FlagReturn> FlagReturns = new();

    [Export] public Array<RespawnPoint> RespawnPoints = new Array<RespawnPoint>();

	private RandomNumberGenerator _rng;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_rng = new RandomNumberGenerator();
		Scores = new Dictionary<Team, int>();
		_flagInitialPositions = new Dictionary<Flag, Vector2>();
		foreach(Flag f in Flags)
		{
			_flagInitialPositions.Add(f, f.GlobalPosition);
		}

		foreach(FlagReturn fr in FlagReturns)
		{
			fr.OnFlagScored += OnFlagScored;
		}
		

		ResetGame();

	}


	public void ResetGame()
	{
		// reset scores
		Scores.Clear();
        Scores.Add(Team.PLAYER, 0);
        Scores.Add(Team.ENEMY, 0);

		// reset flags
		foreach(Flag f in Flags)
		{
			ResetFlag(f);
		}

		// reset players and enemies
		// TODO



    }

	public void OnFlagScored(int teamEnum, Flag scoredFlag)
	{
		if(teamEnum < 1 || teamEnum >= (int)Team.MAX)
		{
			GD.PushError($"teamEnum of value {teamEnum} is not a valid team when flag {scoredFlag.Name} was scored!");
			return;
		}

		// will be a non-NONE team
		Team scoredTeam = (Team)teamEnum;

		// make sure we know team 
		if(!Scores.ContainsKey(scoredTeam))
		{
			GD.PushError($"did not have tracked score for team {scoredTeam}");
		}

		// score
		Scores[scoredTeam] += 1;
		int newScore = Scores[scoredTeam];

		// determine win
		if(newScore >= GoalScore)
		{
			TeamWon(scoredTeam);
		}
		// otherwise reset
		else
		{
			ResetFlag(scoredFlag);
		}
		
	}

	public void TeamWon(Team t)
	{

	}

	public void ResetFlag(Flag f)
	{
		// drop
		if(f.BeingCarried && !f.Carrier.TryDrop(f))
		{
			GD.PushWarning("failed to have carrier drop flag! forcing drop, but carrier will believe it is carrying still.");
			f.Drop();
		}
		// set vel
        f.LinearVelocity = Vector2.Zero;
		// set pos
        f.GlobalPosition = _flagInitialPositions[f];
		ResetPhysicsInterpolation(); //no interpolation
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

        var pArray = new Godot.Collections.Array(validPoints.ToArray());

        return validPoints.ElementAt(_rng.RandiRange(0, validPoints.Count() - 1));
		//_rng.RandWeighted()
	}



}
         