///The magical plumbing component used by the chemical filters. The different supply connects behave differently depending on the filters set on the chemical filter
/datum/component/plumbing/filter
	demand_connects = NORTH
	supply_connects = SOUTH | EAST | WEST //SOUTH is straight, EAST is left and WEST is right. We look from the perspective of the insert

/datum/component/plumbing/filter/Initialize()
	. = ..()
	if(!istype(parent, /obj/machinery/plumbing/filter))
		return COMPONENT_INCOMPATIBLE

/datum/component/plumbing/filter/can_give(amount, reagent, datum/ductnet/net)
	. = ..()
	if(.)
		var/direction
		for(var/A in ducts)
			if(ducts[A] == net)
				direction = get_original_direction(text2num(A)) //we need it relative to the direction, so filters don't change when we turn the filter
				break
		if(!direction)
			return FALSE
		if(reagent)
			if(!can_give_in_direction(direction, reagent))
				return FALSE

/datum/component/plumbing/filter/transfer_to(datum/component/plumbing/target, amount, reagent, datum/ductnet/net)
	if(!reagents || !target || !target.reagents)
		return FALSE
	var/direction
	for(var/A in ducts)
		if(ducts[A] == net)
			direction = get_original_direction(text2num(A))
			break
	if(reagent)
		reagents.trans_id_to(target.parent, reagent, amount)
	else
		for(var/A in reagents.reagent_list)
			var/datum/reagent/R = A
			if(!can_give_in_direction(direction, R.type))
				continue
			var/new_amount
			if(R.volume < amount)
				new_amount = amount - R.volume
			reagents.trans_id_to(target.parent, R.type, amount)
			amount = new_amount
			if(amount <= 0)
				break
///We check if the direction and reagent are valid to give. Needed for filters since different outputs have different behaviours
/datum/component/plumbing/filter/proc/can_give_in_direction(dir, reagent)
	var/obj/machinery/plumbing/filter/F = parent
	switch(dir)
		if(SOUTH) //straight
			if(!F.left.Find(reagent) && !F.right.Find(reagent))
				return TRUE
		if(WEST) //right
			if(F.right.Find(reagent))
				return TRUE
		if(EAST) //left
			if(F.left.Find(reagent))
				return TRUE
