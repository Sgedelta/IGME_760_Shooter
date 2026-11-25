extends GoapGoal


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
	return priority;

# what happens when this goal is achieved - should likely lower the prio
func on_goal_achieved() -> void:
	pass
	# example had the desired state set to FALSE here - to recur goal, probably
	# _world_state.set_state(StateKeys.KEY, false)
