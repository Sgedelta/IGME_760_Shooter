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
	
	return GoapWorldState.is_satisfied(StateKeys.FLAG_AT_HOME, true);

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 1; #div 2 to discount, because returning flags is important
