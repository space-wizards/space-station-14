SUBSYSTEM_DEF(vis_overlays)
	name = "Vis contents overlays"
	wait = 1 MINUTES
	priority = FIRE_PRIORITY_VIS
	init_order = INIT_ORDER_VIS

	var/list/vis_overlay_cache
	var/list/unique_vis_overlays
	var/list/currentrun

/datum/controller/subsystem/vis_overlays/Initialize()
	vis_overlay_cache = list()
	unique_vis_overlays = list()
	return ..()

/datum/controller/subsystem/vis_overlays/fire(resumed = FALSE)
	if(!resumed)
		currentrun = vis_overlay_cache.Copy()
	var/list/current_run = currentrun

	while(current_run.len)
		var/key = current_run[current_run.len]
		var/obj/effect/overlay/vis/overlay = current_run[key]
		current_run.len--
		if(!overlay.unused && !length(overlay.vis_locs))
			overlay.unused = world.time
		else if(overlay.unused && overlay.unused + overlay.cache_expiration < world.time)
			vis_overlay_cache -= key
			qdel(overlay)
		if(MC_TICK_CHECK)
			return

//the "thing" var can be anything with vis_contents which includes images
/datum/controller/subsystem/vis_overlays/proc/add_vis_overlay(atom/movable/thing, icon, iconstate, layer, plane, dir, alpha = 255, add_appearance_flags = NONE, unique = FALSE)
	var/obj/effect/overlay/vis/overlay
	if(!unique)
		. = "[icon]|[iconstate]|[layer]|[plane]|[dir]|[alpha]|[add_appearance_flags]"
		overlay = vis_overlay_cache[.]
		if(!overlay)
			overlay = _create_new_vis_overlay(icon, iconstate, layer, plane, dir, alpha, add_appearance_flags)
			vis_overlay_cache[.] = overlay
		else
			overlay.unused = 0
	else
		overlay = _create_new_vis_overlay(icon, iconstate, layer, plane, dir, alpha, add_appearance_flags)
		overlay.cache_expiration = -1
		var/cache_id = "\ref[overlay]@{[world.time]}"
		unique_vis_overlays += overlay
		vis_overlay_cache[cache_id] = overlay
		. = overlay
	thing.vis_contents += overlay

	if(!isatom(thing)) // Automatic rotation is not supported on non atoms
		return

	if(!thing.managed_vis_overlays)
		thing.managed_vis_overlays = list(overlay)
		RegisterSignal(thing, COMSIG_ATOM_DIR_CHANGE, .proc/rotate_vis_overlay)
	else
		thing.managed_vis_overlays += overlay

/datum/controller/subsystem/vis_overlays/proc/_create_new_vis_overlay(icon, iconstate, layer, plane, dir, alpha, add_appearance_flags)
	var/obj/effect/overlay/vis/overlay = new
	overlay.icon = icon
	overlay.icon_state = iconstate
	overlay.layer = layer
	overlay.plane = plane
	overlay.dir = dir
	overlay.alpha = alpha
	overlay.appearance_flags |= add_appearance_flags
	return overlay


/datum/controller/subsystem/vis_overlays/proc/remove_vis_overlay(atom/movable/thing, list/overlays)
	thing.vis_contents -= overlays
	if(!isatom(thing))
		return
	thing.managed_vis_overlays -= overlays
	if(!length(thing.managed_vis_overlays))
		thing.managed_vis_overlays = null
		UnregisterSignal(thing, COMSIG_ATOM_DIR_CHANGE)

/datum/controller/subsystem/vis_overlays/proc/rotate_vis_overlay(atom/thing, old_dir, new_dir)
	if(old_dir == new_dir)
		return
	var/rotation = dir2angle(old_dir) - dir2angle(new_dir)
	var/list/overlays_to_remove = list()
	for(var/i in thing.managed_vis_overlays - unique_vis_overlays)
		var/obj/effect/overlay/vis/overlay = i
		add_vis_overlay(thing, overlay.icon, overlay.icon_state, overlay.layer, overlay.plane, turn(overlay.dir, rotation), overlay.alpha, overlay.appearance_flags)
		overlays_to_remove += overlay
	for(var/i in thing.managed_vis_overlays & unique_vis_overlays)
		var/obj/effect/overlay/vis/overlay = i
		overlay.dir = turn(overlay.dir, rotation)
	remove_vis_overlay(thing, overlays_to_remove)
