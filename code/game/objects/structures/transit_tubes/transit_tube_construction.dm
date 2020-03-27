// transit tube construction

// normal transit tubes
/obj/structure/c_transit_tube
	name = "unattached transit tube"
	icon = 'icons/obj/atmospherics/pipes/transit_tube.dmi'
	icon_state = "straight"
	desc = "An unattached segment of transit tube."
	density = FALSE
	layer = LOW_ITEM_LAYER //same as the built tube
	anchored = FALSE
	var/const/time_to_unwrench = 2 SECONDS
	var/flipped = 0
	var/build_type = /obj/structure/transit_tube
	var/flipped_build_type
	var/base_icon

/obj/structure/c_transit_tube/proc/can_wrench_in_loc(mob/user)
	var/turf/source_turf = get_turf(loc)
	var/existing_tubes = 0
	for(var/obj/structure/transit_tube/tube in source_turf)
		existing_tubes +=1
		if(existing_tubes >= 2)
			to_chat(user, "<span class='warning'>You cannot wrench any more transit tubes!</span> ")
			return FALSE
	return TRUE

/obj/structure/c_transit_tube/ComponentInitialize()
	. = ..()
	AddComponent(/datum/component/simple_rotation,ROTATION_ALTCLICK | ROTATION_CLOCKWISE | ROTATION_FLIP | ROTATION_VERBS,null,null,CALLBACK(src,.proc/after_rot))

/obj/structure/c_transit_tube/proc/after_rot(mob/user,rotation_type)
	if(flipped_build_type && rotation_type == ROTATION_FLIP)
		setDir(turn(dir,-180)) //Turn back we don't actually flip
		flipped = !flipped
		var/cur_flip = initial(flipped) ? !flipped : flipped
		if(cur_flip)
			build_type = flipped_build_type
		else
			build_type = initial(build_type)
		icon_state = "[base_icon][flipped]"

/obj/structure/c_transit_tube/wrench_act(mob/living/user, obj/item/I)
	..()
	if(!can_wrench_in_loc(user))
		return
	to_chat(user, "<span class='notice'>You start attaching the [name]...</span>")
	add_fingerprint(user)
	if(I.use_tool(src, user, time_to_unwrench, volume=50, extra_checks=CALLBACK(src, .proc/can_wrench_in_loc, user)))
		to_chat(user, "<span class='notice'>You attach the [name].</span>")
		var/obj/structure/transit_tube/R = new build_type(loc, dir)
		transfer_fingerprints_to(R)
		qdel(src)
	return TRUE

// transit tube station
/obj/structure/c_transit_tube/station
	name = "unattached through station"
	icon_state = "closed_station0"
	build_type = /obj/structure/transit_tube/station
	flipped_build_type = /obj/structure/transit_tube/station/flipped
	base_icon = "closed_station"

/obj/structure/c_transit_tube/station/flipped
	icon_state = "closed_station1"
	flipped = 1
	build_type = /obj/structure/transit_tube/station/flipped
	flipped_build_type = /obj/structure/transit_tube/station


// reverser station, used for the terminus
/obj/structure/c_transit_tube/station/reverse
	name = "unattached terminus station"
	icon_state = "closed_terminus0"
	build_type = /obj/structure/transit_tube/station/reverse
	flipped_build_type = /obj/structure/transit_tube/station/reverse/flipped
	base_icon = "closed_terminus"

/obj/structure/c_transit_tube/station/reverse/flipped
	icon_state = "closed_terminus1"
	flipped = 1
	build_type = /obj/structure/transit_tube/station/reverse/flipped
	flipped_build_type = /obj/structure/transit_tube/station/reverse

//all the dispenser stations

/obj/structure/c_transit_tube/station/dispenser
	icon_state = "closed_dispenser0"
	name = "unattached dispenser station"
	build_type = /obj/structure/transit_tube/station/dispenser
	flipped_build_type = /obj/structure/transit_tube/station/dispenser/flipped

/obj/structure/c_transit_tube/station/dispenser/flipped
	icon_state = "closed_station1"
	flipped = 1
	build_type = /obj/structure/transit_tube/station/dispenser/flipped
	flipped_build_type = /obj/structure/transit_tube/station/dispenser

//and the ones that reverse

/obj/structure/c_transit_tube/station/dispenser/reverse
	name = "unattached terminus dispenser station"
	icon_state = "closed_terminus0"
	build_type = /obj/structure/transit_tube/station/dispenser/reverse
	flipped_build_type = /obj/structure/transit_tube/station/dispenser/reverse/flipped
	base_icon = "closed_terminus"

/obj/structure/c_transit_tube/station/dispenser/reverse/flipped
	icon_state = "closed_terminus1"
	flipped = 1
	build_type = /obj/structure/transit_tube/station/dispenser/reverse/flipped
	flipped_build_type = /obj/structure/transit_tube/station/dispenser/reverse

//onto some special tube types

/obj/structure/c_transit_tube/crossing
	icon_state = "crossing"
	build_type = /obj/structure/transit_tube/crossing


/obj/structure/c_transit_tube/diagonal
	icon_state = "diagonal"
	build_type = /obj/structure/transit_tube/diagonal

/obj/structure/c_transit_tube/diagonal/crossing
	icon_state = "diagonal_crossing"
	build_type = /obj/structure/transit_tube/diagonal/crossing


/obj/structure/c_transit_tube/curved
	icon_state = "curved0"
	build_type = /obj/structure/transit_tube/curved
	flipped_build_type = /obj/structure/transit_tube/curved/flipped
	base_icon = "curved"

/obj/structure/c_transit_tube/curved/flipped
	icon_state = "curved1"
	build_type = /obj/structure/transit_tube/curved/flipped
	flipped_build_type = /obj/structure/transit_tube/curved
	flipped = 1


/obj/structure/c_transit_tube/junction
	icon_state = "junction0"
	build_type = /obj/structure/transit_tube/junction
	flipped_build_type = /obj/structure/transit_tube/junction/flipped
	base_icon = "junction"


/obj/structure/c_transit_tube/junction/flipped
	icon_state = "junction1"
	flipped = 1
	build_type = /obj/structure/transit_tube/junction/flipped
	flipped_build_type = /obj/structure/transit_tube/junction


//transit tube pod
//see station.dm for the logic
/obj/structure/c_transit_tube_pod
	name = "unattached transit tube pod"
	icon = 'icons/obj/atmospherics/pipes/transit_tube.dmi'
	icon_state = "pod"
	desc = "Could probably be <b>dragged</b> into an open Transit Tube."
	anchored = FALSE
	density = FALSE
