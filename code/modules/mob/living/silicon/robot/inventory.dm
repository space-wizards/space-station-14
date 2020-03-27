//These procs handle putting stuff in your hand. It's probably best to use these rather than setting stuff manually
//as they handle all relevant stuff like adding it to the player's screen and such

//Returns the thing in our active hand (whatever is in our active module-slot, in this case)
/mob/living/silicon/robot/get_active_held_item()
	return module_active

/obj/item/proc/cyborg_unequip(mob/user)
	return

/mob/living/silicon/robot/proc/uneq_module(obj/item/O)
	if(!O)
		return 0
	O.mouse_opacity = MOUSE_OPACITY_OPAQUE
	if(istype(O, /obj/item/borg/sight))
		var/obj/item/borg/sight/S = O
		sight_mode &= ~S.sight_mode
		update_sight()
	else if(istype(O, /obj/item/storage/bag/tray/))
		SEND_SIGNAL(O, COMSIG_TRY_STORAGE_QUICK_EMPTY)
	if(client)
		client.screen -= O
	observer_screen_update(O,FALSE)

	if(module_active == O)
		module_active = null
	if(held_items[1] == O)
		inv1.icon_state = "inv1"
		held_items[1] = null
	else if(held_items[2] == O)
		inv2.icon_state = "inv2"
		held_items[2] = null
	else if(held_items[3] == O)
		inv3.icon_state = "inv3"
		held_items[3] = null

	if(O.item_flags & DROPDEL)
		O.item_flags &= ~DROPDEL //we shouldn't HAVE things with DROPDEL_1 in our modules, but better safe than runtiming horribly

	O.forceMove(module) //Return item to module so it appears in its contents, so it can be taken out again.
	O.cyborg_unequip(src)

	hud_used.update_robot_modules_display()
	return 1

/mob/living/silicon/robot/proc/activate_module(obj/item/O)
	. = FALSE
	if(!(O in module.modules))
		return
	if(activated(O))
		to_chat(src, "<span class='warning'>That module is already activated.</span>")
		return
	if(!held_items[1])
		held_items[1] = O
		O.screen_loc = inv1.screen_loc
		. = TRUE
	else if(!held_items[2])
		held_items[2] = O
		O.screen_loc = inv2.screen_loc
		. = TRUE
	else if(!held_items[3])
		held_items[3] = O
		O.screen_loc = inv3.screen_loc
		. = TRUE
	else
		to_chat(src, "<span class='warning'>You need to disable a module first!</span>")
	if(.)
		O.equipped(src, ITEM_SLOT_HANDS)
		O.mouse_opacity = initial(O.mouse_opacity)
		O.layer = ABOVE_HUD_LAYER
		O.plane = ABOVE_HUD_PLANE
		observer_screen_update(O,TRUE)
		O.forceMove(src)
		if(istype(O, /obj/item/borg/sight))
			var/obj/item/borg/sight/S = O
			sight_mode |= S.sight_mode
			update_sight()


/mob/living/silicon/robot/proc/observer_screen_update(obj/item/I,add = TRUE)
	if(observers && observers.len)
		for(var/M in observers)
			var/mob/dead/observe = M
			if(observe.client && observe.client.eye == src)
				if(add)
					observe.client.screen += I
				else
					observe.client.screen -= I
			else
				observers -= observe
				if(!observers.len)
					observers = null
					break

/mob/living/silicon/robot/proc/uneq_active()
	uneq_module(module_active)

/mob/living/silicon/robot/proc/uneq_all()
	for(var/obj/item/I in held_items)
		uneq_module(I)

/mob/living/silicon/robot/proc/activated(obj/item/O)
	if(O in held_items)
		return TRUE
	return FALSE

//Helper procs for cyborg modules on the UI.
//These are hackish but they help clean up code elsewhere.

//module_selected(module) - Checks whether the module slot specified by "module" is currently selected.
/mob/living/silicon/robot/proc/module_selected(module) //Module is 1-3
	return module == get_selected_module()

//module_active(module) - Checks whether there is a module active in the slot specified by "module".
/mob/living/silicon/robot/proc/module_active(module) //Module is 1-3
	if(module < 1 || module > 3)
		return FALSE

	if(LAZYLEN(held_items) >= module)
		if(held_items[module])
			return TRUE
	return FALSE

//get_selected_module() - Returns the slot number of the currently selected module.  Returns 0 if no modules are selected.
/mob/living/silicon/robot/proc/get_selected_module()
	if(module_active)
		return held_items.Find(module_active)

	return 0

//select_module(module) - Selects the module slot specified by "module"
/mob/living/silicon/robot/proc/select_module(module) //Module is 1-3
	if(module < 1 || module > 3)
		return

	if(!module_active(module))
		return

	switch(module)
		if(1)
			if(module_active != held_items[module])
				inv1.icon_state = "inv1 +a"
				inv2.icon_state = "inv2"
				inv3.icon_state = "inv3"
		if(2)
			if(module_active != held_items[module])
				inv1.icon_state = "inv1"
				inv2.icon_state = "inv2 +a"
				inv3.icon_state = "inv3"
		if(3)
			if(module_active != held_items[module])
				inv1.icon_state = "inv1"
				inv2.icon_state = "inv2"
				inv3.icon_state = "inv3 +a"
	module_active = held_items[module]

//deselect_module(module) - Deselects the module slot specified by "module"
/mob/living/silicon/robot/proc/deselect_module(module) //Module is 1-3
	if(module < 1 || module > 3)
		return

	if(!module_active(module))
		return

	switch(module)
		if(1)
			if(module_active == held_items[module])
				inv1.icon_state = "inv1"
		if(2)
			if(module_active == held_items[module])
				inv2.icon_state = "inv2"
		if(3)
			if(module_active == held_items[module])
				inv3.icon_state = "inv3"
	module_active = null

//toggle_module(module) - Toggles the selection of the module slot specified by "module".
/mob/living/silicon/robot/proc/toggle_module(module) //Module is 1-3
	if(module < 1 || module > 3)
		return

	if(module_selected(module))
		deselect_module(module)
	else
		if(module_active(module))
			select_module(module)
		else
			deselect_module(get_selected_module()) //If we can't do select anything, at least deselect the current module.
	return

//cycle_modules() - Cycles through the list of selected modules.
/mob/living/silicon/robot/proc/cycle_modules()
	var/slot_start = get_selected_module()
	if(slot_start)
		deselect_module(slot_start) //Only deselect if we have a selected slot.

	var/slot_num
	if(slot_start == 0)
		slot_num = 1
		slot_start = 4
	else
		slot_num = slot_start + 1

	while(slot_num != slot_start) //If we wrap around without finding any free slots, just give up.
		if(module_active(slot_num))
			select_module(slot_num)
			return
		slot_num++
		if(slot_num > 4) // not >3 otherwise cycling with just one item on module 3 wouldn't work
			slot_num = 1 //Wrap around.



/mob/living/silicon/robot/swap_hand()
	cycle_modules()
