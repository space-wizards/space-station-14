#define ACTION_BUTTON_DEFAULT_BACKGROUND "default"

/obj/screen/movable/action_button
	var/datum/action/linked_action
	var/actiontooltipstyle = ""
	screen_loc = null

	var/button_icon_state
	var/appearance_cache

	var/id
	var/ordered = TRUE //If the button gets placed into the default bar

/obj/screen/movable/action_button/proc/can_use(mob/user)
	if (linked_action)
		return linked_action.owner == user
	else if (isobserver(user))
		var/mob/dead/observer/O = user
		return !O.observetarget
	else
		return TRUE

/obj/screen/movable/action_button/MouseDrop(over_object)
	if(!can_use(usr))
		return
	if((istype(over_object, /obj/screen/movable/action_button) && !istype(over_object, /obj/screen/movable/action_button/hide_toggle)))
		if(locked)
			to_chat(usr, "<span class='warning'>Action button \"[name]\" is locked, unlock it first.</span>")
			return
		var/obj/screen/movable/action_button/B = over_object
		var/list/actions = usr.actions
		actions.Swap(actions.Find(src.linked_action), actions.Find(B.linked_action))
		moved = FALSE
		ordered = TRUE
		B.moved = FALSE
		B.ordered = TRUE
		usr.update_action_buttons()
	else
		return ..()

/obj/screen/movable/action_button/Click(location,control,params)
	if (!can_use(usr))
		return

	var/list/modifiers = params2list(params)
	if(modifiers["shift"])
		if(locked)
			to_chat(usr, "<span class='warning'>Action button \"[name]\" is locked, unlock it first.</span>")
			return TRUE
		moved = 0
		usr.update_action_buttons() //redraw buttons that are no longer considered "moved"
		return TRUE
	if(modifiers["ctrl"])
		locked = !locked
		to_chat(usr, "<span class='notice'>Action button \"[name]\" [locked ? "" : "un"]locked.</span>")
		if(id && usr.client) //try to (un)remember position
			usr.client.prefs.action_buttons_screen_locs["[name]_[id]"] = locked ? moved : null
		return TRUE
	if(usr.next_click > world.time)
		return
	usr.next_click = world.time + 1
	linked_action.Trigger()
	return TRUE

//Hide/Show Action Buttons ... Button
/obj/screen/movable/action_button/hide_toggle
	name = "Hide Buttons"
	desc = "Shift-click any button to reset its position, and Control-click it to lock it in place. Alt-click this button to reset all buttons to their default positions."
	icon = 'icons/mob/actions.dmi'
	icon_state = "bg_default"
	var/hidden = 0
	var/hide_icon = 'icons/mob/actions.dmi'
	var/hide_state = "hide"
	var/show_state = "show"
	var/mutable_appearance/hide_appearance
	var/mutable_appearance/show_appearance

/obj/screen/movable/action_button/hide_toggle/Initialize()
	. = ..()
	var/static/list/icon_cache = list()
	
	var/cache_key = "[hide_icon][hide_state]"
	hide_appearance = icon_cache[cache_key]
	if(!hide_appearance)
		hide_appearance = icon_cache[cache_key] = mutable_appearance(hide_icon, hide_state)

	cache_key = "[hide_icon][show_state]"
	show_appearance = icon_cache[cache_key]
	if(!show_appearance)
		show_appearance = icon_cache[cache_key] = mutable_appearance(hide_icon, show_state)


/obj/screen/movable/action_button/hide_toggle/Click(location,control,params)
	if (!can_use(usr))
		return

	var/list/modifiers = params2list(params)
	if(modifiers["shift"])
		if(locked)
			to_chat(usr, "<span class='warning'>Action button \"[name]\" is locked, unlock it first.</span>")
			return TRUE
		moved = FALSE
		usr.update_action_buttons(TRUE)
		return TRUE
	if(modifiers["ctrl"])
		locked = !locked
		to_chat(usr, "<span class='notice'>Action button \"[name]\" [locked ? "" : "un"]locked.</span>")
		if(id && usr.client) //try to (un)remember position
			usr.client.prefs.action_buttons_screen_locs["[name]_[id]"] = locked ? moved : null
		return TRUE
	if(modifiers["alt"])
		for(var/V in usr.actions)
			var/datum/action/A = V
			var/obj/screen/movable/action_button/B = A.button
			B.moved = FALSE
			if(B.id && usr.client)
				usr.client.prefs.action_buttons_screen_locs["[B.name]_[B.id]"] = null
			B.locked = usr.client.prefs.buttons_locked
		locked = usr.client.prefs.buttons_locked
		moved = FALSE
		if(id && usr.client)
			usr.client.prefs.action_buttons_screen_locs["[name]_[id]"] = null
		usr.update_action_buttons(TRUE)
		to_chat(usr, "<span class='notice'>Action button positions have been reset.</span>")
		return TRUE
	usr.hud_used.action_buttons_hidden = !usr.hud_used.action_buttons_hidden

	hidden = usr.hud_used.action_buttons_hidden
	if(hidden)
		name = "Show Buttons"
	else
		name = "Hide Buttons"
	update_icon()
	usr.update_action_buttons()

