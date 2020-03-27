/obj/vehicle
	name = "generic vehicle"
	desc = "Yell at coderbus."
	icon = 'icons/obj/vehicles.dmi'
	icon_state = "fuckyou"
	max_integrity = 300
	armor = list("melee" = 30, "bullet" = 30, "laser" = 30, "energy" = 0, "bomb" = 30, "bio" = 0, "rad" = 0, "fire" = 60, "acid" = 60)
	density = TRUE
	anchored = FALSE
	var/list/mob/occupants				//mob = bitflags of their control level.
	var/max_occupants = 1
	var/max_drivers = 1
	var/movedelay = 2
	var/lastmove = 0
	var/key_type
	var/obj/item/key/inserted_key
	var/key_type_exact = TRUE		//can subtypes work
	var/canmove = TRUE //If this is false the vehicle cant drive. (thanks for making this actually functional kevinz :^) )
	var/emulate_door_bumps = TRUE	//when bumping a door try to make occupants bump them to open them.
	var/default_driver_move = TRUE	//handle driver movement instead of letting something else do it like riding datums.
	var/list/autogrant_actions_passenger	//plain list of typepaths
	var/list/autogrant_actions_controller	//assoc list "[bitflag]" = list(typepaths)
	var/list/mob/occupant_actions			//assoc list mob = list(type = action datum assigned to mob)
	var/obj/vehicle/trailer
	var/are_legs_exposed = FALSE

/obj/vehicle/Initialize(mapload)
	. = ..()
	occupants = list()
	autogrant_actions_passenger = list()
	autogrant_actions_controller = list()
	occupant_actions = list()
	generate_actions()

/obj/vehicle/examine(mob/user)
	. = ..()
	if(resistance_flags & ON_FIRE)
		. += "<span class='warning'>It's on fire!</span>"
	var/healthpercent = obj_integrity/max_integrity * 100
	switch(healthpercent)
		if(50 to 99)
			. += "It looks slightly damaged."
		if(25 to 50)
			. += "It appears heavily damaged."
		if(0 to 25)
			. += "<span class='warning'>It's falling apart!</span>"

/obj/vehicle/proc/is_key(obj/item/I)
	return I? (key_type_exact? (I.type == key_type) : istype(I, key_type)) : FALSE

/obj/vehicle/proc/return_occupants()
	return occupants

/obj/vehicle/proc/occupant_amount()
	return length(occupants)

/obj/vehicle/proc/return_amount_of_controllers_with_flag(flag)
	. = 0
	for(var/i in occupants)
		if(occupants[i] & flag)
			.++

/obj/vehicle/proc/return_controllers_with_flag(flag)
	RETURN_TYPE(/list/mob)
	. = list()
	for(var/i in occupants)
		if(occupants[i] & flag)
			. += i

/obj/vehicle/proc/return_drivers()
	return return_controllers_with_flag(VEHICLE_CONTROL_DRIVE)

/obj/vehicle/proc/driver_amount()
	return return_amount_of_controllers_with_flag(VEHICLE_CONTROL_DRIVE)

/obj/vehicle/proc/is_driver(mob/M)
	return is_occupant(M) && occupants[M] & VEHICLE_CONTROL_DRIVE

/obj/vehicle/proc/is_occupant(mob/M)
	return !isnull(occupants[M])

/obj/vehicle/proc/add_occupant(mob/M, control_flags)
	if(!istype(M) || occupants[M])
		return FALSE
	occupants[M] = NONE
	add_control_flags(M, control_flags)
	after_add_occupant(M)
	grant_passenger_actions(M)
	return TRUE

/obj/vehicle/proc/after_add_occupant(mob/M)
	auto_assign_occupant_flags(M)

/obj/vehicle/proc/auto_assign_occupant_flags(mob/M)	//override for each type that needs it. Default is assign driver if drivers is not at max.
	if(driver_amount() < max_drivers)
		add_control_flags(M, VEHICLE_CONTROL_DRIVE|VEHICLE_CONTROL_PERMISSION)

/obj/vehicle/proc/remove_occupant(mob/M)
	if(!istype(M))
		return FALSE
	remove_control_flags(M, ALL)
	remove_passenger_actions(M)
	occupants -= M
	cleanup_actions_for_mob(M)
	after_remove_occupant(M)
	return TRUE

/obj/vehicle/proc/after_remove_occupant(mob/M)

/obj/vehicle/relaymove(mob/user, direction)
	if(is_driver(user))
		return driver_move(user, direction)
	return FALSE

/obj/vehicle/proc/driver_move(mob/user, direction)
	if(key_type && !is_key(inserted_key))
		to_chat(user, "<span class='warning'>[src] has no key inserted!</span>")
		return FALSE
	if(!default_driver_move)
		return
	if(!canmove)
		return
	vehicle_move(direction)
	return TRUE

/obj/vehicle/proc/vehicle_move(direction)
	if(lastmove + movedelay > world.time)
		return FALSE
	lastmove = world.time
	if(trailer)
		var/dir_to_move = get_dir(trailer.loc, loc)
		var/did_move = step(src, direction)
		if(did_move)
			step(trailer, dir_to_move)
		return did_move
	else
		after_move(direction)
		return step(src, direction)

/obj/vehicle/proc/after_move(direction)
	return

/obj/vehicle/proc/add_control_flags(mob/controller, flags)
	if(!istype(controller) || !flags)
		return FALSE
	occupants[controller] |= flags
	for(var/i in GLOB.bitflags)
		if(flags & i)
			grant_controller_actions_by_flag(controller, i)
	return TRUE

/obj/vehicle/proc/remove_control_flags(mob/controller, flags)
	if(!istype(controller) || !flags)
		return FALSE
	occupants[controller] &= ~flags
	for(var/i in GLOB.bitflags)
		if(flags & i)
			remove_controller_actions_by_flag(controller, i)
	return TRUE

/obj/vehicle/Bump(atom/movable/M)
	. = ..()
	if(emulate_door_bumps)
		if(istype(M, /obj/machinery/door))
			for(var/m in occupants)
				M.Bumped(m)

/obj/vehicle/Move(newloc, dir)
	. = ..()
	if(trailer && .)
		var/dir_to_move = get_dir(trailer.loc, newloc)
		step(trailer, dir_to_move)
