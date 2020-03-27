#define MONKEYS_ESCAPED		1
#define MONKEYS_LIVED		2
#define MONKEYS_DIED		3
#define DISEASE_LIVED		4

/datum/antagonist/monkey
	name = "Monkey"
	job_rank = ROLE_MONKEY
	roundend_category = "monkeys"
	antagpanel_category = "Monkey"
	var/datum/team/monkey/monkey_team
	var/monkey_only = TRUE

/datum/antagonist/monkey/can_be_owned(datum/mind/new_owner)
	return ..() && (!monkey_only || ismonkey(new_owner.current))

/datum/antagonist/monkey/get_team()
	return monkey_team

/datum/antagonist/monkey/on_gain()
	. = ..()
	SSticker.mode.ape_infectees += owner
	owner.special_role = "Infected Monkey"

	var/datum/disease/D = new /datum/disease/transformation/jungle_fever/monkeymode
	if(!owner.current.HasDisease(D))
		owner.current.ForceContractDisease(D)
	else
		QDEL_NULL(D)

/datum/antagonist/monkey/greet()
	to_chat(owner, "<b>You are a monkey now!</b>")
	to_chat(owner, "<b>Bite humans to infect them, follow the orders of the monkey leaders, and help fellow monkeys!</b>")
	to_chat(owner, "<b>Ensure at least one infected monkey escapes on the Emergency Shuttle!</b>")
	to_chat(owner, "<b><i>As an intelligent monkey, you know how to use technology and how to ventcrawl while wearing things.</i></b>")
	to_chat(owner, "<b>You can use :k to talk to fellow monkeys!</b>")
	SEND_SOUND(owner.current, sound('sound/ambience/antag/monkey.ogg'))

/datum/antagonist/monkey/on_removal()
	owner.special_role = null
	SSticker.mode.ape_infectees -= owner

	var/datum/disease/transformation/jungle_fever/D =  locate() in owner.current.diseases
	if(D)
		qdel(D)

	. = ..()

/datum/antagonist/monkey/create_team(datum/team/monkey/new_team)
	if(!new_team)
		for(var/datum/antagonist/monkey/H in GLOB.antagonists)
			if(!H.owner)
				continue
			if(H.monkey_team)
				monkey_team = H.monkey_team
				return
		monkey_team = new /datum/team/monkey
		monkey_team.update_objectives()
		return
	if(!istype(new_team))
		stack_trace("Wrong team type passed to [type] initialization.")
	monkey_team = new_team

/datum/antagonist/monkey/proc/forge_objectives()
	objectives |= monkey_team.objectives

/datum/antagonist/monkey/admin_remove(mob/admin)
	var/mob/living/carbon/monkey/M = owner.current
	if(istype(M))
		switch(alert(admin, "Humanize?", "Humanize", "Yes", "No"))
			if("Yes")
				if(admin == M)
					admin = M.humanize(TR_KEEPITEMS  |  TR_KEEPIMPLANTS  |  TR_KEEPORGANS  |  TR_KEEPDAMAGE  |  TR_KEEPVIRUS  | TR_KEEPSTUNS | TR_KEEPREAGENTS |  TR_DEFAULTMSG)
				else
					M.humanize(TR_KEEPITEMS  |  TR_KEEPIMPLANTS  |  TR_KEEPORGANS  |  TR_KEEPDAMAGE  |  TR_KEEPVIRUS  |  TR_KEEPSTUNS  |  TR_KEEPREAGENTS  |  TR_DEFAULTMSG)
			if("No")
				//nothing
			else
				return
	. = ..()

/datum/antagonist/monkey/leader
	name = "Monkey Leader"
	monkey_only = FALSE

/datum/antagonist/monkey/leader/admin_add(datum/mind/new_owner,mob/admin)
	var/mob/living/carbon/human/H = new_owner.current
	if(istype(H))
		switch(alert(admin, "Monkeyize?", "Monkeyize", "Yes", "No"))
			if("Yes")
				if(admin == H)
					admin = H.monkeyize()
				else
					H.monkeyize()
			if("No")
				//nothing
			else
				return
	new_owner.add_antag_datum(src)
	log_admin("[key_name(admin)] made [key_name(new_owner)] a monkey leader!")
	message_admins("[key_name_admin(admin)] made [key_name_admin(new_owner)] a monkey leader!")

/datum/antagonist/monkey/leader/on_gain()
	. = ..()
	var/obj/item/organ/heart/freedom/F = new
	F.Insert(owner.current, drop_if_replaced = FALSE)
	SSticker.mode.ape_leaders += owner
	owner.special_role = "Monkey Leader"

/datum/antagonist/monkey/leader/on_removal()
	SSticker.mode.ape_leaders -= owner
	var/obj/item/organ/heart/H = new
	H.Insert(owner.current, drop_if_replaced = FALSE) //replace freedom heart with normal heart

	. = ..()

