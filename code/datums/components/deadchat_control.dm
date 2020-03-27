#define DEMOCRACY_MODE "democracy"
#define ANARCHY_MODE "anarchy"

/datum/component/deadchat_control
	dupe_mode = COMPONENT_DUPE_UNIQUE
	var/timerid

	var/list/datum/callback/inputs = list()
	var/list/ckey_to_cooldown = list()
	var/orbiters = list()
	var/deadchat_mode
	var/input_cooldown

/datum/component/deadchat_control/Initialize(_deadchat_mode, _inputs, _input_cooldown = 12 SECONDS)
	if(!isatom(parent))
		return COMPONENT_INCOMPATIBLE
	RegisterSignal(parent, COMSIG_ATOM_ORBIT_BEGIN, .proc/orbit_begin)
	RegisterSignal(parent, COMSIG_ATOM_ORBIT_STOP, .proc/orbit_stop)
	deadchat_mode = _deadchat_mode
	inputs = _inputs
	input_cooldown = _input_cooldown
	if(deadchat_mode == DEMOCRACY_MODE)
		timerid = addtimer(CALLBACK(src, .proc/democracy_loop), input_cooldown, TIMER_STOPPABLE | TIMER_LOOP)
	notify_ghosts("[parent] is now deadchat controllable!", source = parent, action = NOTIFY_ORBIT, header="Something Interesting!")


/datum/component/deadchat_control/Destroy(force, silent)
	inputs = null
	orbiters = null
	ckey_to_cooldown = null
	return ..()

/datum/component/deadchat_control/proc/deadchat_react(mob/source, message)
	message = lowertext(message)
	if(!inputs[message])
		return 
	if(deadchat_mode == ANARCHY_MODE)
		var/cooldown = ckey_to_cooldown[source.ckey]
		if(cooldown)
			return MOB_DEADSAY_SIGNAL_INTERCEPT
		inputs[message].Invoke()
		ckey_to_cooldown[source.ckey] = TRUE
		addtimer(CALLBACK(src, .proc/remove_cooldown, source.ckey), input_cooldown)
	else if(deadchat_mode == DEMOCRACY_MODE)
		ckey_to_cooldown[source.ckey] = message
	return MOB_DEADSAY_SIGNAL_INTERCEPT

/datum/component/deadchat_control/proc/remove_cooldown(ckey)
	ckey_to_cooldown.Remove(ckey)
	
/datum/component/deadchat_control/proc/democracy_loop()
	if(QDELETED(parent) || deadchat_mode != DEMOCRACY_MODE)
		deltimer(timerid)
		return
	var/result = count_democracy_votes()
	if(!isnull(result))
		inputs[result].Invoke()
		var/message = "<span class='deadsay italics bold'>[parent] has done action [result]!<br>New vote started. It will end in [input_cooldown/10] seconds.</span>"
		for(var/M in orbiters)
			to_chat(M, message)
	else
		var/message = "<span class='deadsay italics bold'>No votes were cast this cycle.</span>"
		for(var/M in orbiters)
			to_chat(M, message)
			
/datum/component/deadchat_control/proc/count_democracy_votes()
	if(!length(ckey_to_cooldown))
		return
	var/list/votes = list()
	for(var/command in inputs)
		votes["[command]"] = 0
	for(var/vote in ckey_to_cooldown)
		votes[ckey_to_cooldown[vote]]++
		ckey_to_cooldown.Remove(vote)
	
	// Solve which had most votes.
	var/prev_value = 0
	var/result
	for(var/vote in votes)
		if(votes[vote] > prev_value)
			prev_value = votes[vote]
			result = vote
	
	if(result in inputs)
		return result

/datum/component/deadchat_control/vv_edit_var(var_name, var_value)
	. = ..()
	if(!.)
		return
	if(var_name != NAMEOF(src, deadchat_mode))
		return
	ckey_to_cooldown = list()
	if(var_value == DEMOCRACY_MODE)
		timerid = addtimer(CALLBACK(src, .proc/democracy_loop), input_cooldown, TIMER_STOPPABLE | TIMER_LOOP)
	else
		deltimer(timerid)

/datum/component/deadchat_control/proc/orbit_begin(atom/source, atom/orbiter)
	RegisterSignal(orbiter, COMSIG_MOB_DEADSAY, .proc/deadchat_react)
	orbiters |= orbiter

/datum/component/deadchat_control/proc/orbit_stop(atom/source, atom/orbiter)
	if(orbiter in orbiters)
		UnregisterSignal(orbiter, COMSIG_MOB_DEADSAY)
		orbiters -= orbiter
