@tool
extends BTAction
class_name ListenerBTSetTargetMode

@export_enum(
	"None",
	"Token",
	"Sound Investigate",
	"Non-Frenzy Sprint",
	"Scream Frenzy",
	"Vocal Sacrifice",
	"Phase 3 Permanent Frenzy",
	"Second Listener Imprint"
) var target_mode: int = 0

func _generate_name() -> String:
	return "Set Listener Mode: %s" % _mode_name()

func _tick(_delta: float) -> Status:
	if not is_instance_valid(agent) or not agent.has_method("SetBehaviorTargetModeValue"):
		return FAILURE

	agent.SetBehaviorTargetModeValue(target_mode)
	return SUCCESS

func _mode_name() -> String:
	match target_mode:
		0:
			return "None"
		1:
			return "Token"
		2:
			return "Sound Investigate"
		3:
			return "Non-Frenzy Sprint"
		4:
			return "Scream Frenzy"
		5:
			return "Vocal Sacrifice"
		6:
			return "Phase 3 Permanent Frenzy"
		7:
			return "Second Listener Imprint"
		_:
			return "Unknown"
