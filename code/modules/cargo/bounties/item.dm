/datum/bounty/item
	var/required_count = 1
	var/shipped_count = 0
	var/list/wanted_types  // Types accepted for the bounty.
	var/include_subtypes = TRUE     // Set to FALSE to make the datum apply only to a strict type.
	var/list/exclude_types // Types excluded.

/datum/bounty/item/New()
	..()
	wanted_types = typecacheof(wanted_types)
	exclude_types = typecacheof(exclude_types)

/datum/bounty/item/completion_string()
	return {"[shipped_count]/[required_count]"}

/datum/bounty/item/can_claim()
	return ..() && shipped_count >= required_count

/datum/bounty/item/applies_to(obj/O)
	if(!include_subtypes && !(O.type in wanted_types))
		return FALSE
	if(include_subtypes && (!is_type_in_typecache(O, wanted_types) || is_type_in_typecache(O, exclude_types)))
		return FALSE
	if(O.flags_1 & HOLOGRAM_1)
		return FALSE
	return shipped_count < required_count

/datum/bounty/item/ship(obj/O)
	if(!applies_to(O))
		return
	if(istype(O,/obj/item/stack))
		var/obj/item/stack/O_is_a_stack = O
		shipped_count += O_is_a_stack.amount
	else
		shipped_count += 1

/datum/bounty/item/compatible_with(datum/other_bounty)
	return type != other_bounty.type