/obj/screen/movable/action_button/hide_toggle/AltClick(mob/user)
	for(var/V in user.actions)
		var/datum/action/A = V
		var/obj/screen/movable/action_button/B = A.button
		B.moved = FALSE
	if(moved)
		moved = FALSE
	user.update_action_buttons(TRUE)
	to_chat(user, "<span class='notice'>Action button positions have been reset.</span>")


/obj/screen/movable/action_button/hide_toggle/proc/InitialiseIcon(datum/hud/owner_hud)
	var/settings = owner_hud.get_action_buttons_icons()
	icon = settings["bg_icon"]
	icon_state = settings["bg_state"]
	hide_icon = settings["toggle_icon"]
	hide_state = settings["toggle_hide"]
	show_state = settings["toggle_show"]
	update_icon()

/obj/screen/movable/action_button/hide_toggle/update_overlays()
	. = ..()
	if(hidden)
		. += show_appearance
	else
		. += hide_appearance

/obj/screen/movable/action_button/MouseEntered(location,control,params)
	if(!QDELETED(src))
		openToolTip(usr,src,params,title = name,content = desc,theme = actiontooltipstyle)


/obj/screen/movable/action_button/MouseExited()
	closeToolTip(usr)

/datum/hud/proc/get_action_buttons_icons()
	. = list()
	.["bg_icon"] = ui_style
	.["bg_state"] = "template"

	//TODO : Make these fit theme
	.["toggle_icon"] = 'icons/mob/actions.dmi'
	.["toggle_hide"] = "hide"
	.["toggle_show"] = "show"

//see human and alien hud for specific implementations.

/mob/proc/update_action_buttons_icon(status_only = FALSE)
	for(var/X in actions)
		var/datum/action/A = X
		A.UpdateButtonIcon(status_only)

//This is the proc used to update all the action buttons.
/mob/proc/update_action_buttons(reload_screen)
	if(!hud_used || !client)
		return

	if(hud_used.hud_shown != HUD_STYLE_STANDARD)
		return

	var/button_number = 0

	if(hud_used.action_buttons_hidden)
		for(var/datum/action/A in actions)
			A.button.screen_loc = null
			if(reload_screen)
				client.screen += A.button
	else
		for(var/datum/action/A in actions)
			A.UpdateButtonIcon()
			var/obj/screen/movable/action_button/B = A.button
			if(B.ordered)
				button_number++
			if(B.moved)
				B.screen_loc = B.moved
			else
				B.screen_loc = hud_used.ButtonNumberToScreenCoords(button_number)
			if(reload_screen)
				client.screen += B

		if(!button_number)
			hud_used.hide_actions_toggle.screen_loc = null
			return

	if(!hud_used.hide_actions_toggle.moved)
		hud_used.hide_actions_toggle.screen_loc = hud_used.ButtonNumberToScreenCoords(button_number+1)
	else
		hud_used.hide_actions_toggle.screen_loc = hud_used.hide_actions_toggle.moved
	if(reload_screen)
		client.screen += hud_used.hide_actions_toggle



#define AB_MAX_COLUMNS 10

/datum/hud/proc/ButtonNumberToScreenCoords(number) // TODO : Make this zero-indexed for readabilty
	var/row = round((number - 1)/AB_MAX_COLUMNS)
	var/col = ((number - 1)%(AB_MAX_COLUMNS)) + 1

	var/coord_col = "+[col-1]"
	var/coord_col_offset = 4 + 2 * col

	var/coord_row = "[row ? -row : "+0"]"

	return "WEST[coord_col]:[coord_col_offset],NORTH[coord_row]:-6"

/datum/hud/proc/SetButtonCoords(obj/screen/button,number)
	var/row = round((number-1)/AB_MAX_COLUMNS)
	var/col = ((number - 1)%(AB_MAX_COLUMNS)) + 1
	var/x_offset = 32*(col-1) + 4 + 2*col
	var/y_offset = -32*(row+1) + 26

	var/matrix/M = matrix()
	M.Translate(x_offset,y_offset)
	button.transform = M
