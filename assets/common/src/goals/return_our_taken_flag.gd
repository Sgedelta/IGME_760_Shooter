extends GoapGoal

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# set desired state here
func _ready() -> void:
	desired_state = {StateKeys.FLAG_AT_HOME: true}

# returns if this goal is a valid action, aka when this activates
func is_valid() -> bool:
	if not _world_state:
		return false
	return (
		!_world_state.get_state(StateKeys.FLAG_AT_HOME, true)
		&& !_world_state.get_state(StateKeys.PLAYER_HAS_FLAG, true)
		&& !_world_state.get_state(StateKeys.HAS_FLAG, true)
	);
	# return _world_state.get_state(StateKeys.KEY, false)

# get prio, or how important this goal is
func get_priority() -> int:
	# if we know we cannot reach it, never do this..
	var d = bridge.get_path_dist_to_gloabl_point(bridge.get_nearest_enemy_flag_pos());
	
	print("distance is ", d);
	#numbers here are based on speed and flag return time. not good to hardcode but I don't want to make it work better rn
	if d >= ((200 * 10) + 50): 
		return 0
	
	# if not winning, prioritize this
	if !_world_state.get_state(StateKeys.IS_WINNING, false):
		return priority;
	return priority / 2.0;

# what happens when this goal is achieved - should likely lower the prio
func on_goal_achieved() -> void:
	pass
	# example had the desired state set to FALSE here - to recur goal, probably
	# _world_state.set_state(StateKeys.KEY, false)
