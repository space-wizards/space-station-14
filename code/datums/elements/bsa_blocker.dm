//blocks bluespace artillery beams that try to fly through
//look not all elements need to be fancy
/datum/element/bsa_blocker/Attach(datum/target)
	if(!isatom(target))
		return ELEMENT_INCOMPATIBLE
	RegisterSignal(target, COMSIG_ATOM_BSA_BEAM, .proc/block_bsa)
	return ..()

/datum/element/bsa_blocker/proc/block_bsa()
	return COMSIG_ATOM_BLOCKS_BSA_BEAM
