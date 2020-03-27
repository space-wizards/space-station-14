 /**
  * tgui state: physical_state
  *
  * Short-circuits the default state to only check physical distance.
 **/

GLOBAL_DATUM_INIT(physical_state, /datum/ui_state/physical, new)

/datum/ui_state/physical/can_use_topic(src_object, mob/user)
	. = user.shared_ui_interaction(src_object)
	if(. > UI_CLOSE)
		return min(., user.physical_can_use_topic(src_object))

/mob/proc/physical_can_use_topic(src_object)
	return UI_CLOSE

/mob/living/physical_can_use_topic(src_object)
	return shared_living_ui_distance(src_object)

/mob/living/silicon/physical_can_use_topic(src_object)
	return max(UI_UPDATE, shared_living_ui_distance(src_object)) // Silicons can always see.

/mob/living/silicon/ai/physical_can_use_topic(src_object)
	return UI_UPDATE // AIs are not physical.


 /**
  * tgui state: physical_obscured_state
  *
  * Short-circuits the default state to only check physical distance, being in view doesn't matter
 **/

GLOBAL_DATUM_INIT(physical_obscured_state, /datum/ui_state/physical_obscured_state, new)

/datum/ui_state/physical_obscured_state/can_use_topic(src_object, mob/user)
	. = user.shared_ui_interaction(src_object)
	if(. > UI_CLOSE)
		return min(., user.physical_obscured_can_use_topic(src_object))

/mob/proc/physical_obscured_can_use_topic(src_object)
	return UI_CLOSE

/mob/living/physical_obscured_can_use_topic(src_object)
	return shared_living_ui_distance(src_object, viewcheck = FALSE)

/mob/living/silicon/physical_obscured_can_use_topic(src_object)
	return max(UI_UPDATE, shared_living_ui_distance(src_object, viewcheck = FALSE)) // Silicons can always see.

/mob/living/silicon/ai/physical_obscured_can_use_topic(src_object)
	return UI_UPDATE // AIs are not physical.
