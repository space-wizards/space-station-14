/obj/machinery/launchpad
	name = "bluespace launchpad"
	desc = "A bluespace pad able to thrust matter through bluespace, teleporting it to or from nearby locations."
	icon = 'icons/obj/telescience.dmi'
	icon_state = "lpad-idle"
	use_power = TRUE
	idle_power_usage = 200
	active_power_usage = 2500
	hud_possible = list(DIAG_LAUNCHPAD_HUD)
	circuit = /obj/item/circuitboard/machine/launchpad
	var/icon_teleport = "lpad-beam"
	var/stationary = TRUE //to prevent briefcase pad deconstruction and such
	var/display_name = "Launchpad"
	var/teleport_speed = 35
	var/range = 15
	var/teleporting = FALSE //if it's in the process of teleporting
	var/power_efficiency = 1
	var/x_offset = 0
	var/y_offset = 0
	var/indicator_icon = "launchpad_target"

/obj/machinery/launchpad/RefreshParts()
	var/E = 0
	for(var/obj/item/stock_parts/manipulator/M in component_parts)
		E += M.rating
	range = initial(range)
	range *= E

/obj/machinery/launchpad/Initialize()
	. = ..()
	prepare_huds()
	for(var/datum/atom_hud/data/diagnostic/diag_hud in GLOB.huds)
		diag_hud.add_to_hud(src)

	var/image/holder = hud_list[DIAG_LAUNCHPAD_HUD]
	var/mutable_appearance/MA = new /mutable_appearance()
	MA.icon = 'icons/effects/effects.dmi'
	MA.icon_state = "launchpad_target"
	MA.layer = ABOVE_OPEN_TURF_LAYER
	MA.plane = 0
	holder.appearance = MA

	update_indicator()

/obj/machinery/launchpad/Destroy()
	qdel(hud_list[DIAG_LAUNCHPAD_HUD])
	return ..()

/obj/machinery/launchpad/examine(mob/user)
	. = ..()
	if(in_range(user, src) || isobserver(user))
		. += "<span class='notice'>The status display reads: Maximum range: <b>[range]</b> units.</span>"

/obj/machinery/launchpad/attackby(obj/item/I, mob/user, params)
	if(stationary)
		if(default_deconstruction_screwdriver(user, "lpad-idle-o", "lpad-idle", I))
			update_indicator()
			return

		if(panel_open)
			if(I.tool_behaviour == TOOL_MULTITOOL)
				if(!multitool_check_buffer(user, I))
					return
				var/obj/item/multitool/M = I
				M.buffer = src
				to_chat(user, "<span class='notice'>You save the data in the [I.name]'s buffer.</span>")
				return 1

		if(default_deconstruction_crowbar(I))
			return

	return ..()

/obj/machinery/launchpad/attack_ghost(mob/dead/observer/ghost)
	. = ..()
	if(.)
		return
	var/target_x = x + x_offset
	var/target_y = y + y_offset
	var/turf/target = locate(target_x, target_y, z)
	ghost.forceMove(target)

/obj/machinery/launchpad/proc/isAvailable()
	if(stat & NOPOWER)
		return FALSE
	if(panel_open)
		return FALSE
	return TRUE

/obj/machinery/launchpad/proc/update_indicator()
	var/image/holder = hud_list[DIAG_LAUNCHPAD_HUD]
	var/turf/target_turf
	if(isAvailable())
		target_turf = locate(x + x_offset, y + y_offset, z)
	if(target_turf)
		holder.icon_state = indicator_icon
		holder.loc = target_turf
	else
		holder.icon_state = null

/obj/machinery/launchpad/proc/set_offset(x, y)
	if(teleporting)
		return
	if(!isnull(x))
		x_offset = CLAMP(x, -range, range)
	if(!isnull(y))
		y_offset = CLAMP(y, -range, range)
	update_indicator()

