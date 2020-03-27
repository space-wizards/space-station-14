/obj/item/twohanded/rcl
	name = "rapid pipe cleaner layer"
	desc = "A device used to rapidly deploy pipe cleaners. It has screws on the side which can be removed to slide off the pipe cleaners. Do not use without insulation!"
	icon = 'icons/obj/tools.dmi'
	icon_state = "rcl-0"
	item_state = "rcl-0"
	var/obj/structure/pipe_cleaner/last
	var/obj/item/stack/pipe_cleaner_coil/loaded
	opacity = FALSE
	force = 5 //Plastic is soft
	throwforce =5
	throw_speed = 1
	throw_range = 7
	w_class = WEIGHT_CLASS_NORMAL
	var/max_amount = 90
	var/active = FALSE
	actions_types = list(/datum/action/item_action/rcl_col,/datum/action/item_action/rcl_gui,)
	var/list/colors = list("red", "yellow", "green", "blue", "pink", "orange", "cyan", "white")
	var/current_color_index = 1
	var/ghetto = FALSE
	lefthand_file = 'icons/mob/inhands/equipment/tools_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/tools_righthand.dmi'
	var/datum/radial_menu/persistent/wiring_gui_menu
	var/mob/listeningTo

/obj/item/twohanded/rcl/ComponentInitialize()
	. = ..()
	AddElement(/datum/element/update_icon_updates_onmob)

/obj/item/twohanded/rcl/attackby(obj/item/W, mob/user)
	if(istype(W, /obj/item/stack/pipe_cleaner_coil))
		var/obj/item/stack/pipe_cleaner_coil/C = W

		if(!loaded)
			if(!user.transferItemToLoc(W, src))
				to_chat(user, "<span class='warning'>[src] is stuck to your hand!</span>")
				return
			else
				loaded = W //W.loc is src at this point.
				loaded.max_amount = max_amount //We store a lot.
				return

		if(loaded.amount < max_amount)
			var/transfer_amount = min(max_amount - loaded.amount, C.amount)
			C.use(transfer_amount)
			loaded.amount += transfer_amount
		else
			return
		update_icon()
		to_chat(user, "<span class='notice'>You add the pipe cleaners to [src]. It now contains [loaded.amount].</span>")
	else if(W.tool_behaviour == TOOL_SCREWDRIVER)
		if(!loaded)
			return
		if(ghetto && prob(10)) //Is it a ghetto RCL? If so, give it a 10% chance to fall apart
			to_chat(user, "<span class='warning'>You attempt to loosen the securing screws on the side, but it falls apart!</span>")
			while(loaded.amount > 30) //There are only two kinds of situations: "nodiff" (60,90), or "diff" (31-59, 61-89)
				var/diff = loaded.amount % 30
				if(diff)
					loaded.use(diff)
					new /obj/item/stack/pipe_cleaner_coil(get_turf(user), diff)
				else
					loaded.use(30)
					new /obj/item/stack/pipe_cleaner_coil(get_turf(user), 30)
			qdel(src)
			return

		to_chat(user, "<span class='notice'>You loosen the securing screws on the side, allowing you to lower the guiding edge and retrieve the wires.</span>")
		while(loaded.amount > 30) //There are only two kinds of situations: "nodiff" (60,90), or "diff" (31-59, 61-89)
			var/diff = loaded.amount % 30
			if(diff)
				loaded.use(diff)
				new /obj/item/stack/pipe_cleaner_coil(get_turf(user), diff)
			else
				loaded.use(30)
				new /obj/item/stack/pipe_cleaner_coil(get_turf(user), 30)
		loaded.max_amount = initial(loaded.max_amount)
		if(!user.put_in_hands(loaded))
			loaded.forceMove(get_turf(user))

		loaded = null
		update_icon()
	else
		..()

/obj/item/twohanded/rcl/examine(mob/user)
	. = ..()
	if(loaded)
		. += "<span class='info'>It contains [loaded.amount]/[max_amount] pipe cleaners.</span>"

/obj/item/twohanded/rcl/Destroy()
	QDEL_NULL(loaded)
	last = null
	listeningTo = null
	QDEL_NULL(wiring_gui_menu)
	return ..()

