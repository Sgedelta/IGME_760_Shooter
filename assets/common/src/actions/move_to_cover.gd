extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {StateKeys.FOUND_PLAYER: false} 
	preconditions = {StateKeys.FOUND_PLAYER: true}
	pass

# Called when goal is begun
func enter() -> void:
	bridge.seek_cover()
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.seek_cover()
	return true;

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 2;
