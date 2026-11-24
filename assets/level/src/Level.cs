using Godot;
using Godot.Collections;
using System;
using System.Linq;
using SCG = System.Collections.Generic;

public partial class Level : Node2D
{

	Dictionary<Team, int> Scores;

	[Export] Dictionary<Team, Color> TeamColors = new Dictionary<Team, Color>();

	[Export] public int GoalScore = 3;

	[Export] public Array<Flag> Flags = new();
	private Dictionary<Flag, Vector2> _flagInitialPositions;
	[Export] public Array<FlagReturn> FlagReturns = new();

    [Export] public Array<RespawnPoint> RespawnPoints = new Array<RespawnPoint>();

	[Export] public Curve NegativeSpawnChanceByDistance = new Curve();

	private RandomNumberGenerator _rng;

	private Array<Enemy> _enemies;
	private Array<Player> _players;

	private TileMapLayer _obstacles;
	private Array<Vector2I> _coverIndexes;
	private Array<Vector2> _coverPositions;
	private Array<Vector2> _coverOffsets = new Array<Vector2>()
	{
		new(.1f, .1f), new(.9f, .1f),
		new(.9f, .1f), new(.9f, .1f)
	};

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_rng = new RandomNumberGenerator();
		Scores = new Dictionary<Team, int>();
		_flagInitialPositions = new Dictionary<Flag, Vector2>();

		_obstacles = GetNode<TileMapLayer>("%Obstacles");
		_coverIndexes = new();
		var minMax = new Vector4(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
		var used_cells = _obstacles.GetUsedCells();

		foreach (var cell in used_cells)
		{
			if (cell.X < minMax.X)
			{
				minMax.X = cell.X;
			}
			if (cell.X > minMax.Z)
			{
				minMax.Z = cell.X;
			}
			if (cell.Y < minMax.Y)
			{
				minMax.Y = cell.Y;
			}
			if (cell.Y > minMax.W)
			{
				minMax.W = cell.Y;
			}
		}

		foreach (var cell in used_cells) 
		{
			for (var x = cell.X-1; x <= cell.X+1; x += 2)
			{
				for (var y = cell.Y-1; y <= cell.Y+1; y += 2)
				{
					// ignore points outside of the bounds
					if(!(x >= minMax.X && x <= minMax.Z && y >= minMax.Y && y <= minMax.W))
					{
						continue;
					}

					Vector2I index = new Vector2I(x, y);

                    // ignore walls
					if (used_cells.Contains(index))
					{
						continue;
					}

                    // do not double store
                    if (!_coverIndexes.Contains(index))
					{
						_coverIndexes.Add(index);
					}
				}
			}
		}

		//get the actual positions 
		_coverPositions = new();
        Vector2 tileSize = _obstacles.TileSet.TileSize;
		foreach(var i in _coverIndexes)
		{
			var top_left = tileSize * i;
            foreach (var offset in _coverOffsets)
			{
				var pos = top_left + offset * tileSize;
				_coverPositions.Add(pos);
			}
		}


		foreach(Flag f in Flags)
		{
			_flagInitialPositions.Add(f, f.GlobalPosition);
			f.OnFlagReturn += () => ResetFlag(f);
			f.Modulate = TeamColors[f.team];
			f.OnFlagPickedUp += UpdateEnemyInfo;
			f.OnFlagDropped += UpdateEnemyInfo;

        }

		foreach(FlagReturn fr in FlagReturns)
		{
			fr.OnFlagScored += OnFlagScored;
		}

		Player[] playersInScene = GetTree().GetNodesInGroup("player_team").Where((node) => (node is Player)).OfType<Player>().ToArray();
        Enemy[] enemiesInScene = GetTree().GetNodesInGroup("enemy_team").Where((node) => (node is Enemy)).OfType<Enemy>().ToArray();

        foreach (Player p in playersInScene)
		{
			p.OnKilled += () => { 
				OnRespawnRequested(p);
				foreach(Enemy e in enemiesInScene)
				{
					e.HasKilledPlayer = true;
				}
                UpdateEnemyInfo();
            };
		}

		foreach (Enemy e in enemiesInScene)
		{
			e.OnKilled += () =>
			{
				UpdateEnemyInfo();
			};

			e.OnRequestFlagLocation += (team) =>
			{
				e.SeekArbitraryGolablPos(
					Flags.Where(f => f.team == team)
					.OrderBy(
						f => f.GlobalPosition.DistanceSquaredTo(e.GlobalPosition)
					).First().GlobalPosition
				);
			};

			e.OnRequestCoverLocation += () =>
			{

			};

			e.OnDamageTaken += (_) =>
			{
				e.LookAtPosition(_players.OrderBy(p => p.GlobalPosition.DistanceSquaredTo(e.GlobalPosition)).First().GlobalPosition, 0, false);
			};
		}

		_enemies = new Array<Enemy>(enemiesInScene);
        _players = new Array<Player>(playersInScene);

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
		//TODO
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
		f.InternalReset();
		ResetPhysicsInterpolation(); //no interpolation
		UpdateEnemyInfo(); // update the enemy
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

        var pArray = new Godot.Collections.Array<RespawnPoint>(validPoints.ToArray());

		// grab all other team
        Node2D[] oppositeTeamPlayers = (GetTree().GetNodesInGroup(teamString)).Where((obj) => obj is Player or Enemy).OfType<Node2D>().ToArray();
		float[] posWeights = new float[pArray.Count];

		// get weights
		for(int i = 0; i < pArray.Count; i++)
		{
			posWeights[i] = SpawnChanceFromPoints(pArray[i].GlobalPosition, oppositeTeamPlayers);
		}

		int index = (int)_rng.RandWeighted(posWeights);

        return validPoints.ElementAt(index);
		
	}

