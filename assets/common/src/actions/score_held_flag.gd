extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {
		StateKeys.SCORED_FLAG: true,
		StateKeys.HAS_FLAG: false
	} 
	preconditions = {
		StateKeys.HAS_FLAG: true
	}
	pass

# Called when goal is begun
func enter() -> void:
	_actor.guide_to_home()
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	
	return _world_state.get_state(StateKeys.HAS_FLAG, false);

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return bridge.get_dist_to_home();
