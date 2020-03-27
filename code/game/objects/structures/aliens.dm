/* Alien shit!
 * Contains:
 *		structure/alien
 *		Resin
 *		Weeds
 *		Egg
 */


/obj/structure/alien
	icon = 'icons/mob/alien.dmi'
	max_integrity = 100

/obj/structure/alien/run_obj_armor(damage_amount, damage_type, damage_flag = 0, attack_dir)
	if(damage_flag == "melee")
		switch(damage_type)
			if(BRUTE)
				damage_amount *= 0.25
			if(BURN)
				damage_amount *= 2
	. = ..()

/obj/structure/alien/play_attack_sound(damage_amount, damage_type = BRUTE, damage_flag = 0)
	switch(damage_type)
		if(BRUTE)
			if(damage_amount)
				playsound(loc, 'sound/effects/attackblob.ogg', 100, TRUE)
			else
				playsound(src, 'sound/weapons/tap.ogg', 50, TRUE)
		if(BURN)
			if(damage_amount)
				playsound(loc, 'sound/items/welder.ogg', 100, TRUE)

/*
 * Generic alien stuff, not related to the purple lizards but still alien-like
 */

/obj/structure/alien/gelpod
	name = "gelatinous mound"
	desc = "A mound of jelly-like substance encasing something inside."
	icon = 'icons/obj/fluff.dmi'
	icon_state = "gelmound"

/obj/structure/alien/gelpod/deconstruct(disassembled = TRUE)
	if(!(flags_1 & NODECONSTRUCT_1))
		new/obj/effect/mob_spawn/human/corpse/damaged(get_turf(src))
	qdel(src)

/*
 * Resin
 */
/obj/structure/alien/resin
	name = "resin"
	desc = "Looks like some kind of thick resin."
	icon = 'icons/obj/smooth_structures/alien/resin_wall.dmi'
	icon_state = "smooth"
	density = TRUE
	opacity = 1
	anchored = TRUE
	canSmoothWith = list(/obj/structure/alien/resin)
	max_integrity = 200
	smooth = SMOOTH_TRUE
	var/resintype = null
	CanAtmosPass = ATMOS_PASS_DENSITY


/obj/structure/alien/resin/Initialize(mapload)
	. = ..()
	air_update_turf(TRUE)

/obj/structure/alien/resin/Move()
	var/turf/T = loc
	. = ..()
	move_update_air(T)

/obj/structure/alien/resin/wall
	name = "resin wall"
	desc = "Thick resin solidified into a wall."
	icon = 'icons/obj/smooth_structures/alien/resin_wall.dmi'
	icon_state = "smooth"	//same as resin, but consistency ho!
	resintype = "wall"
	canSmoothWith = list(/obj/structure/alien/resin/wall, /obj/structure/alien/resin/membrane)

/obj/structure/alien/resin/wall/BlockSuperconductivity()
	return 1

/obj/structure/alien/resin/membrane
	name = "resin membrane"
	desc = "Resin just thin enough to let light pass through."
	icon = 'icons/obj/smooth_structures/alien/resin_membrane.dmi'
	icon_state = "smooth"
	opacity = 0
	max_integrity = 160
	resintype = "membrane"
	canSmoothWith = list(/obj/structure/alien/resin/wall, /obj/structure/alien/resin/membrane)

/obj/structure/alien/resin/attack_paw(mob/user)
	return attack_hand(user)

/*
 * Weeds
 */

#define NODERANGE 3

/obj/structure/alien/weeds
	gender = PLURAL
	name = "resin floor"
	desc = "A thick resin surface covers the floor."
	anchored = TRUE
	density = FALSE
	layer = TURF_LAYER
	plane = FLOOR_PLANE
	icon_state = "weeds"
	max_integrity = 15
	canSmoothWith = list(/obj/structure/alien/weeds, /turf/closed/wall)
	smooth = SMOOTH_MORE
	var/last_expand = 0 //last world.time this weed expanded
	var/growth_cooldown_low = 150
	var/growth_cooldown_high = 200
	var/static/list/blacklisted_turfs

/obj/structure/alien/weeds/Initialize()
	pixel_x = -4
	pixel_y = -4 //so the sprites line up right in the map editor
	. = ..()

	if(!blacklisted_turfs)
		blacklisted_turfs = typecacheof(list(
			/turf/open/space,
			/turf/open/chasm,
			/turf/open/lava))


	last_expand = world.time + rand(growth_cooldown_low, growth_cooldown_high)
	if(icon == initial(icon))
		switch(rand(1,3))
			if(1)
				icon = 'icons/obj/smooth_structures/alien/weeds1.dmi'
			if(2)
				icon = 'icons/obj/smooth_structures/alien/weeds2.dmi'
			if(3)
				icon = 'icons/obj/smooth_structures/alien/weeds3.dmi'

/obj/structure/alien/weeds/proc/expand()
	var/turf/U = get_turf(src)
	if(is_type_in_typecache(U, blacklisted_turfs))
		qdel(src)
		return FALSE

	for(var/turf/T in U.GetAtmosAdjacentTurfs())
		if(locate(/obj/structure/alien/weeds) in T)
			continue

		if(is_type_in_typecache(T, blacklisted_turfs))
			continue

		new /obj/structure/alien/weeds(T)
	return TRUE

/obj/structure/alien/weeds/temperature_expose(datum/gas_mixture/air, exposed_temperature, exposed_volume)
	if(exposed_temperature > 300)
		take_damage(5, BURN, 0, 0)

