/obj/effect/oneway
	name = "one way effect"
	desc = "Only lets things in from it's dir."
	icon = 'icons/effects/mapping_helpers.dmi'
	icon_state = "field_dir"
	invisibility = INVISIBILITY_MAXIMUM
	anchored = TRUE

/obj/effect/oneway/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	var/turf/T = get_turf(src)
	var/turf/MT = get_turf(mover)
	return . && (T == MT || get_dir(MT,T) == dir)


/obj/effect/wind
	name = "wind effect"
	desc = "Creates pressure effect in it's direction. Use sparingly."
	icon = 'icons/effects/mapping_helpers.dmi'
	icon_state = "field_dir"
	invisibility = INVISIBILITY_MAXIMUM
	var/strength = 30

/obj/effect/wind/Initialize()
	. = ..()
	START_PROCESSING(SSobj,src)

/obj/effect/wind/process()
	var/turf/open/T = get_turf(src)
	if(istype(T))
		T.consider_pressure_difference(get_step(T,dir),strength)

//Keep these rare due to cost of doing these checks
/obj/effect/path_blocker
	name = "magic barrier"
	desc = "You shall not pass."
	icon = 'icons/effects/mapping_helpers.dmi'
	icon_state = "blocker" //todo make this actually look fine when visible
	anchored = TRUE
	var/list/blocked_types = list()
	var/reverse = FALSE //Block if path not present

/obj/effect/path_blocker/Initialize()
	. = ..()
	if(blocked_types.len)
		blocked_types = typecacheof(blocked_types)

/obj/effect/path_blocker/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	if(blocked_types.len)
		var/list/mover_contents = mover.GetAllContents()
		for(var/atom/movable/thing in mover_contents)
			if(blocked_types[thing.type])
				return reverse
	return !reverse

/obj/structure/pitgrate
	name = "pit grate"
	icon = 'icons/obj/smooth_structures/lattice.dmi'
	icon_state = "lattice"
	plane = FLOOR_PLANE
	anchored = TRUE
	obj_flags = CAN_BE_HIT | BLOCK_Z_FALL
	var/id
	var/open = FALSE
	var/hidden = FALSE

/obj/structure/pitgrate/Initialize()
	. = ..()
	RegisterSignal(SSdcs,COMSIG_GLOB_BUTTON_PRESSED, .proc/OnButtonPressed)
	if(hidden)
		update_openspace()

/obj/structure/pitgrate/proc/OnButtonPressed(datum/source,obj/machinery/button/button)
	if(button.id == id) //No range checks because this is admin abuse mostly.
		toggle()

/obj/structure/pitgrate/proc/update_openspace()
	var/turf/open/openspace/T = get_turf(src)
	if(!istype(T))
		return
	//Simple way to keep plane conflicts away, could probably be upgraded to something less nuclear with 513
	T.invisibility = open ? 0 : INVISIBILITY_MAXIMUM

/obj/structure/pitgrate/proc/toggle()
	open = !open
	var/talpha
	if(open)
		talpha = 0
		obj_flags &= ~BLOCK_Z_FALL
	else
		talpha = 255
		obj_flags |= BLOCK_Z_FALL
	plane = BYOND_LIGHTING_LAYER //What matters it's one above openspace, so our animation is not dependant on what's there. Up to revision with 513
	animate(src,alpha = talpha,time = 10)
	addtimer(CALLBACK(src,.proc/reset_plane),10)
	if(hidden)
		update_openspace()
	var/turf/T = get_turf(src)
	for(var/atom/movable/AM in T)
		if(!AM.zfalling)
			T.zFall(AM)

/obj/structure/pitgrate/proc/reset_plane()
	plane = FLOOR_PLANE

/obj/structure/pitgrate/Destroy()
	if(hidden)
		open = TRUE
		update_openspace()
	. = ..()

/obj/structure/pitgrate/hidden
	name = "floor"
	icon = 'icons/turf/floors.dmi'
	icon_state = "floor"
	hidden = TRUE
