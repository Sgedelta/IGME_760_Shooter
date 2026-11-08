using Godot;
using System;

public enum Team
{
    NONE,
    PLAYER,
    ENEMY,

    MAX
}

public class StateKeys
{
    public const string FOUND_PLAYER = "found_player";
    public const string HAS_FLAG = "has_flag";
    public const string FLAG_AVAIL = "flag_avail";
    public const string PLAYER_HAS_FLAG = "player_has_flag";
    public const string IS_FIGHTING = "is_fighting";
    public const string SCORED_FLAG = "scored_flag";
    public const string FLAG_AT_HOME = "flag_at_home";
}
