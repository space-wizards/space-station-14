/datum/game_mode
	var/list/target_list = list()
	var/list/late_joining_list = list()

/datum/game_mode/traitor/internal_affairs
	name = "Internal Affairs"
	config_tag = "internal_affairs"
	report_type = "internal_affairs"
	false_report_weight = 10
	required_players = 25
	required_enemies = 5
	recommended_enemies = 8
	reroll_friendly = 0
	traitor_name = "Nanotrasen Internal Affairs Agent"
	antag_flag = ROLE_INTERNAL_AFFAIRS

	traitors_possible = 10 //hard limit on traitors if scaling is turned off
	num_modifier = 4 // Four additional traitors
	antag_datum = /datum/antagonist/traitor/internal_affairs

	announce_text = "There are Nanotrasen Internal Affairs Agents trying to kill each other!\n\
	<span class='danger'>IAA</span>: Eliminate your targets and protect yourself!\n\
	<span class='notice'>Crew</span>: Stop the IAA agents before they can cause too much mayhem."



/datum/game_mode/traitor/internal_affairs/post_setup()
	var/i = 0
	for(var/datum/mind/traitor in pre_traitors)
		i++
		if(i + 1 > pre_traitors.len)
			i = 0
		target_list[traitor] = pre_traitors[i+1]
	..()


/datum/game_mode/traitor/internal_affairs/add_latejoin_traitor(datum/mind/character)

	check_potential_agents()

	// As soon as we get 3 or 4 extra latejoin traitors, make them traitors and kill each other.
	if(late_joining_list.len >= rand(3, 4))
		// True randomness
		shuffle_inplace(late_joining_list)
		// Reset the target_list, it'll be used again in force_traitor_objectives
		target_list = list()

		// Basically setting the target_list for who is killing who
		var/i = 0
		for(var/datum/mind/traitor in late_joining_list)
			i++
			if(i + 1 > late_joining_list.len)
				i = 0
			target_list[traitor] = late_joining_list[i + 1]
			traitor.special_role = traitor_name

		// Now, give them their targets
		for(var/datum/mind/traitor in target_list)
			..(traitor)

		late_joining_list = list()
	else
		late_joining_list += character
	return

/datum/game_mode/traitor/internal_affairs/proc/check_potential_agents()

	for(var/M in late_joining_list)
		if(istype(M, /datum/mind))
			var/datum/mind/agent_mind = M
			if(ishuman(agent_mind.current))
				var/mob/living/carbon/human/H = agent_mind.current
				if(H.stat != DEAD)
					if(H.client)
						continue // It all checks out.

		// If any check fails, remove them from our list
		late_joining_list -= M


/datum/game_mode/traitor/internal_affairs/generate_report()
	return "Nanotrasen denies any accusations of placing internal affairs agents onboard your station to eliminate inconvenient employees.  Any further accusations against CentCom for such \
			actions will be met with a conversation with an official internal affairs agent."
