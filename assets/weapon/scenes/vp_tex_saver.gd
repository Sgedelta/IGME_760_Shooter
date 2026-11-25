extends SubViewport

@export var filename : String;
@export var sub_viewport : SubViewport;
var saved = false;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if not saved:
		save()
		saved = true
	pass


func save():
	await RenderingServer.frame_post_draw
	var img: Image = sub_viewport.get_texture().get_image()
	img.save_png("res://" + filename + ".png")
	print("SAVED")
