 /**
  * tgui state: notcontained_state
  *
  * Checks that the user is not inside src_object, and then makes the default checks.
 **/

GLOBAL_DATUM_INIT(notcontained_state, /datum/ui_state/notcontained_state, new)

/datum/ui_state/notcontained_state/can_use_topic(atom/src_object, mob/user)
	. = user.shared_ui_interaction(src_object)
	if(. > UI_CLOSE)
		return min(., user.notcontained_can_use_topic(src_object))

/mob/proc/notcontained_can_use_topic(src_object)
	return UI_CLOSE

/mob/living/notcontained_can_use_topic(atom/src_object)
	if(src_object.contains(src))
		return UI_CLOSE // Close if we're inside it.
	return default_can_use_topic(src_object)

/mob/living/silicon/notcontained_can_use_topic(src_object)
	return default_can_use_topic(src_object) // Silicons use default bevhavior.

/mob/living/simple_animal/drone/notcontained_can_use_topic(src_object)
	return default_can_use_topic(src_object) // Drones use default bevhavior.
