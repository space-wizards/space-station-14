// This is just a bit of fun while making an example for global signal
/datum/component/edit_complainer
	var/list/say_lines

/datum/component/edit_complainer/Initialize(list/text)
	if(!ismovableatom(parent))
		return COMPONENT_INCOMPATIBLE

	var/static/list/default_lines = list(
		"CentCom's profligacy frays another thread.",
		"Another tug at the weave.",
		"Who knows when the stresses will finally shatter the form?",
		"Even now a light shines through the cracks.",
		"CentCom once more twists knowledge beyond its authority.",
		"There is an uncertain air in the mansus.",
		)
	say_lines = text || default_lines

	RegisterSignal(SSdcs, COMSIG_GLOB_VAR_EDIT, .proc/var_edit_react)

/datum/component/edit_complainer/proc/var_edit_react(datum/source, list/arguments)
	var/atom/movable/master = parent
	master.say(pick(say_lines))
