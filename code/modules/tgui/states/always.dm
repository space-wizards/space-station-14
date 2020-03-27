
 /**
  * tgui state: always_state
  *
  * Always grants the user UI_INTERACTIVE. Period.
 **/

GLOBAL_DATUM_INIT(always_state, /datum/ui_state/always_state, new)

/datum/ui_state/always_state/can_use_topic(src_object, mob/user)
	return UI_INTERACTIVE
