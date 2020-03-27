/datum/component/empprotection
	var/flags = NONE

/datum/component/empprotection/Initialize(_flags)
	if(!istype(parent, /atom))
		return COMPONENT_INCOMPATIBLE
	flags = _flags
	RegisterSignal(parent, list(COMSIG_ATOM_EMP_ACT), .proc/getEmpFlags)

/datum/component/empprotection/proc/getEmpFlags(datum/source, severity)
	return flags
