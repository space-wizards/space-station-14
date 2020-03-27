/datum/antagonist/disease
	name = "Sentient Disease"
	roundend_category = "diseases"
	antagpanel_category = "Disease"
	var/disease_name = ""

/datum/antagonist/disease/on_gain()
	owner.special_role = "Sentient Disease"
	owner.assigned_role = "Sentient Disease"
	var/datum/objective/O = new /datum/objective/disease_infect()
	O.owner = owner
	objectives += O

	O = new /datum/objective/disease_infect_centcom()
	O.owner = owner
	objectives += O

	. = ..()

/datum/antagonist/disease/greet()
	to_chat(owner.current, "<span class='notice'>You are the [owner.special_role]!</span>")
	to_chat(owner.current, "<span class='notice'>Infect members of the crew to gain adaptation points, and spread your infection further.</span>")
	owner.announce_objectives()

/datum/antagonist/disease/apply_innate_effects(mob/living/mob_override)
	if(!istype(owner.current, /mob/camera/disease))
		var/turf/T = get_turf(owner.current)
		T = T ? T : SSmapping.get_station_center()
		var/mob/camera/disease/D = new /mob/camera/disease(T)
		owner.transfer_to(D)

/datum/antagonist/disease/admin_add(datum/mind/new_owner,mob/admin)
	..()
	var/mob/camera/disease/D = new_owner.current
	D.pick_name()

/datum/antagonist/disease/roundend_report()
	var/list/result = list()

	result += "<b>Disease name:</b> [disease_name]"
	result += printplayer(owner)

	var/win = TRUE
	var/objectives_text = ""
	var/count = 1
	for(var/datum/objective/objective in objectives)
		if(objective.check_completion())
			objectives_text += "<br><B>Objective #[count]</B>: [objective.explanation_text] <span class='greentext'>Success!</span>"
		else
			objectives_text += "<br><B>Objective #[count]</B>: [objective.explanation_text] <span class='redtext'>Fail.</span>"
			win = FALSE
		count++

	result += objectives_text

	var/special_role_text = lowertext(name)

	if(win)
		result += "<span class='greentext'>The [special_role_text] was successful!</span>"
	else
		result += "<span class='redtext'>The [special_role_text] has failed!</span>"

	if(istype(owner.current, /mob/camera/disease))
		var/mob/camera/disease/D = owner.current
		result += "<B>[disease_name] completed the round with [D.hosts.len] infected hosts, and reached a maximum of [D.total_points] concurrent infections.</B>"
		result += "<B>[disease_name] completed the round with the following adaptations:</B>"
		var/list/adaptations = list()
		for(var/V in D.purchased_abilities)
			var/datum/disease_ability/A = V
			adaptations += A.name
		result += adaptations.Join(", ")

	return result.Join("<br>")


/datum/objective/disease_infect
	explanation_text = "Survive and infect as many people as possible."

/datum/objective/disease_infect/check_completion()
	var/mob/camera/disease/D = owner.current
	if(istype(D) && D.hosts.len) //theoretically it should not exist if it has no hosts, but better safe than sorry.
		return TRUE
	return FALSE


/datum/objective/disease_infect_centcom
	explanation_text = "Ensure that at least one infected host escapes on the shuttle or an escape pod."

/datum/objective/disease_infect_centcom/check_completion()
	var/mob/camera/disease/D = owner.current
	if(!istype(D))
		return FALSE
	for(var/V in D.hosts)
		var/mob/living/L = V
		if(L.onCentCom() || L.onSyndieBase())
			return TRUE
	return FALSE
