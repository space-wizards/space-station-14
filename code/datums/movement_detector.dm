/// A datum to handle the busywork of registering signals to handle in depth tracking of a movable
/datum/movement_detector
	var/atom/movable/tracked
	var/datum/callback/listener

/datum/movement_detector/New(atom/movable/target, datum/callback/listener)
	if(target)
		track(target, listener)

/datum/movement_detector/Destroy()
	untrack()
	tracked = null
	listener = null
	return ..()

/// Sets up tracking of the given movable atom
/datum/movement_detector/proc/track(atom/movable/target, datum/callback/listener)
	untrack()
	tracked = target
	src.listener = listener
	
	while(ismovableatom(target))
		RegisterSignal(target, COMSIG_MOVABLE_MOVED, .proc/move_react)
		target = target.loc

/// Stops tracking
/datum/movement_detector/proc/untrack()
	if(!tracked)
		return
	var/atom/movable/target = tracked
	while(ismovableatom(target))
		UnregisterSignal(target, COMSIG_MOVABLE_MOVED)
		target = target.loc

/**
  * Reacts to any movement that would cause a change in coordinates of the tracked movable atom
  * This works by detecting movement of either the tracked object, or anything it is inside, recursively
  */
/datum/movement_detector/proc/move_react(atom/movable/mover, atom/oldloc, direction)
	var/turf/newturf = get_turf(tracked)
	
	if(oldloc && !isturf(oldloc))
		var/atom/target = oldloc
		while(ismovableatom(target))
			UnregisterSignal(target, COMSIG_MOVABLE_MOVED)
			target = target.loc
	if(tracked.loc != newturf)
		var/atom/target = mover.loc
		while(ismovableatom(target))
			RegisterSignal(target, COMSIG_MOVABLE_MOVED, .proc/move_react, TRUE)
			target = target.loc

	listener.Invoke(tracked, mover, oldloc, direction)
