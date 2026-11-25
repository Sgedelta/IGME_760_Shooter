extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {StateKeys.IS_HEALTHY: true} 
	preconditions = {StateKeys.FOUND_PLAYER: false, StateKeys.IS_HEALTHY: false}
	pass

# Called when goal is begun
func enter() -> void:
	bridge.seek_cover()
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.seek_cover()
	return bridge.get_agent_health_percent() > .666;

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 5;
