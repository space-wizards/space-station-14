 /**
  * tgui states
  *
  * Base state and helpers for states. Just does some sanity checks, implement a state for in-depth checks.
 **/

 /**
  * public
  *
  * Checks the UI state for a mob.
  *
  * required user mob The mob who opened/is using the UI.
  * required state datum/ui_state The state to check.
  *
  * return UI_state The state of the UI.
 **/
/datum/proc/ui_status(mob/user, datum/ui_state/state)
	var/src_object = ui_host(user)
	. = UI_CLOSE
	if(!state)
		return

	if(isobserver(user))
		// If they turn on ghost AI control, admins can always interact.
		if(IsAdminGhost(user))
			. = max(., UI_INTERACTIVE)

		// Regular ghosts can always at least view if in range.
		var/clientviewlist = getviewsize(user.client.view)
		if(get_dist(src_object, user) < max(clientviewlist[1],clientviewlist[2]))
			. = max(., UI_UPDATE)

	// Check if the state allows interaction
	var/result = state.can_use_topic(src_object, user)
	. = max(., result)

 /**
  * private
  *
  * Checks if a user can use src_object's UI, and returns the state.
  * Can call a mob proc, which allows overrides for each mob.
  *
  * required src_object datum The object/datum which owns the UI.
  * required user mob The mob who opened/is using the UI.
  *
  * return UI_state The state of the UI.
 **/
/datum/ui_state/proc/can_use_topic(src_object, mob/user)
	return UI_CLOSE // Don't allow interaction by default.

 /**
  * public
  *
  * Standard interaction/sanity checks. Different mob types may have overrides.
  *
  * return UI_state The state of the UI.
 **/
/mob/proc/shared_ui_interaction(src_object)
	if(!client) // Close UIs if mindless.
		return UI_CLOSE
	else if(stat) // Disable UIs if unconcious.
		return UI_DISABLED
	else if(incapacitated()) // Update UIs if incapicitated but concious.
		return UI_UPDATE
	return UI_INTERACTIVE

/mob/living/shared_ui_interaction(src_object)
	. = ..()
	if(!(mobility_flags & MOBILITY_UI) && . == UI_INTERACTIVE)
		return UI_UPDATE

/mob/living/silicon/ai/shared_ui_interaction(src_object)
	if(lacks_power()) // Disable UIs if the AI is unpowered.
		return UI_DISABLED
	return ..()

/mob/living/silicon/robot/shared_ui_interaction(src_object)
	if(!cell || cell.charge <= 0 || lockcharge) // Disable UIs if the Borg is unpowered or locked.
		return UI_DISABLED
	return ..()

/**
  * public
  *
  * Check the distance for a living mob.
  * Really only used for checks outside the context of a mob.
  * Otherwise, use shared_living_ui_distance().
  *
  * required src_object The object which owns the UI.
  * required user mob The mob who opened/is using the UI.
  *
  * return UI_state The state of the UI.
 **/
/atom/proc/contents_ui_distance(src_object, mob/living/user)
	return user.shared_living_ui_distance(src_object) // Just call this mob's check.

 /**
  * public
  *
  * Distance versus interaction check.
  *
  * required src_object atom/movable The object which owns the UI.
  *
  * return UI_state The state of the UI.
 **/
/mob/living/proc/shared_living_ui_distance(atom/movable/src_object, viewcheck = TRUE)
	if(viewcheck && !(src_object in view(src))) // If the object is obscured, close it.
		return UI_CLOSE

	var/dist = get_dist(src_object, src)
	if(dist <= 1) // Open and interact if 1-0 tiles away.
		return UI_INTERACTIVE
	else if(dist <= 2) // View only if 2-3 tiles away.
		return UI_UPDATE
	else if(dist <= 5) // Disable if 5 tiles away.
		return UI_DISABLED
	return UI_CLOSE // Otherwise, we got nothing.

/mob/living/carbon/human/shared_living_ui_distance(atom/movable/src_object, viewcheck = TRUE)
	if(dna.check_mutation(TK) && tkMaxRangeCheck(src, src_object))
		return UI_INTERACTIVE
	return ..()
