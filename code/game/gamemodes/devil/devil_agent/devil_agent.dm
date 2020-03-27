/datum/game_mode/devil/devil_agents
	name = "Devil Agents"
	config_tag = "devil_agents"
	required_players = 25
	required_enemies = 3
	recommended_enemies = 8
	reroll_friendly = 0

	traitors_possible = 10 //hard limit on traitors if scaling is turned off
	num_modifier = 4
	objective_count = 2

	var/list/devil_target_list = list() //will update to be a child of internal affairs when bothered
	var/list/devil_late_joining_list = list()
	minimum_devils = 3

	announce_text = "There are devil agents onboard the station, trying to outbid each other!\n\
		+	<span class='danger'>Devils</span>: Purchase souls and interfere with your rivals!\n\
		+	<span class='notice'>Crew</span>: Resist the lure of sin and remain pure!"

/datum/game_mode/devil/devil_agents/post_setup()
	var/i = 0
	for(var/datum/mind/devil in devils)
		i++
		if(i + 1 > devils.len)
			i = 0
		devil_target_list[devil] = devils[i + 1]
	..()

/datum/game_mode/devil/devil_agents/add_devil_objectives(datum/mind/devil_mind, quantity)
	..(devil_mind, quantity - give_outsell_objective(devil_mind))

/datum/game_mode/devil/devil_agents/proc/give_outsell_objective(datum/mind/devil)
	//If you override this method, have it return the number of objectives added.
	if(devil_target_list.len && devil_target_list[devil]) // Is a double agent
		var/datum/mind/target_mind = devil_target_list[devil]
		var/datum/antagonist/devil/D = target_mind.has_antag_datum(/datum/antagonist/devil)
		var/datum/objective/devil/outsell/outsellobjective = new
		outsellobjective.owner = devil
		outsellobjective.target = target_mind
		outsellobjective.update_explanation_text()
		D.objectives += outsellobjective
		return 1
	return 0
