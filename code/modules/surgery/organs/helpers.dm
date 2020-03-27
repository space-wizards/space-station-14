/**
  * Get the organ object from the mob matching the passed in typepath
  *
  * Arguments:
  * * typepath The typepath of the organ to get
  */
/mob/proc/getorgan(typepath)
	return
/**
  * Get organ objects by zone
  *
  * This will return a list of all the organs that are relevant to the zone that is passedin
  *
  * Arguments:
  * * zone [a BODY_ZONE_X define](https://github.com/tgstation/tgstation/blob/master/code/__DEFINES/combat.dm#L187-L200)
  */
/mob/proc/getorganszone(zone)
	return
/**
  * Get an organ relating to a specific slot
  *
  * Arguments:
  * * slot Slot to get the organ from
  */
/mob/proc/getorganslot(slot)
	return

/mob/living/carbon/getorgan(typepath)
	return (locate(typepath) in internal_organs)

/mob/living/carbon/getorganszone(zone, subzones = 0)
	var/list/returnorg = list()
	if(subzones)
		// Include subzones - groin for chest, eyes and mouth for head
		if(zone == BODY_ZONE_HEAD)
			returnorg = getorganszone(BODY_ZONE_PRECISE_EYES) + getorganszone(BODY_ZONE_PRECISE_MOUTH)
		if(zone == BODY_ZONE_CHEST)
			returnorg = getorganszone(BODY_ZONE_PRECISE_GROIN)

	for(var/X in internal_organs)
		var/obj/item/organ/O = X
		if(zone == O.zone)
			returnorg += O
	return returnorg

/mob/living/carbon/getorganslot(slot)
	return internal_organs_slot[slot]
