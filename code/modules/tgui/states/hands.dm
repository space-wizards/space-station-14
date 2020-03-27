 /**
  * tgui state: hands_state
  *
  * Checks that the src_object is in the user's hands.
 **/

GLOBAL_DATUM_INIT(hands_state, /datum/ui_state/hands_state, new)

/datum/ui_state/hands_state/can_use_topic(src_object, mob/user)
	. = user.shared_ui_interaction(src_object)
	if(. > UI_CLOSE)
		return min(., user.hands_can_use_topic(src_object))

/mob/proc/hands_can_use_topic(src_object)
	return UI_CLOSE

/mob/living/hands_can_use_topic(src_object)
	if(is_holding(src_object))
		return UI_INTERACTIVE
	return UI_CLOSE

/mob/living/silicon/robot/hands_can_use_topic(src_object)
	if(activated(src_object))
		return UI_INTERACTIVE
	return UI_CLOSE
