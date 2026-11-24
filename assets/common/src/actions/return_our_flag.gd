extends GoapAction

#never do this
@onready var bridge = get_node("../../") as GoapBridge

# Set effects and preconditions here
func _ready() -> void:
	effects = {StateKeys.FLAG_AT_HOME: true} 
	preconditions = {StateKeys.FLAG_AT_HOME: false}
	pass

# Called when goal is begun
func enter() -> void:
	bridge.go_to_our_flag()
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	bridge.go_to_our_flag()
	return bridge.get_if_flag_home();

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return bridge.get_dist_to_enemy_flag() / 2.0; #div 2 to discount, because returning flags is important
