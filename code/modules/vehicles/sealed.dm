/obj/vehicle/sealed
	var/enter_delay = 20
	var/mouse_pointer

/obj/vehicle/sealed/generate_actions()
	. = ..()
	initialize_passenger_action_type(/datum/action/vehicle/sealed/climb_out)

/obj/vehicle/sealed/generate_action_type()
	var/datum/action/vehicle/sealed/E = ..()
	. = E
	if(istype(E))
		E.vehicle_entered_target = src

/obj/vehicle/sealed/MouseDrop_T(atom/dropping, mob/M)
	if(!istype(dropping) || !istype(M))
		return ..()
	if(M == dropping)
		mob_try_enter(M)
	return ..()

/obj/vehicle/sealed/Exited(atom/movable/AM, atom/newLoc)
	. = ..()
	if(ismob(AM))
		remove_occupant(AM)

/obj/vehicle/sealed/proc/mob_try_enter(mob/M)
	if(!istype(M))
		return FALSE
	if(occupant_amount() >= max_occupants)
		return FALSE
	if(do_after(M, get_enter_delay(M), FALSE, src, TRUE))
		mob_enter(M)
		return TRUE
	return FALSE

/obj/vehicle/sealed/proc/get_enter_delay(mob/M)
	return enter_delay

/obj/vehicle/sealed/proc/mob_enter(mob/M, silent = FALSE)
	if(!istype(M))
		return FALSE
	if(!silent)
		M.visible_message("<span class='notice'>[M] climbs into \the [src]!</span>")
	M.forceMove(src)
	add_occupant(M)
	return TRUE

/obj/vehicle/sealed/proc/mob_try_exit(mob/M, mob/user, silent = FALSE, randomstep = FALSE)
	mob_exit(M, silent, randomstep)

/obj/vehicle/sealed/proc/mob_exit(mob/M, silent = FALSE, randomstep = FALSE)
	if(!istype(M))
		return FALSE
	remove_occupant(M)
	M.forceMove(exit_location(M))
	if(randomstep)
		var/turf/target_turf = get_step(exit_location(M), pick(GLOB.cardinals))
		M.throw_at(target_turf, 5, 10)

	if(!silent)
		M.visible_message("<span class='notice'>[M] drops out of \the [src]!</span>")
	return TRUE

/obj/vehicle/sealed/proc/exit_location(M)
	return drop_location()

/obj/vehicle/sealed/attackby(obj/item/I, mob/user, params)
	if(key_type && !is_key(inserted_key) && is_key(I))
		if(user.transferItemToLoc(I, src))
			to_chat(user, "<span class='notice'>You insert [I] into [src].</span>")
			if(inserted_key)	//just in case there's an invalid key
				inserted_key.forceMove(drop_location())
			inserted_key = I
		else
			to_chat(user, "<span class='warning'>[I] seems to be stuck to your hand!</span>")
		return
	return ..()

/obj/vehicle/sealed/proc/remove_key(mob/user)
	if(!inserted_key)
		to_chat(user, "<span class='warning'>There is no key in [src]!</span>")
		return
	if(!is_occupant(user) || !(occupants[user] & VEHICLE_CONTROL_DRIVE))
		to_chat(user, "<span class='warning'>You must be driving [src] to remove [src]'s key!</span>")
		return
	to_chat(user, "<span class='notice'>You remove [inserted_key] from [src].</span>")
	inserted_key.forceMove(drop_location())
	user.put_in_hands(inserted_key)
	inserted_key = null

/obj/vehicle/sealed/Destroy()
	DumpMobs()
	explosion(loc, 0, 1, 2, 3, 0)
	return ..()

/obj/vehicle/sealed/proc/DumpMobs(randomstep = TRUE)
	for(var/i in occupants)
		mob_exit(i, null, randomstep)
		if(iscarbon(i))
			var/mob/living/carbon/Carbon = i
			Carbon.Paralyze(40)

/obj/vehicle/sealed/proc/DumpSpecificMobs(flag, randomstep = TRUE)
	for(var/i in occupants)
		if((occupants[i] & flag))
			mob_exit(i, null, randomstep)
			if(iscarbon(i))
				var/mob/living/carbon/C = i
				C.Paralyze(40)


/obj/vehicle/sealed/AllowDrop()
	return FALSE
