extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {StateKeys.PLAYER_HAS_FLAG: false} 
	preconditions = {StateKeys.FOUND_PLAYER: true}
	pass

# Called when goal is begun
func enter() -> void:
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.maintain_distance_from_player(10.0)
	return bridge.check_killed_player();

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 2;
