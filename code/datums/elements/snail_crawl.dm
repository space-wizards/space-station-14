/datum/element/snailcrawl
	element_flags = ELEMENT_DETACH

/datum/element/snailcrawl/Attach(datum/target)
	. = ..()
	if(!ismovableatom(target))
		return ELEMENT_INCOMPATIBLE
	var/P
	if(iscarbon(target))
		P = .proc/snail_crawl
	else
		P = .proc/lubricate
	RegisterSignal(target, COMSIG_MOVABLE_MOVED, P)

/datum/element/snailcrawl/Detach(mob/living/carbon/target)
	. = ..()
	UnregisterSignal(target, COMSIG_MOVABLE_MOVED)
	if(istype(target))
		target.remove_movespeed_modifier(MOVESPEED_ID_SNAIL_CRAWL)

/datum/element/snailcrawl/proc/snail_crawl(mob/living/carbon/snail)
	if(snail.resting && !snail.buckled && lubricate(snail))
		snail.add_movespeed_modifier(MOVESPEED_ID_SNAIL_CRAWL, update=TRUE, priority=100, multiplicative_slowdown=-7, movetypes=GROUND)
	else
		snail.remove_movespeed_modifier(MOVESPEED_ID_SNAIL_CRAWL)

/datum/element/snailcrawl/proc/lubricate(atom/movable/snail)
	var/turf/open/OT = get_turf(snail)
	if(istype(OT))
		OT.MakeSlippery(TURF_WET_LUBE, 20)
		return TRUE
