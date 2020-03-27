#define LOCKON_AIMING_MAX_CURSOR_RADIUS 7
#define LOCKON_IGNORE_RESULT "ignore_my_result"
#define LOCKON_RANGING_BREAK_CHECK if(current_ranging_id != this_id){return LOCKON_IGNORE_RESULT}

/datum/component/lockon_aiming
	dupe_mode = COMPONENT_DUPE_ALLOWED
	var/lock_icon = 'icons/mob/cameramob.dmi'
	var/lock_icon_state = "marker"
	var/mutable_appearance/lock_appearance
	var/list/image/lock_images
	var/list/target_typecache
	var/list/immune_weakrefs				//list(weakref = TRUE)
	var/mob_stat_check = TRUE				//if a potential target is a mob make sure it's conscious!
	var/lock_amount = 1
	var/lock_cursor_range = 5
	var/list/locked_weakrefs
	var/update_disabled = FALSE
	var/current_ranging_id = 0
	var/list/last_location
	var/datum/callback/on_lock
	var/datum/callback/can_target_callback

/datum/component/lockon_aiming/Initialize(range, list/typecache, amount, list/immune, datum/callback/when_locked, icon, icon_state, datum/callback/target_callback)
	if(!ismob(parent))
		return COMPONENT_INCOMPATIBLE
	if(target_callback)
		can_target_callback = target_callback
	else
		can_target_callback = CALLBACK(src, .proc/can_target)
	if(range)
		lock_cursor_range = range
	if(typecache)
		target_typecache = typecache
	if(amount)
		lock_amount = amount
	immune_weakrefs = list(WEAKREF(parent) = TRUE)			//Manually take this out if you want..
	if(immune)
		for(var/i in immune)
			if(isweakref(i))
				immune_weakrefs[i] = TRUE
			else if(isatom(i))
				immune_weakrefs[WEAKREF(i)] = TRUE
	if(when_locked)
		on_lock = when_locked
	if(icon)
		lock_icon = icon
	if(icon_state)
		lock_icon_state = icon_state
	generate_lock_visuals()
	START_PROCESSING(SSfastprocess, src)

/datum/component/lockon_aiming/Destroy()
	clear_visuals()
	STOP_PROCESSING(SSfastprocess, src)
	return ..()

/datum/component/lockon_aiming/proc/show_visuals()
	LAZYINITLIST(lock_images)
	var/mob/M = parent
	if(!M.client)
		return
	for(var/i in locked_weakrefs)
		var/datum/weakref/R = i
		var/atom/A = R.resolve()
		if(!A)
			continue			//It'll be cleared by processing.
		var/image/I = new
		I.appearance = lock_appearance
		I.loc = A
		M.client.images |= I
		lock_images |= I

/datum/component/lockon_aiming/proc/clear_visuals()
	var/mob/M = parent
	if(!M.client)
		return
	if(!lock_images)
		return
	for(var/i in lock_images)
		M.client.images -= i
		qdel(i)
	lock_images.Cut()

/datum/component/lockon_aiming/proc/refresh_visuals()
	clear_visuals()
	show_visuals()

/datum/component/lockon_aiming/proc/generate_lock_visuals()
	lock_appearance = mutable_appearance(icon = lock_icon, icon_state = lock_icon_state, layer = FLOAT_LAYER)

/datum/component/lockon_aiming/proc/unlock_all(refresh_vis = TRUE)
	LAZYCLEARLIST(locked_weakrefs)
	if(refresh_vis)
		refresh_visuals()

/datum/component/lockon_aiming/proc/unlock(atom/A, refresh_vis = TRUE)
	if(!A.weak_reference)
		return
	LAZYREMOVE(locked_weakrefs, A.weak_reference)
	if(refresh_vis)
		refresh_visuals()

/datum/component/lockon_aiming/proc/lock(atom/A, refresh_vis = TRUE)
	LAZYOR(locked_weakrefs, WEAKREF(A))
	if(refresh_vis)
		refresh_visuals()