/obj/item/twohanded/rcl/update_icon_state()
	if(!loaded)
		icon_state = "rcl-0"
		item_state = "rcl-0"
		return
	switch(loaded.amount)
		if(61 to INFINITY)
			icon_state = "rcl-30"
			item_state = "rcl"
		if(31 to 60)
			icon_state = "rcl-20"
			item_state = "rcl"
		if(1 to 30)
			icon_state = "rcl-10"
			item_state = "rcl"
		else
			icon_state = "rcl-0"
			item_state = "rcl-0"

/obj/item/twohanded/rcl/proc/is_empty(mob/user, loud = 1)
	update_icon()
	if(!loaded || !loaded.amount)
		if(loud)
			to_chat(user, "<span class='notice'>The last of the pipe cleaners unreel from [src].</span>")
		if(loaded)
			QDEL_NULL(loaded)
			loaded = null
		QDEL_NULL(wiring_gui_menu)
		unwield(user)
		active = wielded
		return TRUE
	return FALSE

/obj/item/twohanded/rcl/pickup(mob/user)
	..()
	getMobhook(user)


/obj/item/twohanded/rcl/dropped(mob/wearer)
	..()
	UnregisterSignal(wearer, COMSIG_MOVABLE_MOVED)
	listeningTo = null
	last = null
	QDEL_NULL(wiring_gui_menu)

/obj/item/twohanded/rcl/attack_self(mob/user)
	..()
	active = wielded
	if(!active)
		last = null
	else if(!last)
		for(var/obj/structure/pipe_cleaner/C in get_turf(user))
			if(C.d1 == FALSE || C.d2 == FALSE)
				last = C
				break

/obj/item/twohanded/rcl/proc/getMobhook(mob/to_hook)
	if(listeningTo == to_hook)
		return
	if(listeningTo)
		UnregisterSignal(listeningTo, COMSIG_MOVABLE_MOVED)
	RegisterSignal(to_hook, COMSIG_MOVABLE_MOVED, .proc/trigger)
	listeningTo = to_hook

/obj/item/twohanded/rcl/proc/trigger(mob/user)
	if(active)
		layCable(user)
	if(wiring_gui_menu) //update the wire options as you move
		wiringGuiUpdate(user)


//previous contents of trigger(), lays pipe_cleaner each time the player moves
/obj/item/twohanded/rcl/proc/layCable(mob/user)
	if(!isturf(user.loc))
		return
	if(is_empty(user, 0))
		to_chat(user, "<span class='warning'>\The [src] is empty!</span>")
		return

	if(prob(2) && ghetto) //Give ghetto RCLs a 2% chance to jam, requiring it to be reactviated manually.
		to_chat(user, "<span class='warning'>[src]'s wires jam!</span>")
		active = FALSE
		return
	else
		if(last)
			if(get_dist(last, user) == 1) //hacky, but it works
				var/turf/T = get_turf(user)
				if(T.intact || !T.can_have_cabling())
					last = null
					return
				if(get_dir(last, user) == last.d2)
					//Did we just walk backwards? Well, that's the one direction we CAN'T complete a stub.
					last = null
					return
				loaded.pipe_cleaner_join(last, user, FALSE)
				if(is_empty(user))
					return //If we've run out, display message and exit
			else
				last = null
		loaded.pipe_cleaner_color = colors[current_color_index]
		last = loaded.place_turf(get_turf(src), user, turn(user.dir, 180))
		is_empty(user) //If we've run out, display message
	update_icon()


//searches the current tile for a stub pipe_cleaner of the same colour
/obj/item/twohanded/rcl/proc/findLinkingCable(mob/user)
	var/turf/T
	if(!isturf(user.loc))
		return

	T = get_turf(user)
	if(T.intact || !T.can_have_cabling())
		return

	for(var/obj/structure/pipe_cleaner/C in T)
		if(!C)
			continue
		if(C.pipe_cleaner_color != GLOB.pipe_cleaner_colors[colors[current_color_index]])
			continue
		if(C.d1 == 0)
			return C
	return


