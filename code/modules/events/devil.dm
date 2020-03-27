/datum/round_event_control/devil
	name = "Create Devil"
	typepath = /datum/round_event/ghost_role/devil
	max_occurrences = 0

/datum/round_event/ghost_role/devil
	var/success_spawn = 0
	role_name = "devil"
	fakeable = FALSE

/datum/round_event/ghost_role/devil/kill()
	if(!success_spawn && control)
		control.occurrences--
	return ..()

/datum/round_event/ghost_role/devil/spawn_role()
	//selecting a spawn_loc
	if(!SSjob.latejoin_trackers.len)
		return MAP_ERROR

	//selecting a candidate player
	var/list/candidates = get_candidates(ROLE_DEVIL, null, ROLE_DEVIL)
	if(!candidates.len)
		return NOT_ENOUGH_PLAYERS

	var/mob/dead/selected_candidate = pick_n_take(candidates)
	var/key = selected_candidate.key

	var/datum/mind/Mind = create_devil_mind(key)
	Mind.active = 1

	var/mob/living/carbon/human/devil = create_event_devil()
	Mind.transfer_to(devil)
	add_devil(devil, ascendable = FALSE)

	spawned_mobs += devil
	message_admins("[ADMIN_LOOKUPFLW(devil)] has been made into a devil by an event.")
	log_game("[key_name(devil)] was spawned as a devil by an event.")
	var/datum/job/jobdatum = SSjob.GetJob("Assistant")
	devil.job = jobdatum.title
	jobdatum.equip(devil)
	return SUCCESSFUL_SPAWN


/proc/create_event_devil(spawn_loc)
	var/mob/living/carbon/human/new_devil = new(spawn_loc)
	if(!spawn_loc)
		SSjob.SendToLateJoin(new_devil)
	var/datum/preferences/A = new() //Randomize appearance for the devil.
	A.copy_to(new_devil)
	new_devil.dna.update_dna_identity()
	return new_devil

/proc/create_devil_mind(key)
	var/datum/mind/Mind = new /datum/mind(key)
	Mind.assigned_role = ROLE_DEVIL
	Mind.special_role = ROLE_DEVIL
	SSticker.mode.devils |= Mind
	return Mind
