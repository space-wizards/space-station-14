/datum/game_mode
	var/list/ape_infectees = list()
	var/list/ape_leaders = list()

/datum/game_mode/monkey
	name = "monkey"
	config_tag = "monkey"
	report_type = "monkey"
	antag_flag = ROLE_MONKEY
	false_report_weight = 1

	required_players = 20
	required_enemies = 1
	recommended_enemies = 1

	restricted_jobs = list("Cyborg", "AI")

	announce_span = "Monkey"
	announce_text = "One or more crewmembers have been infected with Jungle Fever! Crew: Contain the outbreak. None of the infected monkeys may escape alive to CentCom. Monkeys: Ensure that your kind lives on! Rise up against your captors!"

	var/carriers_to_make = 1
	var/list/carriers = list()

	var/monkeys_to_win = 1
	var/escaped_monkeys = 0

	var/players_per_carrier = 30

	var/datum/team/monkey/monkey_team



/datum/game_mode/monkey/pre_setup()
	carriers_to_make = max(round(num_players()/players_per_carrier, 1), 1)

	for(var/j = 0, j < carriers_to_make, j++)
		if (!antag_candidates.len)
			break
		var/datum/mind/carrier = pick(antag_candidates)
		carriers += carrier
		carrier.special_role = "Monkey Leader"
		carrier.restricted_roles = restricted_jobs
		log_game("[key_name(carrier)] has been selected as a Jungle Fever carrier")
		antag_candidates -= carrier

	if(!carriers.len)
		setup_error = "No monkey candidates"
		return FALSE
	return TRUE

/datum/game_mode/monkey/post_setup()
	for(var/datum/mind/carriermind in carriers)
		var/datum/antagonist/monkey/M = add_monkey_leader(carriermind, monkey_team)
		if(M)
			monkey_team = M.monkey_team
	return ..()

/datum/game_mode/monkey/check_finished()
	if((SSshuttle.emergency.mode == SHUTTLE_ENDGAME) || station_was_nuked)
		return TRUE

	if(!round_converted)
		for(var/datum/mind/monkey_mind in ape_infectees)
			continuous_sanity_checked = TRUE
			if(monkey_mind.current && monkey_mind.current.stat != DEAD)
				return FALSE

		var/datum/disease/D = new /datum/disease/transformation/jungle_fever() //ugly but unfortunately needed
		for(var/mob/living/carbon/human/H in GLOB.alive_mob_list)
			if(!is_station_level(H.z))
				continue
			if(H.mind && H.client && H.stat != DEAD)
				if(H.HasDisease(D))
					return FALSE

	return ..()

/datum/game_mode/monkey/proc/check_monkey_victory()
	if(SSshuttle.emergency.mode != SHUTTLE_ENDGAME)
		return FALSE
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/monkey/M in GLOB.alive_mob_list)
		if (M.HasDisease(D))
			if(M.onCentCom() || M.onSyndieBase())
				escaped_monkeys++
	if(escaped_monkeys >= monkeys_to_win)
		return TRUE
	else
		return FALSE


/datum/game_mode/monkey/set_round_result()
	..()
	if(check_monkey_victory())
		SSticker.mode_result = "win - monkey win"
	else
		SSticker.mode_result = "loss - staff stopped the monkeys"

/datum/game_mode/monkey/special_report()
	if(check_monkey_victory())
		return "<div class='panel redborder'><span class='redtext big'>The monkeys have overthrown their captors! Eeek eeeek!!</span></div>"
	else
		return "<div class='panel redborder'><span class='redtext big'>The staff managed to contain the monkey infestation!</span></div>"

/datum/game_mode/monkey/generate_report()
	return "Reports of an ancient [pick("retrovirus", "flesh eating bacteria", "disease", "magical curse blamed on viruses", "banana blight")] outbreak that turn humans into monkeys has been reported in your quadrant. Due to strain mutation, such infections are no longer curable by any known means. If an outbreak occurs, ensure the station is quarantined to prevent a largescale outbreak at CentCom."

/proc/add_monkey_leader(datum/mind/monkey_mind)
	if(is_monkey_leader(monkey_mind))
		return FALSE
	var/datum/antagonist/monkey/leader/M = monkey_mind.add_antag_datum(/datum/antagonist/monkey/leader)
	return M

/proc/add_monkey(datum/mind/monkey_mind)
	if(is_monkey(monkey_mind))
		return FALSE
	var/datum/antagonist/monkey/M = monkey_mind.add_antag_datum(/datum/antagonist/monkey)
	return M

/proc/remove_monkey(datum/mind/monkey_mind)
	if(!is_monkey(monkey_mind))
		return FALSE
	var/datum/antagonist/monkey/M = monkey_mind.has_antag_datum(/datum/antagonist/monkey)
	M.on_removal()
	return TRUE

/proc/is_monkey_leader(datum/mind/monkey_mind)
	return monkey_mind && monkey_mind.has_antag_datum(/datum/antagonist/monkey/leader)

/proc/is_monkey(datum/mind/monkey_mind)
	return monkey_mind && (monkey_mind.has_antag_datum(/datum/antagonist/monkey) || is_monkey_leader(monkey_mind))