/obj/machinery/launchpad/proc/doteleport(mob/user, sending)
	if(teleporting)
		to_chat(user, "<span class='warning'>ERROR: Launchpad busy.</span>")
		return

	var/turf/dest = get_turf(src)

	if(dest && is_centcom_level(dest.z))
		to_chat(user, "<span class='warning'>ERROR: Launchpad not operative. Heavy area shielding makes teleporting impossible.</span>")
		return

	var/target_x = x + x_offset
	var/target_y = y + y_offset
	var/turf/target = locate(target_x, target_y, z)
	var/area/A = get_area(target)

	flick(icon_teleport, src)

	//Change the indicator's icon to show that we're teleporting
	if(sending)
		indicator_icon = "launchpad_launch"
	else
		indicator_icon = "launchpad_pull"
	update_indicator()

	playsound(get_turf(src), 'sound/weapons/flash.ogg', 25, TRUE)
	teleporting = TRUE


	sleep(teleport_speed)

	//Set the indicator icon back to normal
	indicator_icon = "launchpad_target"
	update_indicator()

	if(QDELETED(src) || !isAvailable())
		return

	teleporting = FALSE

	// use a lot of power
	use_power(1000)

	var/turf/source = target
	var/list/log_msg = list()
	log_msg += ": [key_name(user)] has teleported "

	if(sending)
		source = dest
		dest = target

	playsound(get_turf(src), 'sound/weapons/emitter2.ogg', 25, TRUE)
	var/first = TRUE
	for(var/atom/movable/ROI in source)
		if(ROI == src)
			continue
		if(!istype(ROI) || isdead(ROI) || iscameramob(ROI) || istype(ROI, /obj/effect/dummy/phased_mob))
			continue//don't teleport these
		var/on_chair = ""
		if(ROI.anchored)// if it's anchored, don't teleport
			if(isliving(ROI))
				var/mob/living/L = ROI
				if(L.buckled)
					// TP people on office chairs
					if(L.buckled.anchored)
						continue
					on_chair = " (on a chair)"
				else
					continue
			else
				continue
		if(!first)
			log_msg += ", "
		if(ismob(ROI))
			var/mob/T = ROI
			log_msg += "[key_name(T)][on_chair]"
		else
			log_msg += "[ROI.name]"
			if (istype(ROI, /obj/structure/closet))
				log_msg += " ("
				var/first_inner = TRUE
				for(var/atom/movable/Q as mob|obj in ROI)
					if(!first_inner)
						log_msg += ", "
					first_inner = FALSE
					if(ismob(Q))
						log_msg += "[key_name(Q)]"
					else
						log_msg += "[Q.name]"
				if(first_inner)
					log_msg += "empty"
				log_msg += ")"
		do_teleport(ROI, dest, no_effects = !first, channel = TELEPORT_CHANNEL_BLUESPACE)
		first = FALSE

	if (first)
		log_msg += "nothing"
	log_msg += " [sending ? "to" : "from"] [target_x], [target_y], [z] ([A ? A.name : "null area"])"
	investigate_log(log_msg.Join(), INVESTIGATE_TELESCI)
	updateDialog()

//Starts in the briefcase. Don't spawn this directly, or it will runtime when closing.
/obj/machinery/launchpad/briefcase
	name = "briefcase launchpad"
	desc = "A portable bluespace pad able to thrust matter through bluespace, teleporting it to or from nearby locations. Controlled via remote."
	icon_state = "blpad-idle"
	icon_teleport = "blpad-beam"
	anchored = FALSE
	use_power = FALSE
	idle_power_usage = 0
	active_power_usage = 0
	teleport_speed = 20
	range = 8
	stationary = FALSE
	var/closed = TRUE
	var/obj/item/storage/briefcase/launchpad/briefcase

/obj/machinery/launchpad/briefcase/Initialize(mapload, briefcase)
    . = ..()
    if(!briefcase)
        log_game("[src] has been spawned without a briefcase.")
        return INITIALIZE_HINT_QDEL
    src.briefcase = briefcase

/obj/machinery/launchpad/briefcase/Destroy()
	QDEL_NULL(briefcase)
	return ..()

/obj/machinery/launchpad/briefcase/isAvailable()
	if(closed)
		return FALSE
	return ..()

/obj/machinery/launchpad/briefcase/MouseDrop(over_object, src_location, over_location)
	. = ..()
	if(over_object == usr)
		if(!briefcase || !usr.can_hold_items())
			return
		if(!usr.canUseTopic(src, BE_CLOSE, ismonkey(usr)))
			return
		usr.visible_message("<span class='notice'>[usr] starts closing [src]...</span>", "<span class='notice'>You start closing [src]...</span>")
		if(do_after(usr, 30, target = usr))
			usr.put_in_hands(briefcase)
			moveToNullspace() //hides it from suitcase contents
			closed = TRUE
			update_indicator()

/obj/machinery/launchpad/briefcase/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/launchpad_remote))
		var/obj/item/launchpad_remote/L = I
		if(L.pad == src) //do not attempt to link when already linked
			return ..()
		L.pad = src
		to_chat(user, "<span class='notice'>You link [src] to [L].</span>")
	else
		return ..()

