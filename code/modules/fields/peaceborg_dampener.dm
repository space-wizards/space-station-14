
//Projectile dampening field that slows projectiles and lowers their damage for an energy cost deducted every 1/5 second.
//Only use square radius for this!
/datum/proximity_monitor/advanced/peaceborg_dampener
	name = "\improper Hyperkinetic Dampener Field"
	setup_edge_turfs = TRUE
	setup_field_turfs = TRUE
	requires_processing = TRUE
	field_shape = FIELD_SHAPE_RADIUS_SQUARE
	var/static/image/edgeturf_south = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_south")
	var/static/image/edgeturf_north = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_north")
	var/static/image/edgeturf_west = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_west")
	var/static/image/edgeturf_east = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_east")
	var/static/image/northwest_corner = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_northwest")
	var/static/image/southwest_corner = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_southwest")
	var/static/image/northeast_corner = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_northeast")
	var/static/image/southeast_corner = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_southeast")
	var/static/image/generic_edge = image('icons/effects/fields.dmi', icon_state = "projectile_dampen_generic")
	var/obj/item/borg/projectile_dampen/projector = null
	var/list/obj/projectile/tracked
	var/list/obj/projectile/staging
	use_host_turf = TRUE

/datum/proximity_monitor/advanced/peaceborg_dampener/New()
	tracked = list()
	staging = list()
	..()

/datum/proximity_monitor/advanced/peaceborg_dampener/Destroy()
	return ..()

/datum/proximity_monitor/advanced/peaceborg_dampener/process()
	if(!istype(projector))
		qdel(src)
	var/list/ranged = list()
	for(var/obj/projectile/P in range(current_range, get_turf(host)))
		ranged += P
	for(var/obj/projectile/P in tracked)
		if(!(P in ranged) || !P.loc)
			release_projectile(P)
	for(var/mob/living/silicon/robot/R in range(current_range, get_turf(host)))
		if(R.has_buckled_mobs())
			for(var/mob/living/L in R.buckled_mobs)
				L.visible_message("<span class='warning'>[L] is knocked off of [R] by the charge in [R]'s chassis induced by [name]!</span>")	//I know it's bad.
				L.Paralyze(10)
				R.unbuckle_mob(L)
				do_sparks(5, 0, L)
	..()

/datum/proximity_monitor/advanced/peaceborg_dampener/setup_edge_turf(turf/T)
	..()
	var/image/I = get_edgeturf_overlay(get_edgeturf_direction(T))
	var/obj/effect/abstract/proximity_checker/advanced/F = edge_turfs[T]
	F.appearance = I.appearance
	F.invisibility = 0
	F.mouse_opacity = MOUSE_OPACITY_TRANSPARENT
	F.layer = 5

/datum/proximity_monitor/advanced/peaceborg_dampener/cleanup_edge_turf(turf/T)
	..()

/datum/proximity_monitor/advanced/peaceborg_dampener/proc/get_edgeturf_overlay(direction)
	switch(direction)
		if(NORTH)
			return edgeturf_north
		if(SOUTH)
			return edgeturf_south
		if(EAST)
			return edgeturf_east
		if(WEST)
			return edgeturf_west
		if(NORTHEAST)
			return northeast_corner
		if(NORTHWEST)
			return northwest_corner
		if(SOUTHEAST)
			return southeast_corner
		if(SOUTHWEST)
			return southwest_corner
		else
			return generic_edge

/datum/proximity_monitor/advanced/peaceborg_dampener/proc/capture_projectile(obj/projectile/P, track_projectile = TRUE)
	if(P in tracked)
		return
	projector.dampen_projectile(P, track_projectile)
	if(track_projectile)
		tracked += P

/datum/proximity_monitor/advanced/peaceborg_dampener/proc/release_projectile(obj/projectile/P)
	projector.restore_projectile(P)
	tracked -= P

/datum/proximity_monitor/advanced/peaceborg_dampener/field_edge_uncrossed(atom/movable/AM, obj/effect/abstract/proximity_checker/advanced/field_edge/F)
	if(!is_turf_in_field(get_turf(AM), src))
		if(istype(AM, /obj/projectile))
			if(AM in tracked)
				release_projectile(AM)
			else
				capture_projectile(AM, FALSE)
	return ..()

/datum/proximity_monitor/advanced/peaceborg_dampener/field_edge_crossed(atom/movable/AM, obj/effect/abstract/proximity_checker/advanced/field_edge/F)
	if(istype(AM, /obj/projectile) && !(AM in tracked) && staging[AM] && !is_turf_in_field(staging[AM], src))
		capture_projectile(AM)
	staging -= AM
	return ..()

/datum/proximity_monitor/advanced/peaceborg_dampener/field_edge_canpass(atom/movable/AM, obj/effect/abstract/proximity_checker/advanced/field_edge/F, turf/entering)
	if(istype(AM, /obj/projectile))
		staging[AM] = get_turf(AM)
	. = ..()
	if(!.)
		staging -= AM	//This one ain't goin' through.
