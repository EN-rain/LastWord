@tool
extends BTCondition
class_name ListenerBTCondition

@export_enum(
	"Phase 3 Permanent Frenzy",
	"Active Scream Frenzy",
	"Vocal Sacrifice Lock",
	"Has Sprint Target",
	"Has Second Listener Imprint Target",
	"Has Token Target",
	"Has Sound Investigation",
	"Always"
) var condition: int = 7

func _generate_name() -> String:
	return "Listener Condition: %s" % _condition_name()

func _tick(_delta: float) -> Status:
	if not is_instance_valid(agent) or not agent.has_method("EvaluateBehaviorConditionValue"):
		return FAILURE

	return SUCCESS if agent.EvaluateBehaviorConditionValue(condition) else FAILURE

func _condition_name() -> String:
	match condition:
		0:
			return "Phase 3 Permanent Frenzy"
		1:
			return "Active Scream Frenzy"
		2:
			return "Vocal Sacrifice Lock"
		3:
			return "Has Sprint Target"
		4:
			return "Has Second Listener Imprint Target"
		5:
			return "Has Token Target"
		6:
			return "Has Sound Investigation"
		7:
			return "Always"
		_:
			return "Unknown"
