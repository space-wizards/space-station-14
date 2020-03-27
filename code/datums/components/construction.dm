#define FORWARD 1
#define BACKWARD -1

#define ITEM_DELETE "delete"
#define ITEM_MOVE_INSIDE "move_inside"


/datum/component/construction
	var/list/steps
	var/result
	var/index = 1
	var/desc

/datum/component/construction/Initialize()
	if(!isatom(parent))
		return COMPONENT_INCOMPATIBLE

	RegisterSignal(parent, COMSIG_PARENT_EXAMINE, .proc/examine)
	RegisterSignal(parent, COMSIG_PARENT_ATTACKBY,.proc/action)
	update_parent(index)

/datum/component/construction/proc/examine(datum/source, mob/user, list/examine_list)
	if(desc)
		examine_list += desc

/datum/component/construction/proc/on_step()
	if(index > steps.len)
		spawn_result()
	else
		update_parent(index)

/datum/component/construction/proc/action(datum/source, obj/item/I, mob/living/user)
	return check_step(I, user)

/datum/component/construction/proc/update_index(diff)
	index += diff
	on_step()

/datum/component/construction/proc/check_step(obj/item/I, mob/living/user)
	var/diff = is_right_key(I)
	if(diff && custom_action(I, user, diff))
		update_index(diff)
		return TRUE
	return FALSE

/datum/component/construction/proc/is_right_key(obj/item/I) // returns index step
	var/list/L = steps[index]
	if(check_used_item(I, L["key"]))
		return FORWARD //to the first step -> forward
	else if(check_used_item(I, L["back_key"]))
		return BACKWARD //to the last step -> backwards
	return FALSE

/datum/component/construction/proc/check_used_item(obj/item/I, key)
	if(!key)
		return FALSE

	if(ispath(key) && istype(I, key))
		return TRUE

	else if(I.tool_behaviour == key)
		return TRUE

	return FALSE

/datum/component/construction/proc/custom_action(obj/item/I, mob/living/user, diff)
	var/target_index = index + diff
	var/list/current_step = steps[index]
	var/list/target_step

	if(target_index > 0 && target_index <= steps.len)
		target_step = steps[target_index]

	. = TRUE

	if(I.tool_behaviour)
		. = I.use_tool(parent, user, 0, volume=50)

	else if(diff == FORWARD)
		switch(current_step["action"])
			if(ITEM_DELETE)
				. = user.transferItemToLoc(I, parent)
				if(.)
					qdel(I)

			if(ITEM_MOVE_INSIDE)
				. = user.transferItemToLoc(I, parent)

			// Using stacks
			else if(istype(I, /obj/item/stack))
				. = I.use_tool(parent, user, 0, volume=50, amount=current_step["amount"])


	// Going backwards? Undo the last action. Drop/respawn the items used in last action, if any.
	if(. && diff == BACKWARD && target_step && !target_step["no_refund"])
		var/target_step_key = target_step["key"]

		switch(target_step["action"])
			if(ITEM_DELETE)
				new target_step_key(drop_location())

			if(ITEM_MOVE_INSIDE)
				var/obj/item/located_item = locate(target_step_key) in parent
				if(located_item)
					located_item.forceMove(drop_location())

			else if(ispath(target_step_key, /obj/item/stack))
				new target_step_key(drop_location(), target_step["amount"])

/datum/component/construction/proc/spawn_result()
	// Some constructions result in new components being added.
	if(ispath(result, /datum/component))
		parent.AddComponent(result)
		qdel(src)

	else if(ispath(result, /atom))
		new result(drop_location())
		qdel(parent)

/datum/component/construction/proc/update_parent(step_index)
	var/list/step = steps[step_index]
	var/atom/parent_atom = parent

	if(step["desc"])
		desc = step["desc"]

	if(step["icon_state"])
		parent_atom.icon_state = step["icon_state"]

/datum/component/construction/proc/drop_location()
	var/atom/parent_atom = parent
	return parent_atom.drop_location()



// Unordered construction.
// Takes a list of part types, to be added in any order, as steps.
// Calls spawn_result() when every type has been added.
/datum/component/construction/unordered/check_step(obj/item/I, mob/living/user)
	for(var/typepath in steps)
		if(istype(I, typepath) && custom_action(I, user, typepath))
			steps -= typepath
			on_step()
			return TRUE
	return FALSE

/datum/component/construction/unordered/on_step()
	if(!steps.len)
		spawn_result()
	else
		update_parent(steps.len)

/datum/component/construction/unordered/update_parent(steps_left)
	return

/datum/component/construction/unordered/custom_action(obj/item/I, mob/living/user, typepath)
	return TRUE