	public float SpawnChanceFromPoints(Vector2 loc, Node2D[] discouragePoints)
	{
		float spawnChance = 1;
		foreach(Vector2 pos in discouragePoints.Select((p) => p.GlobalPosition))
		{
			float dist = pos.DistanceTo(loc);
			float effect = NegativeSpawnChanceByDistance.Sample(dist);
            spawnChance -= effect;
		}
		return Mathf.Max(0, spawnChance);
	}

	public void OnRespawnRequested(Node2D caller)
	{
		// set caller invisible

		// wait

		// get spawn position

		// set caller to spawn position

		// make caller visible

		// tell caller it's respawned
	}


	// catch all function for setting state vars of enemies
	public void UpdateEnemyInfo()
	{

		foreach (Enemy e in _enemies)
		{
			e.DistanceToNearestPlayerFlag = Flags.Where(f => f.team == Team.PLAYER).OrderBy(f => f.GlobalPosition.DistanceSquaredTo(e.GlobalPosition)).FirstOrDefault().GlobalPosition.DistanceTo(e.GlobalPosition);
            e.DistanceToNearestEnemyFlag = Flags.Where(f => f.team == Team.ENEMY).OrderBy(f => f.GlobalPosition.DistanceSquaredTo(e.GlobalPosition)).FirstOrDefault().GlobalPosition.DistanceTo(e.GlobalPosition);


            var agent = (Node)e.GoapBridge.Get("agent");
			var ws = (Node)agent.Call("get_world_state");

			// are any of the flags we want available
			bool anyFlagAvail = !Flags.Where(f => f.team == Team.PLAYER).All(f => f.BeingCarried);
			GD.Print($"Enemy {e.Name} sees any flag avail: {anyFlagAvail}");

            ws.Call("set_state", StateKeys.FLAG_AVAIL, anyFlagAvail);

			// are we carrying a flag?
			bool hasAFlag = Flags.Where(f => f.team == Team.PLAYER).Any(f => f.Carrier == e);
            GD.Print($"Enemy {e.Name} sees having a flag: {hasAFlag}");
            ws.Call("set_state", StateKeys.HAS_FLAG, hasAFlag);

            // are all of our flags at home
            ws.Call("set_state", StateKeys.FLAG_AT_HOME, Flags.Where(f => f.team == Team.ENEMY).All(f => f.OnStand));

			// are any of our flags being carried
			bool playerHasFlag = Flags.Where(f => f.team == Team.ENEMY).Any(f => f.BeingCarried);
            GD.Print($"Enemy {e.Name} sees player(s) having flag: {playerHasFlag}");
            ws.Call("set_state", StateKeys.PLAYER_HAS_FLAG, playerHasFlag);

			// can we see a player right now
			ws.Call("set_state", StateKeys.FOUND_PLAYER, e.LastSensedPlayers.Count > 0);



        }
	}


}
         