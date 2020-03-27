#define MAX_SPAWN_ATTEMPT 3


/datum/round_event/ghost_role
	// We expect 0 or more /clients (or things with .key) in this list
	var/list/priority_candidates = list()
	var/minimum_required = 1
	var/role_name = "debug rat with cancer" // Q U A L I T Y  M E M E S
	var/list/spawned_mobs = list()
	var/status
	fakeable = FALSE

/datum/round_event/ghost_role/start()
	try_spawning()

/datum/round_event/ghost_role/proc/try_spawning(sanity = 0, retry = 0)
	// The event does not run until the spawning has been attempted
	// to prevent us from getting gc'd halfway through
	processing = FALSE

	status = spawn_role()
	if((status == WAITING_FOR_SOMETHING))
		if(retry >= MAX_SPAWN_ATTEMPT)
			message_admins("[role_name] event has exceeded maximum spawn attempts. Aborting and refunding.")
			if(control && control.occurrences > 0)	//Don't refund if it hasn't
				control.occurrences--
			return
		var/waittime = 300 * (2^retry)
		message_admins("The event will not spawn a [role_name] until certain \
			conditions are met. Waiting [waittime/10]s and then retrying.")
		addtimer(CALLBACK(src, .proc/try_spawning, 0, ++retry), waittime)
		return

	if(status == MAP_ERROR)
		message_admins("[role_name] cannot be spawned due to a map error.")
	else if(status == NOT_ENOUGH_PLAYERS)
		message_admins("[role_name] cannot be spawned due to lack of players \
			signing up.")
		deadchat_broadcast(" did not get enough candidates ([minimum_required]) to spawn.", "<b>[role_name]</b>")
	else if(status == SUCCESSFUL_SPAWN)
		message_admins("[role_name] spawned successfully.")
		if(spawned_mobs.len)
			for (var/mob/M in spawned_mobs)
				announce_to_ghosts(M)
		else
			message_admins("No mobs found in the `spawned_mobs` list, this is \
				a bug.")
	else
		message_admins("An attempt to spawn [role_name] returned [status], \
			this is a bug.")

	processing = TRUE

/datum/round_event/ghost_role/proc/spawn_role()
	// Return true if role was successfully spawned, false if insufficent
	// players could be found, and just runtime if anything else happens
	return TRUE

/datum/round_event/ghost_role/proc/get_candidates(jobban, gametypecheck, be_special)
	// Returns a list of candidates in priority order, with candidates from
	// `priority_candidates` first, and ghost roles randomly shuffled and
	// appended after
	var/list/mob/dead/observer/regular_candidates
	// don't get their hopes up
	if(priority_candidates.len < minimum_required)
		regular_candidates = pollGhostCandidates("Do you wish to be considered for the special role of '[role_name]'?", jobban, gametypecheck, be_special)
	else
		regular_candidates = list()

	shuffle_inplace(regular_candidates)

	var/list/candidates = priority_candidates + regular_candidates

	return candidates

#undef MAX_SPAWN_ATTEMPT
