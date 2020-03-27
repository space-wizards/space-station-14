/datum/component/plumbing/acclimator
	demand_connects = WEST
	supply_connects = EAST
	var/obj/machinery/plumbing/acclimator/AC

/datum/component/plumbing/acclimator/Initialize(start=TRUE, _turn_connects=TRUE)
	. = ..()
	if(!istype(parent, /obj/machinery/plumbing/acclimator))
		return COMPONENT_INCOMPATIBLE
	AC = parent

/datum/component/plumbing/acclimator/can_give(amount, reagent)
	. = ..()
	if(. && AC.emptying)
		return TRUE
	return FALSE
///We're overriding process and not send_request, because all process does is do the requests, so we might aswell cut out the middle man and save some code from running
/datum/component/plumbing/acclimator/process()
	if(AC.emptying)
		return 
	. = ..()
