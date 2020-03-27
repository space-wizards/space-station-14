 /**
  * tgui state: conscious_state
  *
  * Only checks if the user is conscious.
 **/

GLOBAL_DATUM_INIT(conscious_state, /datum/ui_state/conscious_state, new)

/datum/ui_state/conscious_state/can_use_topic(src_object, mob/user)
	if(user.stat == CONSCIOUS)
		return UI_INTERACTIVE
	return UI_CLOSE