//Weed nodes
/obj/structure/alien/weeds/node
	name = "glowing resin"
	desc = "Blue bioluminescence shines from beneath the surface."
	icon_state = "weednode"
	light_color = LIGHT_COLOR_BLUE
	light_power = 0.5
	var/lon_range = 4
	var/node_range = NODERANGE

/obj/structure/alien/weeds/node/Initialize()
	icon = 'icons/obj/smooth_structures/alien/weednode.dmi'
	. = ..()
	set_light(lon_range)
	var/obj/structure/alien/weeds/W = locate(/obj/structure/alien/weeds) in loc
	if(W && W != src)
		qdel(W)
	START_PROCESSING(SSobj, src)

/obj/structure/alien/weeds/node/Destroy()
	STOP_PROCESSING(SSobj, src)
	return ..()

/obj/structure/alien/weeds/node/process()
	for(var/obj/structure/alien/weeds/W in range(node_range, src))
		if(W.last_expand <= world.time)
			if(W.expand())
				W.last_expand = world.time + rand(growth_cooldown_low, growth_cooldown_high)

#undef NODERANGE


/*
 * Egg
 */

//for the status var
#define BURST "burst"
#define GROWING "growing"
#define GROWN "grown"
#define MIN_GROWTH_TIME 900	//time it takes to grow a hugger
#define MAX_GROWTH_TIME 1500

/obj/structure/alien/egg
	name = "egg"
	desc = "A large mottled egg."
	var/base_icon = "egg"
	icon_state = "egg_growing"
	density = FALSE
	anchored = TRUE
	max_integrity = 100
	integrity_failure = 0.05
	var/status = GROWING	//can be GROWING, GROWN or BURST; all mutually exclusive
	layer = MOB_LAYER
	var/obj/item/clothing/mask/facehugger/child

/obj/structure/alien/egg/Initialize(mapload)
	. = ..()
	update_icon()
	if(status == GROWING || status == GROWN)
		child = new(src)
	if(status == GROWING)
		addtimer(CALLBACK(src, .proc/Grow), rand(MIN_GROWTH_TIME, MAX_GROWTH_TIME))
	proximity_monitor = new(src, status == GROWN ? 1 : 0)
	if(status == BURST)
		obj_integrity = integrity_failure * max_integrity

/obj/structure/alien/egg/update_icon_state()
	switch(status)
		if(GROWING)
			icon_state = "[base_icon]_growing"
		if(GROWN)
			icon_state = "[base_icon]"
		if(BURST)
			icon_state = "[base_icon]_hatched"

/obj/structure/alien/egg/attack_paw(mob/living/user)
	return attack_hand(user)

/obj/structure/alien/egg/attack_alien(mob/living/carbon/alien/user)
	return attack_hand(user)

/obj/structure/alien/egg/attack_hand(mob/living/user)
	. = ..()
	if(.)
		return
	if(user.getorgan(/obj/item/organ/alien/plasmavessel))
		switch(status)
			if(BURST)
				to_chat(user, "<span class='notice'>You clear the hatched egg.</span>")
				playsound(loc, 'sound/effects/attackblob.ogg', 100, TRUE)
				qdel(src)
				return
			if(GROWING)
				to_chat(user, "<span class='notice'>The child is not developed yet.</span>")
				return
			if(GROWN)
				to_chat(user, "<span class='notice'>You retrieve the child.</span>")
				Burst(kill=FALSE)
				return
	else
		to_chat(user, "<span class='notice'>It feels slimy.</span>")
		user.changeNext_move(CLICK_CD_MELEE)


/obj/structure/alien/egg/proc/Grow()
	status = GROWN
	update_icon()
	proximity_monitor.SetRange(1)

//drops and kills the hugger if any is remaining
/obj/structure/alien/egg/proc/Burst(kill = TRUE)
	if(status == GROWN || status == GROWING)
		proximity_monitor.SetRange(0)
		status = BURST
		update_icon()
		flick("egg_opening", src)
		addtimer(CALLBACK(src, .proc/finish_bursting, kill), 15)

/obj/structure/alien/egg/proc/finish_bursting(kill = TRUE)
	if(child)
		child.forceMove(get_turf(src))
		// TECHNICALLY you could put non-facehuggers in the child var
		if(istype(child))
			if(kill)
				child.Die()
			else
				for(var/mob/M in range(1,src))
					if(CanHug(M))
						child.Leap(M)
						break

/obj/structure/alien/egg/obj_break(damage_flag)
	if(!(flags_1 & NODECONSTRUCT_1))
		if(status != BURST)
			Burst(kill=TRUE)

/obj/structure/alien/egg/temperature_expose(datum/gas_mixture/air, exposed_temperature, exposed_volume)
	if(exposed_temperature > 500)
		take_damage(5, BURN, 0, 0)


/obj/structure/alien/egg/HasProximity(atom/movable/AM)
	if(status == GROWN)
		if(!CanHug(AM))
			return

		var/mob/living/carbon/C = AM
		if(C.stat == CONSCIOUS && C.getorgan(/obj/item/organ/body_egg/alien_embryo))
			return

		Burst(kill=FALSE)

/obj/structure/alien/egg/grown
	status = GROWN
	icon_state = "egg"

/obj/structure/alien/egg/burst
	status = BURST
	icon_state = "egg_hatched"

#undef BURST
#undef GROWING
#undef GROWN
#undef MIN_GROWTH_TIME
#undef MAX_GROWTH_TIME
