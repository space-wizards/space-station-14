 /**
  * tgui external
  *
  * Contains all external tgui declarations.
 **/

 /**
  * public
  *
  * Used to open and update UIs.
  * If this proc is not implemented properly, the UI will not update correctly.
  *
  * required user mob The mob who opened/is using the UI.
  * optional ui_key string The ui_key of the UI.
  * optional ui datum/tgui The UI to be updated, if it exists.
  * optional force_open bool If the UI should be re-opened instead of updated.
  * optional master_ui datum/tgui The parent UI.
  * optional state datum/ui_state The state used to determine status.
 **/
/datum/proc/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	return FALSE // Not implemented.

 /**
  * public
  *
  * Data to be sent to the UI.
  * This must be implemented for a UI to work.
  *
  * required user mob The mob interacting with the UI.
  *
  * return list Data to be sent to the UI.
 **/
/datum/proc/ui_data(mob/user)
	return list() // Not implemented.

 /**
  * public
  *
  * Static Data to be sent to the UI.
  * Static data differs from normal data in that it's large data that should be sent infrequently
  * This is implemented optionally for heavy uis that would be sending a lot of redundant data
  * frequently.
  * Gets squished into one object on the frontend side, but the static part is cached.
  *
  * required user mob The mob interacting with the UI.
  *
  * return list Statuic Data to be sent to the UI.
 **/
/datum/proc/ui_static_data(mob/user)
	return list()

/**
  * public
  *
  * Forces an update on static data. Should be done manually whenever something happens to change static data.
  *
  * required user the mob currently interacting with the ui
  * optional ui ui to be updated
  * optional ui_key ui key of ui to be updated
  *
**/
/datum/proc/update_static_data(mob/user, datum/tgui/ui, ui_key = "main")
	ui = SStgui.try_update_ui(user, src, ui_key, ui)
	if(!ui)
		return //If there was no ui to update, there's no static data to update either.
	ui.push_data(null, ui_static_data(), TRUE)

 /**
  * public
  *
  * Called on a UI when the UI receieves a href.
  * Think of this as Topic().
  *
  * required action string The action/button that has been invoked by the user.
  * required params list A list of parameters attached to the button.
  *
  * return bool If the UI should be updated or not.
 **/
/datum/proc/ui_act(action, list/params, datum/tgui/ui, datum/ui_state/state)
	if(!ui || ui.status != UI_INTERACTIVE)
		return 1 // If UI is not interactive or usr calling Topic is not the UI user, bail.

 /**
  * public
  *
  * Called on an object when a tgui object is being created, allowing you to customise the html
  * For example: inserting a custom stylesheet that you need in the head
  *
  * For this purpose, some tags are available in the html, to be parsed out with replacetext
  * (customheadhtml) - Additions to the head tag
  *
  * required html the html base text
  *
 **/
/datum/proc/ui_base_html(html)
	return html

 /**
  * private
  *
  * The UI's host object (usually src_object).
  * This allows modules/datums to have the UI attached to them,
  * and be a part of another object.
 **/
/datum/proc/ui_host(mob/user)
	return src // Default src.

 /**
  * global
  *
  * Used to track UIs for a mob.
 **/
/mob/var/list/open_uis = list()
 /**
  * public
  *
  * Called on a UI's object when the UI is closed, not to be confused with client/verb/uiclose(), which closes the ui window
  *
  *
 **/
/datum/proc/ui_close()

 /**
  * verb
  *
  * Called by UIs when they are closed.
  * Must be a verb so winset() can call it.
  *
  * required uiref ref The UI that was closed.
 **/
/client/verb/uiclose(ref as text)
	// Name the verb, and hide it from the user panel.
	set name = "uiclose"
	set hidden = 1

	// Get the UI based on the ref.
	var/datum/tgui/ui = locate(ref)

	// If we found the UI, close it.
	if(istype(ui))
		ui.close()
		// Unset machine just to be sure.
		if(src && src.mob)
			src.mob.unset_machine()