/datum/component/lockon_aiming/proc/add_immune_atom(atom/A)
	var/datum/weakref/R = WEAKREF(A)
	if(immune_weakrefs && (immune_weakrefs[R]))
		return
	LAZYSET(immune_weakrefs, R, TRUE)

/datum/component/lockon_aiming/proc/remove_immune_atom(atom/A)
	if(!A.weak_reference || !immune_weakrefs)		//if A doesn't have a weakref how did it get on the immunity list?
		return
	LAZYREMOVE(immune_weakrefs, A.weak_reference)

/datum/component/lockon_aiming/process()
	if(update_disabled)
		return
	if(!last_location)
		return
	var/changed = FALSE
	for(var/i in locked_weakrefs)
		var/datum/weakref/R = i
		if(istype(R))
			var/atom/thing = R.resolve()
			if(!istype(thing) || (get_dist(thing, locate(last_location[1], last_location[2], last_location[3])) > lock_cursor_range))
				unlock(R)
				changed = TRUE
		else
			unlock(R)
			changed = TRUE
	if(changed)
		autolock()

/datum/component/lockon_aiming/proc/autolock()
	var/mob/M = parent
	if(!M.client)
		return FALSE
	var/datum/position/current = mouse_absolute_datum_map_position_from_client(M.client)
	var/turf/target = current.return_turf()
	var/list/atom/targets = get_nearest(target, target_typecache, lock_amount, lock_cursor_range)
	if(targets == LOCKON_IGNORE_RESULT)
		return
	unlock_all(FALSE)
	for(var/i in targets)
		if(immune_weakrefs[WEAKREF(i)])
			continue
		lock(i, FALSE)
	refresh_visuals()
	on_lock.Invoke(locked_weakrefs)

/datum/component/lockon_aiming/proc/can_target(atom/A)
	var/mob/M = A
	return is_type_in_typecache(A, target_typecache) && !(ismob(A) && mob_stat_check && M.stat != CONSCIOUS) && !immune_weakrefs[WEAKREF(A)]

/datum/component/lockon_aiming/proc/get_nearest(turf/T, list/typecache, amount, range)
	current_ranging_id++
	var/this_id = current_ranging_id
	var/list/L = list()
	var/turf/center = get_turf(T)
	if(amount < 1 || range < 0 || !istype(center) || !islist(typecache))
		return
	if(range == 0)
		return typecache_filter_list(T.contents + T, typecache)
	var/x = 0
	var/y = 0
	var/cd = 0
	while(cd <= range)
		x = center.x - cd + 1
		y = center.y + cd
		LOCKON_RANGING_BREAK_CHECK
		for(x in x to center.x + cd)
			T = locate(x, y, center.z)
			if(T)
				L |= special_list_filter(T.contents, can_target_callback)
				if(L.len >= amount)
					L.Cut(amount+1)
					return L
		LOCKON_RANGING_BREAK_CHECK
		y = center.y + cd - 1
		x = center.x + cd
		for(y in center.y - cd to y)
			T = locate(x, y, center.z)
			if(T)
				L |= special_list_filter(T.contents, can_target_callback)
				if(L.len >= amount)
					L.Cut(amount+1)
					return L
		LOCKON_RANGING_BREAK_CHECK
		y = center.y - cd
		x = center.x + cd - 1
		for(x in center.x - cd to x)
			T = locate(x, y, center.z)
			if(T)
				L |= special_list_filter(T.contents, can_target_callback)
				if(L.len >= amount)
					L.Cut(amount+1)
					return L
		LOCKON_RANGING_BREAK_CHECK
		y = center.y - cd + 1
		x = center.x - cd
		for(y in y to center.y + cd)
			T = locate(x, y, center.z)
			if(T)
				L |= special_list_filter(T.contents, can_target_callback)
				if(L.len >= amount)
					L.Cut(amount+1)
					return L
		LOCKON_RANGING_BREAK_CHECK
		cd++
		CHECK_TICK
