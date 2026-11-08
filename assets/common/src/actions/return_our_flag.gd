extends GoapAction


# Set effects and preconditions here
func _ready() -> void:
	effects = {StateKeys.FLAG_AT_HOME: true} 
	preconditions = {StateKeys.FLAG_AT_HOME: false}
	pass

# Called when goal is begun
func enter() -> void:
	
	pass;

# Return true once the action is done, return false otherwise
func perform(_delta) -> bool:
	
	return true;

# the cost of taking this step
func get_cost(_blackboard) -> int:
	return 2;
