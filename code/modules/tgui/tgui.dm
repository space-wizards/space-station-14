 /**
  * tgui
  *
  * /tg/station user interface library
 **/

 /**
  * tgui datum (represents a UI).
 **/
/datum/tgui
	/// The mob who opened/is using the UI.
	var/mob/user
	/// The object which owns the UI.
	var/datum/src_object
	/// The title of te UI.
	var/title
	/// The ui_key of the UI. This allows multiple UIs for one src_object.
	var/ui_key
	/// The window_id for browse() and onclose().
	var/window_id
	/// The window width.
	var/width = 0
	/// The window height
	var/height = 0
	/// The style to be used for this UI.
	var/style = "nanotrasen"
	/// The interface (template) to be used for this UI.
	var/interface
	/// Update the UI every MC tick.
	var/autoupdate = TRUE
	/// If the UI has been initialized yet.
	var/initialized = FALSE
	/// The data (and datastructure) used to initialize the UI.
	var/list/initial_data
	/// The static data used to initialize the UI.
	var/list/initial_static_data
	/// The status/visibility of the UI.
	var/status = UI_INTERACTIVE
	/// Topic state used to determine status/interactability.
	var/datum/ui_state/state = null
	/// The parent UI.
	var/datum/tgui/master_ui
	/// Children of this UI.
	var/list/datum/tgui/children = list()
	var/custom_browser_id = FALSE
	var/ui_screen = "home"

 /**
  * public
  *
  * Create a new UI.
  *
  * required user mob The mob who opened/is using the UI.
  * required src_object datum The object or datum which owns the UI.
  * required ui_key string The ui_key of the UI.
  * required interface string The interface used to render the UI.
  * optional title string The title of the UI.
  * optional width int The window width.
  * optional height int The window height.
  * optional master_ui datum/tgui The parent UI.
  * optional state datum/ui_state The state used to determine status.
  *
  * return datum/tgui The requested UI.
 **/
/datum/tgui/New(mob/user, datum/src_object, ui_key, interface, title, width = 0, height = 0, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state, browser_id = null)
	src.user = user
	src.src_object = src_object
	src.ui_key = ui_key
	src.window_id = browser_id ? browser_id : "[REF(src_object)]-[ui_key]" // DO NOT replace with \ref here. src_object could potentially be tagged
	src.custom_browser_id = browser_id ? TRUE : FALSE

	set_interface(interface)

	if(title)
		src.title = sanitize(title)
	if(width)
		src.width = width
	if(height)
		src.height = height

	src.master_ui = master_ui
	if(master_ui)
		master_ui.children += src
	src.state = state

	var/datum/asset/assets = get_asset_datum(/datum/asset/group/tgui)
	assets.send(user)

 /**
  * public
  *
  * Open this UI (and initialize it with data).
 **/
/datum/tgui/proc/open()
	if(!user.client)
		return // Bail if there is no client.

	update_status(push = FALSE) // Update the window status.
	if(status < UI_UPDATE)
		return // Bail if we're not supposed to open.

	var/window_size
	if(width && height) // If we have a width and height, use them.
		window_size = "size=[width]x[height];"
	else
		window_size = ""

	// Remove titlebar and resize handles for a fancy window
	var/have_title_bar
	if(user.client.prefs.tgui_fancy)
		have_title_bar = "titlebar=0;can_resize=0;"
	else
		have_title_bar = "titlebar=1;can_resize=1;"

	// Generate page html
	var/html
	html = SStgui.basehtml
	// Allow the src object to override the html if needed
	html = src_object.ui_base_html(html)
	// Replace template tokens with important UI data
	// NOTE: Intentional \ref usage; tgui datums can't/shouldn't
	// be tagged, so this is an effective unwrap
	html = replacetextEx(html, "\[ref]", "\ref[src]")
	html = replacetextEx(html, "\[style]", style)

	// Open the window.
	user << browse(html, "window=[window_id];can_minimize=0;auto_format=0;[window_size][have_title_bar]")
	if (!custom_browser_id)
		// Instruct the client to signal UI when the window is closed.
		// NOTE: Intentional \ref usage; tgui datums can't/shouldn't
		// be tagged, so this is an effective unwrap
		winset(user, window_id, "on-close=\"uiclose \ref[src]\"")

	if(!initial_data)
		initial_data = src_object.ui_data(user)
	if(!initial_static_data)
		initial_static_data = src_object.ui_static_data(user)

	SStgui.on_open(src)

 /**
  * public
  *
  * Reinitialize the UI.
  * (Possibly with a new interface and/or data).
  *
  * optional template string The name of the new interface.
  * optional data list The new initial data.
 **/
/datum/tgui/proc/reinitialize(interface, list/data, list/static_data)
	if(interface)
		set_interface(interface) // Set a new interface.
	if(data)
		initial_data = data
	if(static_data)
		initial_static_data = static_data
	open()

 /**
  * public
  *
  * Close the UI, and all its children.
 **/
/datum/tgui/proc/close()
	user << browse(null, "window=[window_id]") // Close the window.
	src_object.ui_close()
	SStgui.on_close(src)
	for(var/datum/tgui/child in children) // Loop through and close all children.
		child.close()
	children.Cut()
	state = null
	master_ui = null
	qdel(src)

 /**
  * public
  *
  * Set the style for this UI.
  *
  * required style string The new UI style.
 **/
/datum/tgui/proc/set_style(style)
	src.style = lowertext(style)

 /**
  * public
  *
  * Set the interface (template) for this UI.
  *
  * required interface string The new UI interface.
 **/
