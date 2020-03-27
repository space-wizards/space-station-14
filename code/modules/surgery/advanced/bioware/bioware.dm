//Bioware
//Body modifications applied through surgery. They generally affect physiology.

/datum/bioware
	var/name = "Generic Bioware"
	var/mob/living/carbon/human/owner
	var/desc = "If you see this something's wrong, warn a coder."
	var/active = FALSE
	var/mod_type = BIOWARE_GENERIC

/datum/bioware/New(mob/living/carbon/human/_owner)
	owner = _owner
	for(var/X in owner.bioware)
		var/datum/bioware/B = X
		if(B.mod_type == mod_type)
			qdel(src)
			return
	owner.bioware += src
	on_gain()
	
/datum/bioware/Destroy()
	owner = null
	if(active)
		on_lose()
	return ..()

/datum/bioware/proc/on_gain()
	active = TRUE

/datum/bioware/proc/on_lose()
	return
