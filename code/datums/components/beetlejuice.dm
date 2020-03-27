/datum/component/beetlejuice
	var/keyword
	var/list/first_heard
	var/list/count
	var/max_delay = 3 SECONDS //How fast they need to be said
	var/min_count = 3
	var/cooldown = 30 SECONDS //Delay between teleports
	var/active = TRUE
	var/case_sensitive = FALSE
	var/regex/R

/datum/component/beetlejuice/Initialize()
	if(!ismovableatom(parent))
		return COMPONENT_INCOMPATIBLE

	first_heard = list()
	count = list()

	var/atom/movable/O = parent
	keyword = O.name
	if(ismob(O))
		var/mob/M = parent
		keyword = M.real_name
	update_regex()

	RegisterSignal(SSdcs, COMSIG_GLOB_LIVING_SAY_SPECIAL, .proc/say_react)

/datum/component/beetlejuice/proc/update_regex()
	R = regex("[REGEX_QUOTE(keyword)]","g[case_sensitive ? "" : "i"]")

/datum/component/beetlejuice/vv_edit_var(var_name, var_value)
	. = ..()
	if (var_name == NAMEOF(src, keyword) || var_name == NAMEOF(src, case_sensitive))
		update_regex()

/datum/component/beetlejuice/proc/say_react(datum/source, mob/speaker,message)
	if(!speaker || !message || !active)
		return
	var/found = R.Find(message)
	if(found)
		var/occurences = 1
		while(R.Find(message))
			occurences++
		R.next = 1

		if(!first_heard[speaker] || (first_heard[speaker] + max_delay < world.time))
			first_heard[speaker] = world.time
			count[speaker] = 0
		count[speaker] += occurences
		if(count[speaker] >= min_count)
			first_heard -= speaker
			count -= speaker
			apport(speaker)


/datum/component/beetlejuice/proc/apport(atom/target)
	var/atom/movable/AM = parent
	do_teleport(AM,get_turf(target))
	active = FALSE
	addtimer(VARSET_CALLBACK(src, active, TRUE), cooldown)
