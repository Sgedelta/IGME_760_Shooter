extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {
		StateKeys.HAS_FLAG: true
	} 
	preconditions = {
		StateKeys.FLAG_AVAIL: true, 
		StateKeys.HAS_FLAG: false
	}
	pass

# Called when goal is begun
func enter() -> void:
	bridge.go_to_player_flag();
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.go_to_player_flag();
	return bridge.has_held_flag();

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 2;
