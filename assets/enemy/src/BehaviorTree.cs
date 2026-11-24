using Godot;
using System;
using System.Diagnostics;

public partial class BehaviorTree : Node
{
    public bool isBehaving;
    [Export] Player player;
    [Export] Enemy enemy;

    [Export] float hoverDistance = 3f;

    float sawPlayerSecondsAgo = -1;
    float timeToPursue = 15; // how long to pursue player after enemy saw player last
    float timeToHide = 15; // how long to seek cover since enemy saw player last
    State currentState;

    enum State
    {
        Aggressive,
        Defensive,
        HasFlag,
    }

    public override void _Process(double delta)
    {
        if (!enemy.canSeePlayer && sawPlayerSecondsAgo >= 0)
        {
            sawPlayerSecondsAgo += (float)delta;
        }
        if (enemy.IsCarrying)
        {
            currentState = State.HasFlag;
        }

        switch (currentState)
        {
            case State.Aggressive:

                // Pursue player if they have enemy flag or are in the area, otherwise pursue player flag
                if (enemy.canSeePlayer)
                {
                    enemy.MoveInDir((player.GlobalPosition - enemy.GlobalPosition).Normalized() * hoverDistance);
                    sawPlayerSecondsAgo = 0;
                }
                else if (/*Player has enemy flag*/)
                {
                    enemy.SeekReturnedFlag();
                }
                else if (sawPlayerSecondsAgo < timeToPursue && sawPlayerSecondsAgo >= 0)
                {
                    enemy.SeekLastKnownPlayerPos();
                }
                else
                {
                    enemy.SeekPlayerFlag();
                }

                // Check if state needs to be switched
                if (enemy.Health < 2)
                    currentState = State.Defensive;

                break;
            case State.Defensive:

                // Either look for cover or pursue the player's flag
                if (enemy.canSeePlayer || sawPlayerSecondsAgo <= timeToHide)
                {
                    //enemy.SeekCoverLocation();
                }
                else
                {
                    enemy.SeekPlayerFlag();
                }

                // Check if state needs to be switched
                if (enemy.Health > 1)
                    currentState = State.Aggressive;

                break;
            case State.HasFlag:

                // Return the flag to base
                //enemy.SeekOurFlagReturn();

                // Check if state needs to be switched
                if (!enemy.IsCarrying)
                {
                    if (enemy.Health < 2)
                        currentState = State.Defensive;
                    else
                        currentState = State.Aggressive;
                }

                break;
            default:
                GD.Print("How did you reach this. You are void of state");
                break;
        }
    }
}