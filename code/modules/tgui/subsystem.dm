 /**
  * tgui subsystem
  *
  * Contains all tgui state and subsystem code.
 **/

 /**
  * public
  *
  * Get a open UI given a user, src_object, and ui_key and try to update it with data.
  *
  * required user mob The mob who opened/is using the UI.
  * required src_object datum The object/datum which owns the UI.
  * required ui_key string The ui_key of the UI.
  * optional ui datum/tgui The UI to be updated, if it exists.
  * optional force_open bool If the UI should be re-opened instead of updated.
  *
  * return datum/tgui The found UI.
 **/
/datum/controller/subsystem/tgui/proc/try_update_ui(mob/user, datum/src_object, ui_key, datum/tgui/ui, force_open = FALSE)
	if(isnull(ui)) // No UI was passed, so look for one.
		ui = get_open_ui(user, src_object, ui_key)

	if(!isnull(ui))
		var/data = src_object.ui_data(user) // Get data from the src_object.
		if(!force_open) // UI is already open; update it.
			ui.push_data(data)
		else // Re-open it anyways.
			ui.reinitialize(null, data)
		return ui // We found the UI, return it.
	else
		return null // We couldn't find a UI.

 /**
  * private
  *
  * Get a open UI given a user, src_object, and ui_key.
  *
  * required user mob The mob who opened/is using the UI.
  * required src_object datum The object/datum which owns the UI.
  * required ui_key string The ui_key of the UI.
  *
  * return datum/tgui The found UI.
 **/
/datum/controller/subsystem/tgui/proc/get_open_ui(mob/user, datum/src_object, ui_key)
	var/src_object_key = "[REF(src_object)]"
	if(isnull(open_uis[src_object_key]) || !istype(open_uis[src_object_key], /list))
		return null // No UIs open.
	else if(isnull(open_uis[src_object_key][ui_key]) || !istype(open_uis[src_object_key][ui_key], /list))
		return null // No UIs open for this object.

	for(var/datum/tgui/ui in open_uis[src_object_key][ui_key]) // Find UIs for this object.
		if(ui.user == user) // Make sure we have the right user
			return ui

	return null // Couldn't find a UI!

 /**
  * private
  *
  * Update all UIs attached to src_object.
  *
  * required src_object datum The object/datum which owns the UIs.
  *
  * return int The number of UIs updated.
 **/
/datum/controller/subsystem/tgui/proc/update_uis(datum/src_object)
	var/src_object_key = "[REF(src_object)]"
	if(isnull(open_uis[src_object_key]) || !istype(open_uis[src_object_key], /list))
		return 0 // Couldn't find any UIs for this object.

	var/update_count = 0
	for(var/ui_key in open_uis[src_object_key])
		for(var/datum/tgui/ui in open_uis[src_object_key][ui_key])
			if(ui && ui.src_object && ui.user && ui.src_object.ui_host(ui.user)) // Check the UI is valid.
				ui.process(force = 1) // Update the UI.
				update_count++ // Count each UI we update.
	return update_count

 /**
  * private
  *
  * Close all UIs attached to src_object.
  *
  * required src_object datum The object/datum which owns the UIs.
  *
  * return int The number of UIs closed.
 **/
/datum/controller/subsystem/tgui/proc/close_uis(datum/src_object)
	var/src_object_key = "[REF(src_object)]"
	if(isnull(open_uis[src_object_key]) || !istype(open_uis[src_object_key], /list))
		return 0 // Couldn't find any UIs for this object.

	var/close_count = 0
	for(var/ui_key in open_uis[src_object_key])
		for(var/datum/tgui/ui in open_uis[src_object_key][ui_key])
			if(ui && ui.src_object && ui.user && ui.src_object.ui_host(ui.user)) // Check the UI is valid.
				ui.close() // Close the UI.
				close_count++ // Count each UI we close.
	return close_count

 /**
  * private
  *
  * Close *ALL* UIs
  *
  * return int The number of UIs closed.
 **/
/datum/controller/subsystem/tgui/proc/close_all_uis()
	var/close_count = 0
	for(var/src_object_key in open_uis)
		for(var/ui_key in open_uis[src_object_key])
			for(var/datum/tgui/ui in open_uis[src_object_key][ui_key])
				if(ui && ui.src_object && ui.user && ui.src_object.ui_host(ui.user)) // Check the UI is valid.
					ui.close() // Close the UI.
					close_count++ // Count each UI we close.
	return close_count

 /**
  * private
  *
  * Update all UIs belonging to a user.
  *
  * required user mob The mob who opened/is using the UI.
  * optional src_object datum If provided, only update UIs belonging this src_object.
  * optional ui_key string If provided, only update UIs with this UI key.
  *
  * return int The number of UIs updated.
 **/
