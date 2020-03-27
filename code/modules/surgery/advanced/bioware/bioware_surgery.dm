/datum/surgery/advanced/bioware
	name = "Enhancement surgery"
	var/bioware_target = BIOWARE_GENERIC

/datum/surgery/advanced/bioware/can_start(mob/user, mob/living/carbon/human/target)
	if(!..())
		return FALSE
	if(!istype(target))
		return FALSE
	for(var/X in target.bioware)
		var/datum/bioware/B = X
		if(B.mod_type == bioware_target)
			return FALSE
	return TRUE
