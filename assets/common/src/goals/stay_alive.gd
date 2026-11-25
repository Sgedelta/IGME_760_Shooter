extends GoapGoal

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# set desired state here
func _ready() -> void:
	desired_state = {StateKeys.IS_HEALTHY: true}
	pass # Replace with function body.

# returns if this goal is a valid action, aka when this activates
func is_valid() -> bool:
	if not _world_state:
		return false
	return !_world_state.get_state(StateKeys.IS_HEALTHY, false);
	# return _world_state.get_state(StateKeys.KEY, false)

# get prio, or how important this goal is
func get_priority() -> int:
	# if we're losing, don't care about staying healthy as much
	var scalar = 1
	if !_world_state.get_state(StateKeys.IS_WINNING, true):
		scalar = .5;
		
	if !_world_state.get_state(StateKeys.IS_HEALTHY, false):
		return priority * scalar;
	else:
		return 0;

# what happens when this goal is achieved - should likely lower the prio
func on_goal_achieved() -> void:
	pass
	# example had the desired state set to FALSE here - to recur goal, probably
	# _world_state.set_state(StateKeys.KEY, false)
