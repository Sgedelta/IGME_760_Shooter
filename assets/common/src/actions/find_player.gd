extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {
		StateKeys.FOUND_PLAYER: true
	} 
	preconditions = {StateKeys.FOUND_PLAYER: false}
	pass

# Called when goal is begun
func enter() -> void:
	bridge.maintain_distance_from_player(10.0)
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.maintain_distance_from_player(10.0)
	return bridge.get_seen_player_count() > 0;

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return bridge.get_dist_to_last_player_pos();
