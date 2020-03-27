// virtual disposal object
// travels through pipes in lieu of actual items
// contents will be items flushed by the disposal
// this allows the gas flushed to be tracked

/obj/structure/disposalholder
	invisibility = INVISIBILITY_MAXIMUM
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF
	dir = NONE
	rad_flags = RAD_PROTECT_CONTENTS | RAD_NO_CONTAMINATE
	var/datum/gas_mixture/gas	// gas used to flush, will appear at exit point
	var/active = FALSE			// true if the holder is moving, otherwise inactive
	var/count = 1000			// can travel 1000 steps before going inactive (in case of loops)
	var/destinationTag = NONE	// changes if contains a delivery container
	var/tomail = FALSE			// contains wrapped package
	var/hasmob = FALSE			// contains a mob

/obj/structure/disposalholder/Destroy()
	QDEL_NULL(gas)
	active = FALSE
	return ..()

// initialize a holder from the contents of a disposal unit
/obj/structure/disposalholder/proc/init(obj/machinery/disposal/D)
	gas = D.air_contents// transfer gas resv. into holder object

	//Check for any living mobs trigger hasmob.
	//hasmob effects whether the package goes to cargo or its tagged destination.
	for(var/mob/living/M in D)
		if(M.client)
			M.reset_perspective(src)
		hasmob = TRUE

	//Checks 1 contents level deep. This means that players can be sent through disposals mail...
	//...but it should require a second person to open the package. (i.e. person inside a wrapped locker)
	for(var/obj/O in D)
		if(locate(/mob/living) in O)
			hasmob = TRUE
			break

	// now everything inside the disposal gets put into the holder
	// note AM since can contain mobs or objs
	for(var/A in D)
		var/atom/movable/AM = A
		if(AM == src)
			continue
		SEND_SIGNAL(AM, COMSIG_MOVABLE_DISPOSING, src, D)
		AM.forceMove(src)
		if(istype(AM, /obj/structure/bigDelivery) && !hasmob)
			var/obj/structure/bigDelivery/T = AM
			src.destinationTag = T.sortTag
		else if(istype(AM, /obj/item/smallDelivery) && !hasmob)
			var/obj/item/smallDelivery/T = AM
			src.destinationTag = T.sortTag
		else if(istype(AM, /mob/living/silicon/robot))
			var/obj/item/destTagger/borg/tagger = locate() in AM
			if (tagger)
				src.destinationTag = tagger.currTag


// start the movement process
// argument is the disposal unit the holder started in
/obj/structure/disposalholder/proc/start(obj/machinery/disposal/D)
	if(!D.trunk)
		D.expel(src)	// no trunk connected, so expel immediately
		return
	forceMove(D.trunk)
	active = TRUE
	setDir(DOWN)
	move()

// movement process, persists while holder is moving through pipes
/obj/structure/disposalholder/proc/move()
	set waitfor = FALSE
	var/obj/structure/disposalpipe/last
	while(active)
		var/obj/structure/disposalpipe/curr = loc
		last = curr
		curr = curr.transfer(src)
		if(!curr && active)
			last.expel(src, loc, dir)

		stoplag()
		if(!(count--))
			active = FALSE

// find the turf which should contain the next pipe
/obj/structure/disposalholder/proc/nextloc()
	return get_step(src, dir)

// find a matching pipe on a turf
/obj/structure/disposalholder/proc/findpipe(turf/T)
	if(!T)
		return null

	var/fdir = turn(dir, 180)	// flip the movement direction
	for(var/obj/structure/disposalpipe/P in T)
		if(fdir & P.dpdir)		// find pipe direction mask that matches flipped dir
			return P
	// if no matching pipe, return null
	return null

// merge two holder objects
// used when a holder meets a stuck holder
/obj/structure/disposalholder/proc/merge(obj/structure/disposalholder/other)
	for(var/A in other)
		var/atom/movable/AM = A
		AM.forceMove(src)		// move everything in other holder to this one
		if(ismob(AM))
			var/mob/M = AM
			M.reset_perspective(src)	// if a client mob, update eye to follow this holder
	qdel(other)


// called when player tries to move while in a pipe
/obj/structure/disposalholder/relaymove(mob/user)
	if(user.incapacitated())
		return
	for(var/mob/M in range(5, get_turf(src)))
		M.show_message("<FONT size=[max(0, 5 - get_dist(src, M))]>CLONG, clong!</FONT>", MSG_AUDIBLE)
	playsound(src.loc, 'sound/effects/clang.ogg', 50, FALSE, FALSE)

// called to vent all gas in holder to a location
/obj/structure/disposalholder/proc/vent_gas(turf/T)
	T.assume_air(gas)
	T.air_update_turf()

/obj/structure/disposalholder/AllowDrop()
	return TRUE

/obj/structure/disposalholder/ex_act(severity, target)
	return
