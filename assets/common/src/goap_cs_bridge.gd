class_name GoapBridge
extends Node2D

@export var active = true;
@export var enemy_number = 1;

@onready var actor = get_node("..")
@onready var agent = $GoapAgent

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	agent.init(self);
	var ws  = agent.get_world_state()
	if ws:
		ws.set_state(StateKeys.SCORED_FLAG, false)
		ws.set_state(StateKeys.HAS_FLAG, false)
		ws.set_state(StateKeys.FLAG_AT_HOME, true)
		ws.set_state(StateKeys.FLAG_AVAIL, true)
		ws.set_state(StateKeys.PLAYER_HAS_FLAG, false)
	

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if active:
		agent.process(delta)


func has_held_flag() -> bool:
	return actor.get("IsCarrying")

func get_dist_to_player_flag() -> float:
	
	return actor.get("DistanceToNearestPlayerFlag");

func get_dist_to_enemy_flag() -> float:
	
	return actor.get("DistanceToNearestEnemyFlag");

func maintain_distance_from_player(dist : float ):
	
	if !active:
		return;
	
	if (actor.get("LastKnownPlayerPosition") as Vector2).distance_to(global_position) < dist:
		return;
	actor.call("SeekLastKnownPlayerPos")
	
	pass

func go_to_our_flag():
	
	if !active:
		return;
	
	actor.call("SeekOurFlag")
	pass

func go_to_player_flag():
	if !active:
		return;
	
	actor.call("SeekPlayerFlag")
	pass

func guide_to_home():
	if !active:
		return;
	
	actor.call("SeekOurFlagReturn")
	pass

func seek_cover():
	if !active:
		return;
	
	actor.call("SeekCover")

func check_killed_player() -> bool:
	if !active:
		return false;
	return actor.call("CheckKilledPlayer");

func get_seen_player_count() -> int:
	if !active:
		return 0;
	return actor.call("GetLastSeenPlayerCount")

func get_dist_to_last_player_pos() -> float:
	return global_position.distance_to(actor.LastKnownPlayerPosition)

func get_dist_to_home() -> float:
	return 20;

func get_if_flag_home() -> bool:
	return agent.get_world_state().get_state(StateKeys.FLAG_AT_HOME, true);

func get_agent_health() -> int:
	return actor.Health;

func get_agent_health_percent() -> float:
	return actor.HealthPercent;