/obj/item/twohanded/rcl/proc/wiringGuiGenerateChoices(mob/user)
	var/fromdir = 0
	var/obj/structure/pipe_cleaner/linkingCable = findLinkingCable(user)
	if(linkingCable)
		fromdir = linkingCable.d2

	var/list/wiredirs = list("1","5","4","6","2","10","8","9")
	for(var/icondir in wiredirs)
		var/dirnum = text2num(icondir)
		var/pipe_cleanersuffix = "[min(fromdir,dirnum)]-[max(fromdir,dirnum)]"
		if(fromdir == dirnum) //pipe_cleaners can't loop back on themselves
			pipe_cleanersuffix = "invalid"
		var/image/img = image(icon = 'icons/mob/radial.dmi', icon_state = "cable_[pipe_cleanersuffix]")
		img.color = GLOB.pipe_cleaner_colors[colors[current_color_index]]
		wiredirs[icondir] = img
	return wiredirs

/obj/item/twohanded/rcl/proc/showWiringGui(mob/user)
	var/list/choices = wiringGuiGenerateChoices(user)

	wiring_gui_menu = show_radial_menu_persistent(user, src , choices, select_proc = CALLBACK(src, .proc/wiringGuiReact, user), radius = 42)

/obj/item/twohanded/rcl/proc/wiringGuiUpdate(mob/user)
	if(!wiring_gui_menu)
		return

	wiring_gui_menu.entry_animation = FALSE //stop the open anim from playing each time we update
	var/list/choices = wiringGuiGenerateChoices(user)

	wiring_gui_menu.change_choices(choices,FALSE)


//Callback used to respond to interactions with the wiring menu
/obj/item/twohanded/rcl/proc/wiringGuiReact(mob/living/user,choice)
	if(!choice) //close on a null choice (the center button)
		QDEL_NULL(wiring_gui_menu)
		return

	choice = text2num(choice)

	if(!isturf(user.loc))
		return
	if(is_empty(user, 0))
		to_chat(user, "<span class='warning'>\The [src] is empty!</span>")
		return

	var/turf/T = get_turf(user)
	if(T.intact || !T.can_have_cabling())
		return

	loaded.pipe_cleaner_color = colors[current_color_index]

	var/obj/structure/pipe_cleaner/linkingCable = findLinkingCable(user)
	if(linkingCable)
		if(choice != linkingCable.d2)
			loaded.pipe_cleaner_join(linkingCable, user, FALSE, choice)
			last = null
	else
		last = loaded.place_turf(get_turf(src), user, choice)

	is_empty(user) //If we've run out, display message

	wiringGuiUpdate(user)


/obj/item/twohanded/rcl/pre_loaded/Initialize() //Comes preloaded with pipe_cleaner, for testing stuff
	. = ..()
	loaded = new()
	loaded.max_amount = max_amount
	loaded.amount = max_amount
	update_icon()

/obj/item/twohanded/rcl/Initialize()
	. = ..()
	update_icon()

/obj/item/twohanded/rcl/ui_action_click(mob/user, action)
	if(istype(action, /datum/action/item_action/rcl_col))
		current_color_index++;
		if (current_color_index > colors.len)
			current_color_index = 1
		var/cwname = colors[current_color_index]
		to_chat(user, "Color changed to [cwname]!")
		if(loaded)
			loaded.pipe_cleaner_color = colors[current_color_index]
		if(wiring_gui_menu)
			wiringGuiUpdate(user)
	else if(istype(action, /datum/action/item_action/rcl_gui))
		if(wiring_gui_menu) //The menu is already open, close it
			QDEL_NULL(wiring_gui_menu)
		else //open the menu
			showWiringGui(user)

/obj/item/twohanded/rcl/ghetto
	actions_types = list()
	max_amount = 30
	name = "makeshift rapid pipe cleaner layer"
	ghetto = TRUE

/obj/item/twohanded/rcl/ghetto/update_icon_state()
	if(!loaded)
		icon_state = "rclg-0"
		item_state = "rclg-0"
		return
	switch(loaded.amount)
		if(1 to INFINITY)
			icon_state = "rclg-1"
			item_state = "rcl"
		else
			icon_state = "rclg-1"
			item_state = "rclg-1"
