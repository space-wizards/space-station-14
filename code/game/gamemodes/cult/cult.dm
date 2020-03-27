#define CULT_SCALING_COEFFICIENT 9.3 //Roughly one new cultist at roundstart per this many players

/datum/game_mode
	var/list/datum/mind/cult = list()

/proc/iscultist(mob/living/M)
	return istype(M) && M.mind && M.mind.has_antag_datum(/datum/antagonist/cult)

/datum/team/cult/proc/is_sacrifice_target(datum/mind/mind)
	for(var/datum/objective/sacrifice/sac_objective in objectives)
		if(mind == sac_objective.target)
			return TRUE
	return FALSE

/proc/is_convertable_to_cult(mob/living/M,datum/team/cult/specific_cult)
	if(!istype(M))
		return FALSE
	if(M.mind)
		if(ishuman(M) && (M.mind.isholy))
			return FALSE
		if(specific_cult && specific_cult.is_sacrifice_target(M.mind))
			return FALSE
		if(M.mind.enslaved_to && !iscultist(M.mind.enslaved_to))
			return FALSE
		if(M.mind.unconvertable)
			return FALSE
	else
		return FALSE
	if(HAS_TRAIT(M, TRAIT_MINDSHIELD) || issilicon(M) || isbot(M) || isdrone(M) || !M.client)
		return FALSE //can't convert machines, shielded, or braindead
	return TRUE

/datum/game_mode/cult
	name = "cult"
	config_tag = "cult"
	report_type = "cult"
	antag_flag = ROLE_CULTIST
	false_report_weight = 10
	restricted_jobs = list("Chaplain","AI", "Cyborg", "Security Officer", "Warden", "Detective", "Head of Security", "Captain", "Head of Personnel")
	protected_jobs = list()
	required_players = 29
	required_enemies = 4
	recommended_enemies = 4
	enemy_minimum_age = 14

	announce_span = "cult"
	announce_text = "Some crew members are trying to start a cult to Nar'Sie!\n\
	<span class='cult'>Cultists</span>: Carry out Nar'Sie's will.\n\
	<span class='notice'>Crew</span>: Prevent the cult from expanding and drive it out."

	var/finished = 0

	var/acolytes_needed = 10 //for the survive objective
	var/acolytes_survived = 0

	var/list/cultists_to_cult = list() //the cultists we'll convert

	var/datum/team/cult/main_cult


/datum/game_mode/cult/pre_setup()
	if(CONFIG_GET(flag/protect_roles_from_antagonist))
		restricted_jobs += protected_jobs

	if(CONFIG_GET(flag/protect_assistant_from_antagonist))
		restricted_jobs += "Assistant"

	//cult scaling goes here
	recommended_enemies = 1 + round(num_players()/CULT_SCALING_COEFFICIENT)
	var/remaining = (num_players() % CULT_SCALING_COEFFICIENT) * 10 //Basically the % of how close the population is toward adding another cultis
	if(prob(remaining))
		recommended_enemies++


	for(var/cultists_number = 1 to recommended_enemies)
		if(!antag_candidates.len)
			break
		var/datum/mind/cultist = antag_pick(antag_candidates)
		antag_candidates -= cultist
		cultists_to_cult += cultist
		cultist.special_role = ROLE_CULTIST
		cultist.restricted_roles = restricted_jobs
		log_game("[key_name(cultist)] has been selected as a cultist")

	if(cultists_to_cult.len>=required_enemies)
		for(var/antag in cultists_to_cult)
			GLOB.pre_setup_antags += antag
		return TRUE
	else
		setup_error = "Not enough cultist candidates"
		return FALSE


/datum/game_mode/cult/post_setup()
	main_cult = new

	for(var/datum/mind/cult_mind in cultists_to_cult)
		add_cultist(cult_mind, 0, equip=TRUE, cult_team = main_cult)
		GLOB.pre_setup_antags -= cult_mind

	main_cult.setup_objectives() //Wait until all cultists are assigned to make sure none will be chosen as sacrifice.

	. = ..()

/datum/game_mode/proc/add_cultist(datum/mind/cult_mind, stun , equip = FALSE, datum/team/cult/cult_team = null)
	if (!istype(cult_mind))
		return FALSE

	var/datum/antagonist/cult/new_cultist = new()
	new_cultist.give_equipment = equip

	if(cult_mind.add_antag_datum(new_cultist,cult_team))
		if(stun)
			cult_mind.current.Unconscious(100)
		return TRUE

/datum/game_mode/proc/remove_cultist(datum/mind/cult_mind, silent, stun)
	if(cult_mind.current)
		var/datum/antagonist/cult/cult_datum = cult_mind.has_antag_datum(/datum/antagonist/cult)
		if(!cult_datum)
			return FALSE
		cult_datum.silent = silent
		cult_mind.remove_antag_datum(cult_datum)
		if(stun)
			cult_mind.current.Unconscious(100)
		return TRUE

/datum/game_mode/cult/proc/check_cult_victory()
	return main_cult.check_cult_victory()


/datum/game_mode/cult/set_round_result()
	..()
	if(check_cult_victory())
		SSticker.mode_result = "win - cult win"
		SSticker.news_report = CULT_SUMMON
	else
		SSticker.mode_result = "loss - staff stopped the cult"
		SSticker.news_report = CULT_FAILURE

/datum/game_mode/cult/proc/check_survive()
	var/acolytes_survived = 0
	for(var/datum/mind/cult_mind in cult)
		if (cult_mind.current && cult_mind.current.stat != DEAD)
			if(cult_mind.current.onCentCom() || cult_mind.current.onSyndieBase())
				acolytes_survived++
	if(acolytes_survived>=acolytes_needed)
		return 0
	else
		return 1


/datum/game_mode/cult/generate_report()
	return "Some stations in your sector have reported evidence of blood sacrifice and strange magic. Ties to the Wizards' Federation have been proven not to exist, and many employees \
			have disappeared; even Central Command employees light-years away have felt strange presences and at times hysterical compulsions. Interrogations point towards this being the work of \
			the cult of Nar'Sie. If evidence of this cult is discovered aboard your station, extreme caution and extreme vigilance must be taken going forward, and all resources should be \
			devoted to stopping this cult. Note that holy water seems to weaken and eventually return the minds of cultists that ingest it, and mindshield implants will prevent conversion \
			altogether."

#undef CULT_SCALING_COEFFICIENT