//Briefcase item that contains the launchpad.
/obj/item/storage/briefcase/launchpad
	var/obj/machinery/launchpad/briefcase/pad

/obj/item/storage/briefcase/launchpad/Initialize()
	pad = new(null, src) //spawns pad in nullspace to hide it from briefcase contents
	. = ..()

/obj/item/storage/briefcase/launchpad/Destroy()
	if(!QDELETED(pad))
		QDEL_NULL(pad)
	return ..()

/obj/item/storage/briefcase/launchpad/PopulateContents()
	new /obj/item/pen(src)
	new /obj/item/launchpad_remote(src, pad)

/obj/item/storage/briefcase/launchpad/attack_self(mob/user)
	if(!isturf(user.loc)) //no setting up in a locker
		return
	add_fingerprint(user)
	user.visible_message("<span class='notice'>[user] starts setting down [src]...", "You start setting up [pad]...</span>")
	if(do_after(user, 30, target = user))
		pad.forceMove(get_turf(src))
		pad.update_indicator()
		pad.closed = FALSE
		user.transferItemToLoc(src, pad, TRUE)
		SEND_SIGNAL(src, COMSIG_TRY_STORAGE_HIDE_ALL)

/obj/item/storage/briefcase/launchpad/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/launchpad_remote))
		var/obj/item/launchpad_remote/L = I
		if(L.pad == src.pad) //do not attempt to link when already linked
			return ..()
		L.pad = src.pad
		to_chat(user, "<span class='notice'>You link [pad] to [L].</span>")
	else
		return ..()

/obj/item/launchpad_remote
	name = "folder"
	desc = "A folder."
	icon = 'icons/obj/bureaucracy.dmi'
	icon_state = "folder"
	w_class = WEIGHT_CLASS_SMALL
	var/sending = TRUE
	var/obj/machinery/launchpad/briefcase/pad

/obj/item/launchpad_remote/Initialize(mapload, pad) //remote spawns linked to the briefcase pad
	. = ..()
	src.pad = pad

/obj/item/launchpad_remote/attack_self(mob/user)
	. = ..()
	ui_interact(user)
	to_chat(user, "<span class='notice'>[src] projects a display onto your retina.</span>")

/obj/item/launchpad_remote/ui_interact(mob/user, ui_key = "launchpad_remote", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "launchpad_remote", "Briefcase Launchpad Remote", 300, 240, master_ui, state) //width, height
		ui.set_style("syndicate")
		ui.open()

	ui.set_autoupdate(TRUE)

/obj/item/launchpad_remote/ui_data(mob/user)
	var/list/data = list()
	data["has_pad"] = pad ? TRUE : FALSE
	if(pad)
		data["pad_closed"] = pad.closed
	if(!pad || pad.closed)
		return data

	data["pad_name"] = pad.display_name
	data["range"] = pad.range
	data["x"] = pad.x_offset
	data["y"] = pad.y_offset
	return data

/obj/item/launchpad_remote/proc/teleport(mob/user, obj/machinery/launchpad/pad)
	if(QDELETED(pad))
		to_chat(user, "<span class='warning'>ERROR: Launchpad not responding. Check launchpad integrity.</span>")
		return
	if(!pad.isAvailable())
		to_chat(user, "<span class='warning'>ERROR: Launchpad not operative. Make sure the launchpad is ready and powered.</span>")
		return
	pad.doteleport(user, sending)

/obj/item/launchpad_remote/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("set_pos")
			var/new_x = text2num(params["x"])
			var/new_y = text2num(params["y"])
			pad.set_offset(new_x, new_y)
			. = TRUE
		if("move_pos")
			var/plus_x = text2num(params["x"])
			var/plus_y = text2num(params["y"])
			pad.set_offset(
				x = pad.x_offset + plus_x,
				y = pad.y_offset + plus_y
			)
			. = TRUE
		if("rename")
			. = TRUE
			var/new_name = params["name"]
			if(!new_name)
				return
			pad.display_name = new_name
		if("remove")
			. = TRUE
			if(usr && alert(usr, "Are you sure?", "Unlink Launchpad", "I'm Sure", "Abort") != "Abort")
				pad = null
		if("launch")
			sending = TRUE
			teleport(usr, pad)
			. = TRUE
		if("pull")
			sending = FALSE
			teleport(usr, pad)
			. = TRUE
