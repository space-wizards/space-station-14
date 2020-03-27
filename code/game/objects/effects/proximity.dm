/datum/proximity_monitor
	var/atom/host	//the atom we are tracking
	var/atom/hasprox_receiver //the atom that will receive HasProximity calls.
	var/atom/last_host_loc
	var/list/checkers //list of /obj/effect/abstract/proximity_checkers
	var/current_range
	var/ignore_if_not_on_turf	//don't check turfs in range if the host's loc isn't a turf
	var/wire = FALSE

/datum/proximity_monitor/New(atom/_host, range, _ignore_if_not_on_turf = TRUE)
	checkers = list()
	last_host_loc = _host.loc
	ignore_if_not_on_turf = _ignore_if_not_on_turf
	current_range = range
	SetHost(_host)

/datum/proximity_monitor/proc/SetHost(atom/H,atom/R)
	if(H == host)
		return
	if(host)
		UnregisterSignal(host, COMSIG_MOVABLE_MOVED)
	if(R)
		hasprox_receiver = R
	else if(hasprox_receiver == host) //Default case
		hasprox_receiver = H
	host = H
	RegisterSignal(host, COMSIG_MOVABLE_MOVED, .proc/HandleMove)
	last_host_loc = host.loc
	SetRange(current_range,TRUE)

/datum/proximity_monitor/Destroy()
	host = null
	last_host_loc = null
	hasprox_receiver = null
	QDEL_LIST(checkers)
	return ..()

/datum/proximity_monitor/proc/HandleMove()
	var/atom/_host = host
	var/atom/new_host_loc = _host.loc
	if(last_host_loc != new_host_loc)
		last_host_loc = new_host_loc	//hopefully this won't cause GC issues with containers
		var/curr_range = current_range
		SetRange(curr_range, TRUE)
		if(curr_range)
			testing("HasProx: [host] -> [host]")
			hasprox_receiver.HasProximity(host)	//if we are processing, we're guaranteed to be a movable

/datum/proximity_monitor/proc/SetRange(range, force_rebuild = FALSE)
	if(!force_rebuild && range == current_range)
		return FALSE
	. = TRUE

	current_range = range

	var/list/checkers_local = checkers
	var/old_checkers_len = checkers_local.len

	var/atom/_host = host

	var/atom/loc_to_use = ignore_if_not_on_turf ? _host.loc : get_turf(_host)
	if(wire && !isturf(loc_to_use)) //it makes assemblies attached on wires work
		loc_to_use = get_turf(loc_to_use)
	if(!isturf(loc_to_use))	//only check the host's loc
		if(range)
			var/obj/effect/abstract/proximity_checker/pc
			if(old_checkers_len)
				pc = checkers_local[old_checkers_len]
				--checkers_local.len
				QDEL_LIST(checkers_local)
			else
				pc = new(loc_to_use, src)

			checkers_local += pc	//only check the host's loc
		return

	var/list/turfs = RANGE_TURFS(range, loc_to_use)
	var/turfs_len = turfs.len
	var/old_checkers_used = min(turfs_len, old_checkers_len)

	//reuse what we can
	for(var/I in 1 to old_checkers_len)
		if(I <= old_checkers_used)
			var/obj/effect/abstract/proximity_checker/pc = checkers_local[I]
			pc.forceMove(turfs[I])
		else
			qdel(checkers_local[I])	//delete the leftovers

	if(old_checkers_len < turfs_len)
		//create what we lack
		for(var/I in (old_checkers_used + 1) to turfs_len)
			checkers_local += new /obj/effect/abstract/proximity_checker(turfs[I], src)
	else
		checkers_local.Cut(old_checkers_used + 1, old_checkers_len)

/obj/effect/abstract/proximity_checker
	invisibility = INVISIBILITY_ABSTRACT
	anchored = TRUE
	var/datum/proximity_monitor/monitor

/obj/effect/abstract/proximity_checker/Initialize(mapload, datum/proximity_monitor/_monitor)
	. = ..()
	if(_monitor)
		monitor = _monitor
	else
		stack_trace("proximity_checker created without host")
		return INITIALIZE_HINT_QDEL

/obj/effect/abstract/proximity_checker/Destroy()
	monitor = null
	return ..()

/obj/effect/abstract/proximity_checker/Crossed(atom/movable/AM)
	set waitfor = FALSE
	monitor.hasprox_receiver.HasProximity(AM)
