/datum/component/swarming
	var/offset_x = 0
	var/offset_y = 0
	var/is_swarming = FALSE
	var/list/swarm_members = list()

/datum/component/swarming/Initialize(max_x = 24, max_y = 24)
	if(!ismovableatom(parent))
		return COMPONENT_INCOMPATIBLE
	offset_x = rand(-max_x, max_x)
	offset_y = rand(-max_y, max_y)

	RegisterSignal(parent, COMSIG_MOVABLE_CROSSED, .proc/join_swarm)
	RegisterSignal(parent, COMSIG_MOVABLE_UNCROSSED, .proc/leave_swarm)

/datum/component/swarming/Destroy()
	for(var/other in swarm_members)
		var/datum/component/swarming/other_swarm = other
		other_swarm.swarm_members -= src
		if(!other_swarm.swarm_members.len)
			other_swarm.unswarm()
	swarm_members = null
	return ..()

/datum/component/swarming/proc/join_swarm(datum/source, atom/movable/AM)
	var/datum/component/swarming/other_swarm = AM.GetComponent(/datum/component/swarming)
	if(!other_swarm)
		return
	swarm()
	swarm_members |= other_swarm
	other_swarm.swarm()
	other_swarm.swarm_members |= src

/datum/component/swarming/proc/leave_swarm(datum/source, atom/movable/AM)
	var/datum/component/swarming/other_swarm = AM.GetComponent(/datum/component/swarming)
	if(!other_swarm || !(other_swarm in swarm_members))
		return
	swarm_members -= other_swarm
	if(!swarm_members.len)
		unswarm()
	other_swarm.swarm_members -= src
	if(!other_swarm.swarm_members.len)
		other_swarm.unswarm()

/datum/component/swarming/proc/swarm()
	var/atom/movable/owner = parent
	if(!is_swarming)
		is_swarming = TRUE
		animate(owner, pixel_x = owner.pixel_x + offset_x, pixel_y = owner.pixel_y + offset_y, time = 2)

/datum/component/swarming/proc/unswarm()
	var/atom/movable/owner = parent
	if(is_swarming)
		animate(owner, pixel_x = owner.pixel_x - offset_x, pixel_y = owner.pixel_y - offset_y, time = 2)
		is_swarming = FALSE