/datum/tgui/proc/set_interface(interface)
	src.interface = lowertext(interface)

 /**
  * public
  *
  * Enable/disable auto-updating of the UI.
  *
  * required state bool Enable/disable auto-updating.
 **/
/datum/tgui/proc/set_autoupdate(state = TRUE)
	autoupdate = state

 /**
  * private
  *
  * Package the data to send to the UI, as JSON.
  * This includes the UI data and config_data.
  *
  * return string The packaged JSON.
 **/
/datum/tgui/proc/get_json(list/data, list/static_data)
	var/list/json_data = list()

	json_data["config"] = list(
		"title" = title,
		"status" = status,
		"screen" = ui_screen,
		"style" = style,
		"interface" = interface,
		"fancy" = user.client.prefs.tgui_fancy,
		"locked" = user.client.prefs.tgui_lock && !custom_browser_id,
		"observer" = isobserver(user),
		"window" = window_id,
		// NOTE: Intentional \ref usage; tgui datums can't/shouldn't
		// be tagged, so this is an effective unwrap
		"ref" = "\ref[src]"
	)
	
	if(!isnull(data))
		json_data["data"] = data
	if(!isnull(static_data))
		json_data["static_data"] = static_data

	// Generate the JSON.
	var/json = json_encode(json_data)
	// Strip #255/improper.
	json = replacetext(json, "\proper", "")
	json = replacetext(json, "\improper", "")
	return json

 /**
  * private
  *
  * Handle clicks from the UI.
  * Call the src_object's ui_act() if status is UI_INTERACTIVE.
  * If the src_object's ui_act() returns 1, update all UIs attacked to it.
 **/
/datum/tgui/Topic(href, href_list)
	if(user != usr)
		return // Something is not right here.

	var/action = href_list["action"]
	var/params = href_list; params -= "action"

	switch(action)
		if("tgui:initialize")
			user << output(url_encode(get_json(initial_data, initial_static_data)), "[custom_browser_id ? window_id : "[window_id].browser"]:initialize")
			initialized = TRUE
		if("tgui:view")
			if(params["screen"])
				ui_screen = params["screen"]
			SStgui.update_uis(src_object)
		if("tgui:log")
			// Force window to show frills on fatal errors
			if(params["fatal"])
				winset(user, window_id, "titlebar=1;can-resize=1;size=600x600")
			log_message(params["log"])
		if("tgui:link")
			user << link(params["url"])
		if("tgui:fancy")
			user.client.prefs.tgui_fancy = TRUE
		if("tgui:nofrills")
			user.client.prefs.tgui_fancy = FALSE
		else
			update_status(push = FALSE) // Update the window state.
			if(src_object.ui_act(action, params, src, state)) // Call ui_act() on the src_object.
				SStgui.update_uis(src_object) // Update if the object requested it.

 /**
  * private
  *
  * Update the UI.
  * Only updates the data if update is true, otherwise only updates the status.
  *
  * optional force bool If the UI should be forced to update.
 **/
/datum/tgui/process(force = FALSE)
	var/datum/host = src_object.ui_host(user)
	if(!src_object || !host || !user) // If the object or user died (or something else), abort.
		close()
		return

	if(status && (force || autoupdate))
		update() // Update the UI if the status and update settings allow it.
	else
		update_status(push = TRUE) // Otherwise only update status.

 /**
  * private
  *
  * Push data to an already open UI.
  *
  * required data list The data to send.
  * optional force bool If the update should be sent regardless of state.
 **/
/datum/tgui/proc/push_data(data, static_data, force = FALSE)
	update_status(push = FALSE) // Update the window state.
	if(!initialized)
		return // Cannot update UI if it is not set up yet.
	if(status <= UI_DISABLED && !force)
		return // Cannot update UI, we have no visibility.

	// Send the new JSON to the update() Javascript function.
	user << output(url_encode(get_json(data, static_data)), "[custom_browser_id ? window_id : "[window_id].browser"]:update")

 /**
  * private
  *
  * Updates the UI by interacting with the src_object again, which will hopefully
  * call try_ui_update on it.
  *
  * optional force_open bool If force_open should be passed to ui_interact.
 **/
/datum/tgui/proc/update(force_open = FALSE)
	src_object.ui_interact(user, ui_key, src, force_open, master_ui, state)

 /**
  * private
  *
  * Update the status/visibility of the UI for its user.
  *
  * optional push bool Push an update to the UI (an update is always sent for UI_DISABLED).
 **/
/datum/tgui/proc/update_status(push = FALSE)
	var/status = src_object.ui_status(user, state)
	if(master_ui)
		status = min(status, master_ui.status)
	set_status(status, push)
	if(status == UI_CLOSE)
		close()

 /**
  * private
  *
  * Set the status/visibility of the UI.
  *
  * required status int The status to set (UI_CLOSE/UI_DISABLED/UI_UPDATE/UI_INTERACTIVE).
  * optional push bool Push an update to the UI (an update is always sent for UI_DISABLED).
 **/
/datum/tgui/proc/set_status(status, push = FALSE)
	if(src.status != status) // Only update if status has changed.
		if(src.status == UI_DISABLED)
			src.status = status
			if(push)
				update()
		else
			src.status = status
			if(status == UI_DISABLED || push) // Update if the UI just because disabled, or a push is requested.
				push_data(null, force = TRUE)

/datum/tgui/proc/log_message(message)
	log_tgui("[user] ([user.ckey]) using \"[title]\":\n[message]")
