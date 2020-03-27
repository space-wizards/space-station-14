/datum/element/waddling

/datum/element/waddling/Attach(datum/target)
	. = ..()
	if(!ismovableatom(target))
		return ELEMENT_INCOMPATIBLE
	if(isliving(target))
		RegisterSignal(target, COMSIG_MOVABLE_MOVED, .proc/LivingWaddle)
	else
		RegisterSignal(target, COMSIG_MOVABLE_MOVED, .proc/Waddle)

/datum/element/waddling/Detach(datum/source, force)
	. = ..()
	UnregisterSignal(source, COMSIG_MOVABLE_MOVED)

/datum/element/waddling/proc/LivingWaddle(mob/living/target)
	if(target.incapacitated() || !(target.mobility_flags & MOBILITY_STAND))
		return
	Waddle(target)

/datum/element/waddling/proc/Waddle(atom/movable/target)
	animate(target, pixel_z = 4, time = 0)
	animate(pixel_z = 0, transform = turn(matrix(), pick(-12, 0, 12)), time=2)
	animate(pixel_z = 0, transform = matrix(), time = 0)
