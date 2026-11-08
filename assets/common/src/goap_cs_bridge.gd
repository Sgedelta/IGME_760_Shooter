class_name GoapBridge
extends Node

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

func get_dist_to_flag() -> float:
	# TODO: use enemy sensing to try and find flag
	
	return 5;

func maintain_distance_from_player():
	# TODO: find player location and move to a point near them
	pass

func guide_to_home():
	
	pass
