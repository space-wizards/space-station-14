/datum/component/orbiter
	can_transfer = TRUE
	dupe_mode = COMPONENT_DUPE_UNIQUE_PASSARGS
	var/list/orbiters
	var/datum/movement_detector/tracker

//radius: range to orbit at, radius of the circle formed by orbiting (in pixels)
//clockwise: whether you orbit clockwise or anti clockwise
//rotation_speed: how fast to rotate (how many ds should it take for a rotation to complete)
//rotation_segments: the resolution of the orbit circle, less = a more block circle, this can be used to produce hexagons (6 segments) triangles (3 segments), and so on, 36 is the best default.
//pre_rotation: Chooses to rotate src 90 degress towards the orbit dir (clockwise/anticlockwise), useful for things to go "head first" like ghosts
/datum/component/orbiter/Initialize(atom/movable/orbiter, radius, clockwise, rotation_speed, rotation_segments, pre_rotation)
	if(!istype(orbiter) || !isatom(parent) || isarea(parent))
		return COMPONENT_INCOMPATIBLE

	orbiters = list()

	begin_orbit(orbiter, radius, clockwise, rotation_speed, rotation_segments, pre_rotation)

/datum/component/orbiter/RegisterWithParent()
	var/atom/target = parent

	target.orbiters = src
	if(ismovableatom(target))
		tracker = new(target, CALLBACK(src, .proc/move_react))

/datum/component/orbiter/UnregisterFromParent()
	var/atom/target = parent
	target.orbiters = null
	QDEL_NULL(tracker)

/datum/component/orbiter/Destroy()
	var/atom/master = parent
	master.orbiters = null
	for(var/i in orbiters)
		end_orbit(i)
	orbiters = null
	return ..()

/datum/component/orbiter/InheritComponent(datum/component/orbiter/newcomp, original, list/arguments)
	if(arguments)
		begin_orbit(arglist(arguments))
		return
	// The following only happens on component transfers
	orbiters += newcomp.orbiters

/datum/component/orbiter/PostTransfer()
	if(!isatom(parent) || isarea(parent) || !get_turf(parent))
		return COMPONENT_INCOMPATIBLE
	move_react(parent)

/datum/component/orbiter/proc/begin_orbit(atom/movable/orbiter, radius, clockwise, rotation_speed, rotation_segments, pre_rotation)
	if(orbiter.orbiting)
		if(orbiter.orbiting == src)
			orbiter.orbiting.end_orbit(orbiter, TRUE)
		else
			orbiter.orbiting.end_orbit(orbiter)
	orbiters[orbiter] = TRUE
	orbiter.orbiting = src
	RegisterSignal(orbiter, COMSIG_MOVABLE_MOVED, .proc/orbiter_move_react)
	SEND_SIGNAL(parent, COMSIG_ATOM_ORBIT_BEGIN, orbiter)
	var/matrix/initial_transform = matrix(orbiter.transform)

	// Head first!
	if(pre_rotation)
		var/matrix/M = matrix(orbiter.transform)
		var/pre_rot = 90
		if(!clockwise)
			pre_rot = -90
		M.Turn(pre_rot)
		orbiter.transform = M

	var/matrix/shift = matrix(orbiter.transform)
	shift.Translate(0, radius)
	orbiter.transform = shift

	orbiter.SpinAnimation(rotation_speed, -1, clockwise, rotation_segments, parallel = FALSE)

	//we stack the orbits up client side, so we can assign this back to normal server side without it breaking the orbit
	orbiter.transform = initial_transform
	orbiter.forceMove(get_turf(parent))
	to_chat(orbiter, "<span class='notice'>Now orbiting [parent].</span>")

/datum/component/orbiter/proc/end_orbit(atom/movable/orbiter, refreshing=FALSE)
	if(!orbiters[orbiter])
		return
	UnregisterSignal(orbiter, COMSIG_MOVABLE_MOVED)
	SEND_SIGNAL(parent, COMSIG_ATOM_ORBIT_STOP, orbiter)
	orbiter.SpinAnimation(0, 0)
	orbiters -= orbiter
	orbiter.stop_orbit(src)
	orbiter.orbiting = null
	if(!refreshing && !length(orbiters) && !QDELING(src))
		qdel(src)

// This proc can receive signals by either the thing being directly orbited or anything holding it
/datum/component/orbiter/proc/move_react(atom/movable/master, atom/mover, atom/oldloc, direction)
	set waitfor = FALSE // Transfer calls this directly and it doesnt care if the ghosts arent done moving

	if(master.loc == oldloc)
		return

	var/turf/newturf = get_turf(master)
	if(!newturf)
		qdel(src)

	var/atom/curloc = master.loc
	for(var/i in orbiters)
		var/atom/movable/thing = i
		if(QDELETED(thing) || thing.loc == newturf)
			continue
		thing.forceMove(newturf)
		if(CHECK_TICK && master.loc != curloc)
			// We moved again during the checktick, cancel current operation
			break


/datum/component/orbiter/proc/orbiter_move_react(atom/movable/orbiter, atom/oldloc, direction)
	if(orbiter.loc == get_turf(parent))
		return
	end_orbit(orbiter)

/////////////////////

/atom/movable/proc/orbit(atom/A, radius = 10, clockwise = FALSE, rotation_speed = 20, rotation_segments = 36, pre_rotation = TRUE)
	if(!istype(A) || !get_turf(A) || A == src)
		return

	return A.AddComponent(/datum/component/orbiter, src, radius, clockwise, rotation_speed, rotation_segments, pre_rotation)

/atom/movable/proc/stop_orbit(datum/component/orbiter/orbits)
	return // We're just a simple hook

/atom/proc/transfer_observers_to(atom/target)
	if(!orbiters || !istype(target) || !get_turf(target) || target == src)
		return
	target.TakeComponent(orbiters)