/datum/controller/subsystem/tgui/proc/update_user_uis(mob/user, datum/src_object = null, ui_key = null)
	if(isnull(user.open_uis) || !istype(user.open_uis, /list) || open_uis.len == 0)
		return 0 // Couldn't find any UIs for this user.

	var/update_count = 0
	for(var/datum/tgui/ui in user.open_uis)
		if((isnull(src_object) || !isnull(src_object) && ui.src_object == src_object) && (isnull(ui_key) || !isnull(ui_key) && ui.ui_key == ui_key))
			ui.process(force = 1) // Update the UI.
			update_count++ // Count each UI we upadte.
	return update_count

 /**
  * private
  *
  * Close all UIs belonging to a user.
  *
  * required user mob The mob who opened/is using the UI.
  * optional src_object datum If provided, only close UIs belonging this src_object.
  * optional ui_key string If provided, only close UIs with this UI key.
  *
  * return int The number of UIs closed.
 **/
/datum/controller/subsystem/tgui/proc/close_user_uis(mob/user, datum/src_object = null, ui_key = null)
	if(isnull(user.open_uis) || !istype(user.open_uis, /list) || open_uis.len == 0)
		return 0 // Couldn't find any UIs for this user.

	var/close_count = 0
	for(var/datum/tgui/ui in user.open_uis)
		if((isnull(src_object) || !isnull(src_object) && ui.src_object == src_object) && (isnull(ui_key) || !isnull(ui_key) && ui.ui_key == ui_key))
			ui.close() // Close the UI.
			close_count++ // Count each UI we close.
	return close_count

 /**
  * private
  *
  * Add a UI to the list of open UIs.
  *
  * required ui datum/tgui The UI to be added.
 **/
/datum/controller/subsystem/tgui/proc/on_open(datum/tgui/ui)
	var/src_object_key = "[REF(ui.src_object)]"
	if(isnull(open_uis[src_object_key]) || !istype(open_uis[src_object_key], /list))
		open_uis[src_object_key] = list(ui.ui_key = list()) // Make a list for the ui_key and src_object.
	else if(isnull(open_uis[src_object_key][ui.ui_key]) || !istype(open_uis[src_object_key][ui.ui_key], /list))
		open_uis[src_object_key][ui.ui_key] = list() // Make a list for the ui_key.

	// Append the UI to all the lists.
	ui.user.open_uis |= ui
	var/list/uis = open_uis[src_object_key][ui.ui_key]
	uis |= ui
	processing_uis |= ui

 /**
  * private
  *
  * Remove a UI from the list of open UIs.
  *
  * required ui datum/tgui The UI to be removed.
  *
  * return bool If the UI was removed or not.
 **/
/datum/controller/subsystem/tgui/proc/on_close(datum/tgui/ui)
	var/src_object_key = "[REF(ui.src_object)]"
	if(isnull(open_uis[src_object_key]) || !istype(open_uis[src_object_key], /list))
		return 0 // It wasn't open.
	else if(isnull(open_uis[src_object_key][ui.ui_key]) || !istype(open_uis[src_object_key][ui.ui_key], /list))
		return 0 // It wasn't open.

	processing_uis.Remove(ui) // Remove it from the list of processing UIs.
	if(ui.user)	// If the user exists, remove it from them too.
		ui.user.open_uis.Remove(ui)
	var/Ukey = ui.ui_key
	var/list/uis = open_uis[src_object_key][Ukey] // Remove it from the list of open UIs.
	uis.Remove(ui)
	if(!uis.len)
		var/list/uiobj = open_uis[src_object_key]
		uiobj.Remove(Ukey)
		if(!uiobj.len)
			open_uis.Remove(src_object_key)

	return 1 // Let the caller know we did it.

 /**
  * private
  *
  * Handle client logout, by closing all their UIs.
  *
  * required user mob The mob which logged out.
  *
  * return int The number of UIs closed.
 **/
/datum/controller/subsystem/tgui/proc/on_logout(mob/user)
	return close_user_uis(user)

 /**
  * private
  *
  * Handle clients switching mobs, by transferring their UIs.
  *
  * required user source The client's original mob.
  * required user target The client's new mob.
  *
  * return bool If the UIs were transferred.
 **/
/datum/controller/subsystem/tgui/proc/on_transfer(mob/source, mob/target)
	if(!source || isnull(source.open_uis) || !istype(source.open_uis, /list) || open_uis.len == 0)
		return 0 // The old mob had no open UIs.

	if(isnull(target.open_uis) || !istype(target.open_uis, /list))
		target.open_uis = list() // Create a list for the new mob if needed.

	for(var/datum/tgui/ui in source.open_uis)
		ui.user = target // Inform the UIs of their new owner.
		target.open_uis.Add(ui) // Transfer all the UIs.

	source.open_uis.Cut() // Clear the old list.
	return 1 // Let the caller know we did it.
