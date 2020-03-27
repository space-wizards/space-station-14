//Stack-only storage.
/datum/component/storage/concrete/stack
	display_numerical_stacking = TRUE
	var/max_combined_stack_amount = 300
	max_w_class = WEIGHT_CLASS_NORMAL
	max_combined_w_class = WEIGHT_CLASS_NORMAL * 14

/datum/component/storage/concrete/stack/proc/total_stack_amount()
	. = 0
	var/atom/real_location = real_location()
	for(var/i in real_location)
		var/obj/item/stack/S = i
		if(!istype(S))
			continue
		. += S.amount

/datum/component/storage/concrete/stack/proc/remaining_space()
	return max(0, max_combined_stack_amount - total_stack_amount())

//emptying procs do not need modification as stacks automatically merge.

/datum/component/storage/concrete/stack/_insert_physical_item(obj/item/I, override = FALSE)
	if(!istype(I, /obj/item/stack))
		if(override)
			return ..()
		return FALSE
	var/atom/real_location = real_location()
	var/obj/item/stack/S = I
	var/can_insert = min(S.amount, remaining_space())
	if(!can_insert)
		return FALSE
	for(var/i in real_location)				//combine.
		if(QDELETED(I))
			return
		var/obj/item/stack/_S = i
		if(!istype(_S))
			continue
		if(_S.merge_type == S.merge_type)
			_S.add(can_insert)
			S.use(can_insert, TRUE)
			return TRUE
	return ..(S.change_stack(null, can_insert), override)

/datum/component/storage/concrete/stack/remove_from_storage(obj/item/I, atom/new_location)
	var/atom/real_location = real_location()
	var/obj/item/stack/S = I
	if(!istype(S))
		return ..()
	if(S.amount > S.max_amount)
		var/overrun = S.amount - S.max_amount
		S.amount = S.max_amount
		var/obj/item/stack/temp = new S.type(real_location, overrun)
		handle_item_insertion(temp)
	return ..(S, new_location)

/datum/component/storage/concrete/stack/_process_numerical_display()
	var/atom/real_location = real_location()
	. = list()
	for(var/i in real_location)
		var/obj/item/stack/I = i
		if(!istype(I) || QDELETED(I))				//We're specialized stack storage, just ignore non stacks.
			continue
		if(!.[I.merge_type])
			.[I.merge_type] = new /datum/numbered_display(I, I.amount)
		else
			var/datum/numbered_display/ND = .[I.merge_type]
			ND.number += I.amount