/datum/antagonist/monkey/leader/greet()
	to_chat(owner, "<B><span class='notice'>You are the Jungle Fever patient zero!!</B></span>")
	to_chat(owner, "<b>You have been planted onto this station by the Animal Rights Consortium.</b>")
	to_chat(owner, "<b>Soon the disease will transform you into an ape. Afterwards, you will be able spread the infection to others with a bite.</b>")
	to_chat(owner, "<b>While your infection strain is undetectable by scanners, any other infectees will show up on medical equipment.</b>")
	to_chat(owner, "<b>Your mission will be deemed a success if any of the live infected monkeys reach CentCom.</b>")
	to_chat(owner, "<b>As an initial infectee, you will be considered a 'leader' by your fellow monkeys.</b>")
	to_chat(owner, "<b>You can use :k to talk to fellow monkeys!</b>")
	SEND_SOUND(owner.current, sound('sound/ambience/antag/monkey.ogg'))

/datum/objective/monkey
	explanation_text = "Ensure that infected monkeys escape on the emergency shuttle!"
	martyr_compatible = TRUE
	var/monkeys_to_win = 1
	var/escaped_monkeys = 0

/datum/objective/monkey/check_completion()
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/monkey/M in GLOB.alive_mob_list)
		if (M.HasDisease(D) && (M.onCentCom() || M.onSyndieBase()))
			escaped_monkeys++
	if(escaped_monkeys >= monkeys_to_win)
		return TRUE
	return FALSE

/datum/team/monkey
	name = "Monkeys"

/datum/team/monkey/proc/update_objectives()
	objectives = list()
	var/datum/objective/monkey/O = new()
	O.team = src
	objectives += O

/datum/team/monkey/proc/infected_monkeys_alive()
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/monkey/M in GLOB.alive_mob_list)
		if(M.HasDisease(D))
			return TRUE
	return FALSE

/datum/team/monkey/proc/infected_monkeys_escaped()
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/monkey/M in GLOB.alive_mob_list)
		if(M.HasDisease(D) && (M.onCentCom() || M.onSyndieBase()))
			return TRUE
	return FALSE

/datum/team/monkey/proc/infected_humans_escaped()
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/human/M in GLOB.alive_mob_list)
		if(M.HasDisease(D) && (M.onCentCom() || M.onSyndieBase()))
			return TRUE
	return FALSE

/datum/team/monkey/proc/infected_humans_alive()
	var/datum/disease/D = new /datum/disease/transformation/jungle_fever()
	for(var/mob/living/carbon/human/M in GLOB.alive_mob_list)
		if(M.HasDisease(D))
			return TRUE
	return FALSE

/datum/team/monkey/proc/get_result()
	if(infected_monkeys_escaped())
		return MONKEYS_ESCAPED
	if(infected_monkeys_alive())
		return MONKEYS_LIVED
	if(infected_humans_alive() || infected_humans_escaped())
		return DISEASE_LIVED
	return MONKEYS_DIED

/datum/team/monkey/roundend_report()
	var/list/parts = list()
	switch(get_result())
		if(MONKEYS_ESCAPED)
			parts += "<span class='greentext big'><B>Monkey Major Victory!</B></span>"
			parts += "<span class='greentext'><B>Central Command and [station_name()] were taken over by the monkeys! Ook ook!</B></span>"
		if(MONKEYS_LIVED)
			parts += "<FONT size = 3><B>Monkey Minor Victory!</B></FONT>"
			parts += "<span class='greentext'><B>[station_name()] was taken over by the monkeys! Ook ook!</B></span>"
		if(DISEASE_LIVED)
			parts += "<span class='redtext big'><B>Monkey Minor Defeat!</B></span>"
			parts += "<span class='redtext'><B>All the monkeys died, but the disease lives on! The future is uncertain.</B></span>"
		if(MONKEYS_DIED)
			parts += "<span class='redtext big'><B>Monkey Major Defeat!</B></span>"
			parts += "<span class='redtext'><B>All the monkeys died, and Jungle Fever was wiped out!</B></span>"
	var/list/leaders = get_antag_minds(/datum/antagonist/monkey/leader, TRUE)
	var/list/monkeys = get_antag_minds(/datum/antagonist/monkey, TRUE)

	if(LAZYLEN(leaders))
		parts += "<span class='header'>The monkey leaders were:</span>"
		parts += printplayerlist(SSticker.mode.ape_leaders)
	if(LAZYLEN(monkeys))
		parts += "<span class='header'>The monkeys were:</span>"
		parts += printplayerlist(SSticker.mode.ape_infectees)
	return "<div class='panel redborder'>[parts.Join("<br>")]</div>"
