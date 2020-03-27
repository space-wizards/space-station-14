
// External storage-related logic:
// /mob/proc/ClickOn() in /_onclick/click.dm - clicking items in storages
// /mob/living/Move() in /modules/mob/living/living.dm - hiding storage boxes on mob movement

/datum/component/storage/concrete
	can_transfer = TRUE
	var/drop_all_on_deconstruct = TRUE
	var/drop_all_on_destroy = FALSE
	var/transfer_contents_on_component_transfer = FALSE
	var/list/datum/component/storage/slaves = list()

	var/list/_contents_limbo // Where objects go to live mid transfer
	var/list/_user_limbo // The last users before the component started moving

/datum/component/storage/concrete/Initialize()
	. = ..()
	RegisterSignal(parent, COMSIG_ATOM_CONTENTS_DEL, .proc/on_contents_del)
	RegisterSignal(parent, COMSIG_OBJ_DECONSTRUCT, .proc/on_deconstruct)

/datum/component/storage/concrete/Destroy()
	var/atom/real_location = real_location()
	for(var/atom/_A in real_location)
		_A.mouse_opacity = initial(_A.mouse_opacity)
	if(drop_all_on_destroy)
		do_quick_empty()
	for(var/i in slaves)
		var/datum/component/storage/slave = i
		slave.change_master(null)
	QDEL_LIST(_contents_limbo)
	_user_limbo = null
	return ..()

/datum/component/storage/concrete/master()
	return src

/datum/component/storage/concrete/real_location()
	return parent

/datum/component/storage/concrete/PreTransfer()
	if(is_using)
		_user_limbo = is_using.Copy()
		close_all()
	if(transfer_contents_on_component_transfer)
		_contents_limbo = list()
		for(var/atom/movable/AM in parent)
			_contents_limbo += AM
			AM.moveToNullspace()

/datum/component/storage/concrete/PostTransfer()
	if(!isatom(parent))
		return COMPONENT_INCOMPATIBLE
	if(transfer_contents_on_component_transfer)
		for(var/i in _contents_limbo)
			var/atom/movable/AM = i
			AM.forceMove(parent)
		_contents_limbo = null
	if(_user_limbo)
		for(var/i in _user_limbo)
			show_to(i)
		_user_limbo = null

/datum/component/storage/concrete/_insert_physical_item(obj/item/I, override = FALSE)
	. = TRUE
	var/atom/real_location = real_location()
	if(I.loc != real_location)
		I.forceMove(real_location)
	refresh_mob_views()

/datum/component/storage/concrete/refresh_mob_views()
	. = ..()
	for(var/i in slaves)
		var/datum/component/storage/slave = i
		slave.refresh_mob_views()

/datum/component/storage/concrete/emp_act(datum/source, severity)
	if(emp_shielded)
		return
	var/atom/real_location = real_location()
	for(var/i in real_location)
		var/atom/A = i
		A.emp_act(severity)

/datum/component/storage/concrete/proc/on_slave_link(datum/component/storage/S)
	if(S == src)
		return FALSE
	slaves += S
	return TRUE

/datum/component/storage/concrete/proc/on_slave_unlink(datum/component/storage/S)
	slaves -= S
	return FALSE

/datum/component/storage/concrete/proc/on_contents_del(datum/source, atom/A)
	var/atom/real_location = parent
	if(A in real_location)
		usr = null
		remove_from_storage(A, null)

/datum/component/storage/concrete/proc/on_deconstruct(datum/source, disassembled)
	if(drop_all_on_deconstruct)
		do_quick_empty()

/datum/component/storage/concrete/can_see_contents()
	. = ..()
	for(var/i in slaves)
		var/datum/component/storage/slave = i
		. |= slave.can_see_contents()

//Resets screen loc and other vars of something being removed from storage.
/datum/component/storage/concrete/_removal_reset(atom/movable/thing)
	thing.layer = initial(thing.layer)
	thing.plane = initial(thing.plane)
	thing.mouse_opacity = initial(thing.mouse_opacity)
	if(thing.maptext)
		thing.maptext = ""

/datum/component/storage/concrete/remove_from_storage(atom/movable/AM, atom/new_location)
	//Cache this as it should be reusable down the bottom, will not apply if anyone adds a sleep to dropped
	//or moving objects, things that should never happen
	var/atom/parent = src.parent
	var/list/seeing_mobs = can_see_contents()
	for(var/mob/M in seeing_mobs)
		M.client.screen -= AM
	if(ismob(parent.loc) && isitem(AM))
		var/obj/item/I = AM
		var/mob/M = parent.loc
		I.dropped(M, TRUE)
		I.item_flags &= ~IN_STORAGE
	if(new_location)
		//Reset the items values
		_removal_reset(AM)
		AM.forceMove(new_location)
		//We don't want to call this if the item is being destroyed
		AM.on_exit_storage(src)
	else
		//Being destroyed, just move to nullspace now (so it's not in contents for the icon update)
		AM.moveToNullspace()
	refresh_mob_views()
	if(isobj(parent))
		var/obj/O = parent
		O.update_icon()
	return TRUE

/datum/component/storage/concrete/proc/slave_can_insert_object(datum/component/storage/slave, obj/item/I, stop_messages = FALSE, mob/M)
	return TRUE

/datum/component/storage/concrete/proc/handle_item_insertion_from_slave(datum/component/storage/slave, obj/item/I, prevent_warning = FALSE, M)
	. = handle_item_insertion(I, prevent_warning, M, slave)
	if(. && !prevent_warning)
		slave.mob_item_insertion_feedback(usr, M, I)

/datum/component/storage/concrete/handle_item_insertion(obj/item/I, prevent_warning = FALSE, mob/M, datum/component/storage/remote)		//Remote is null or the slave datum
	var/datum/component/storage/concrete/master = master()
	var/atom/parent = src.parent
	var/moved = FALSE
	if(!istype(I))
		return FALSE
	if(M)
		if(!M.temporarilyRemoveItemFromInventory(I))
			return FALSE
		else
			moved = TRUE			//At this point if the proc fails we need to manually move the object back to the turf/mob/whatever.
	if(I.pulledby)
		I.pulledby.stop_pulling()
	if(silent)
		prevent_warning = TRUE
	if(!_insert_physical_item(I))
		if(moved)
			if(M)
				if(!M.put_in_active_hand(I))
					I.forceMove(parent.drop_location())
			else
				I.forceMove(parent.drop_location())
		return FALSE
	I.on_enter_storage(master)
	I.item_flags |= IN_STORAGE
	refresh_mob_views()
	I.mouse_opacity = MOUSE_OPACITY_OPAQUE //So you can click on the area around the item to equip it, instead of having to pixel hunt
	if(M)
		if(M.client && M.active_storage != src)
			M.client.screen -= I
		if(M.observers && M.observers.len)
			for(var/i in M.observers)
				var/mob/dead/observe = i
				if(observe.client && observe.active_storage != src)
					observe.client.screen -= I
		if(!remote)
			parent.add_fingerprint(M)
			if(!prevent_warning)
				mob_item_insertion_feedback(usr, M, I)
	update_icon()
	return TRUE

/datum/component/storage/concrete/update_icon()
	if(isobj(parent))
		var/obj/O = parent
		O.update_icon()
	for(var/i in slaves)
		var/datum/component/storage/slave = i
		slave.update_icon()
