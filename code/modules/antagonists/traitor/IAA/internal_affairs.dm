#define PINPOINTER_MINIMUM_RANGE 15
#define PINPOINTER_EXTRA_RANDOM_RANGE 10
#define PINPOINTER_PING_TIME 40
#define PROB_ACTUAL_TRAITOR 20
#define TRAITOR_AGENT_ROLE "Syndicate External Affairs Agent"

/datum/antagonist/traitor/internal_affairs
	name = "Internal Affairs Agent"
	employer = "Nanotrasen"
	special_role = "internal affairs agent"
	antagpanel_category = "IAA"
	var/syndicate = FALSE
	var/last_man_standing = FALSE
	var/list/datum/mind/targets_stolen


/datum/antagonist/traitor/internal_affairs/proc/give_pinpointer()
	if(owner && owner.current)
		owner.current.apply_status_effect(/datum/status_effect/agent_pinpointer)

/datum/antagonist/traitor/internal_affairs/apply_innate_effects()
	.=..() //in case the base is used in future
	if(owner && owner.current)
		give_pinpointer(owner.current)

/datum/antagonist/traitor/internal_affairs/remove_innate_effects()
	.=..()
	if(owner && owner.current)
		owner.current.remove_status_effect(/datum/status_effect/agent_pinpointer)

/datum/antagonist/traitor/internal_affairs/on_gain()
	START_PROCESSING(SSprocessing, src)
	.=..()
/datum/antagonist/traitor/internal_affairs/on_removal()
	STOP_PROCESSING(SSprocessing,src)
	.=..()
/datum/antagonist/traitor/internal_affairs/process()
	iaa_process()


/datum/status_effect/agent_pinpointer
	id = "agent_pinpointer"
	duration = -1
	tick_interval = PINPOINTER_PING_TIME
	alert_type = /obj/screen/alert/status_effect/agent_pinpointer
	var/minimum_range = PINPOINTER_MINIMUM_RANGE
	var/range_fuzz_factor = PINPOINTER_EXTRA_RANDOM_RANGE
	var/mob/scan_target = null
	var/range_mid = 8
	var/range_far = 16

/obj/screen/alert/status_effect/agent_pinpointer
	name = "Internal Affairs Integrated Pinpointer"
	desc = "Even stealthier than a normal implant."
	icon = 'icons/obj/device.dmi'
	icon_state = "pinon"

/datum/status_effect/agent_pinpointer/proc/point_to_target() //If we found what we're looking for, show the distance and direction
	if(!scan_target)
		linked_alert.icon_state = "pinonnull"
		return
	var/turf/here = get_turf(owner)
	var/turf/there = get_turf(scan_target)
	if(here.z != there.z)
		linked_alert.icon_state = "pinonnull"
		return
	if(get_dist_euclidian(here,there)<=minimum_range + rand(0, range_fuzz_factor))
		linked_alert.icon_state = "pinondirect"
	else
		linked_alert.setDir(get_dir(here, there))
		var/dist = (get_dist(here, there))
		if(dist >= 1 && dist <= range_mid)
			linked_alert.icon_state = "pinonclose"
		else if(dist > range_mid && dist <= range_far)
			linked_alert.icon_state = "pinonmedium"
		else if(dist > range_far)
			linked_alert.icon_state = "pinonfar"

/datum/status_effect/agent_pinpointer/proc/scan_for_target()
	scan_target = null
	if(owner)
		if(owner.mind)
			for(var/datum/objective/objective_ in owner.mind.get_all_objectives())
				if(!is_internal_objective(objective_))
					continue
				var/datum/objective/assassinate/internal/objective = objective_
				var/mob/current = objective.target.current
				if(current&&current.stat!=DEAD)
					scan_target = current
				break

/datum/status_effect/agent_pinpointer/tick()
	if(!owner)
		qdel(src)
		return
	scan_for_target()
	point_to_target()


/proc/is_internal_objective(datum/objective/O)
	return (istype(O, /datum/objective/assassinate/internal)||istype(O, /datum/objective/destroy/internal))

/datum/antagonist/traitor/proc/replace_escape_objective()
	if(!owner || !objectives.len)
		return
	for (var/objective_ in objectives)
		if(!(istype(objective_, /datum/objective/escape)||istype(objective_, /datum/objective/survive)))
			continue
		remove_objective(objective_)

	var/datum/objective/martyr/martyr_objective = new
	martyr_objective.owner = owner
	add_objective(martyr_objective)

/datum/antagonist/traitor/proc/reinstate_escape_objective()
	if(!owner||!objectives.len)
		return
	for (var/objective_ in objectives)
		if(!istype(objective_, /datum/objective/martyr))
			continue
		remove_objective(objective_)

/datum/antagonist/traitor/internal_affairs/reinstate_escape_objective()
	..()
	var/objtype = traitor_kind == TRAITOR_HUMAN ? /datum/objective/escape : /datum/objective/survive
	var/datum/objective/escape_objective = new objtype
	escape_objective.owner = owner
	add_objective(escape_objective)

/datum/antagonist/traitor/internal_affairs/proc/steal_targets(datum/mind/victim)
	if(!owner.current||owner.current.stat==DEAD)
		return
	to_chat(owner.current, "<span class='userdanger'>Target eliminated: [victim.name]</span>")
	for(var/objective_ in victim.get_all_objectives())
		if(istype(objective_, /datum/objective/assassinate/internal))
			var/datum/objective/assassinate/internal/objective = objective_
			if(objective.target==owner)
				continue
			else if(targets_stolen.Find(objective.target) == 0)
				var/datum/objective/assassinate/internal/new_objective = new
				new_objective.owner = owner
				new_objective.target = objective.target
				new_objective.update_explanation_text()
				add_objective(new_objective)
				targets_stolen += objective.target
				var/status_text = objective.check_completion() ? "neutralised" : "active"
				to_chat(owner.current, "<span class='userdanger'>New target added to database: [objective.target.name] ([status_text])</span>")
		else if(istype(objective_, /datum/objective/destroy/internal))
			var/datum/objective/destroy/internal/objective = objective_
			var/datum/objective/destroy/internal/new_objective = new
			if(objective.target==owner)
				continue
			else if(targets_stolen.Find(objective.target) == 0)
				new_objective.owner = owner
				new_objective.target = objective.target
				new_objective.update_explanation_text()
				add_objective(new_objective)
				targets_stolen += objective.target
				var/status_text = objective.check_completion() ? "neutralised" : "active"
				to_chat(owner.current, "<span class='userdanger'>New target added to database: [objective.target.name] ([status_text])</span>")
	last_man_standing = TRUE
	for(var/objective_ in objectives)
		if(!is_internal_objective(objective_))
			continue
		var/datum/objective/assassinate/internal/objective = objective_
		if(!objective.check_completion())
			last_man_standing = FALSE
			return
	if(last_man_standing)
		if(syndicate)
			to_chat(owner.current,"<span class='userdanger'>All the loyalist agents are dead, and no more is required of you. Die a glorious death, agent.</span>")
		else
			to_chat(owner.current,"<span class='userdanger'>All the other agents are dead, and you're the last loose end. Stage a Syndicate terrorist attack to cover up for today's events. You no longer have any limits on collateral damage.</span>")
		replace_escape_objective(owner)

/datum/antagonist/traitor/internal_affairs/proc/iaa_process()
	if(owner&&owner.current&&owner.current.stat!=DEAD)
		for(var/objective_ in objectives)
			if(!is_internal_objective(objective_))
				continue
			var/datum/objective/assassinate/internal/objective = objective_
			if(!objective.target)
				continue
			if(objective.check_completion())
				if(objective.stolen)
					continue
				else
					steal_targets(objective.target)
					objective.stolen = TRUE
			else
				if(objective.stolen)
					var/fail_msg = "<span class='userdanger'>Your sensors tell you that [objective.target.current.real_name], one of the targets you were meant to have killed, pulled one over on you, and is still alive - do the job properly this time! </span>"
					if(last_man_standing)
						if(syndicate)
							fail_msg += "<span class='userdanger'> You no longer have permission to die. </span>"
						else
							fail_msg += "<span class='userdanger'> The truth could still slip out!</font><B><font size=5 color=red> Cease any terrorist actions as soon as possible, unneeded property damage or loss of employee life will lead to your contract being terminated.</span>"
						reinstate_escape_objective(owner)
						last_man_standing = FALSE
					to_chat(owner.current, fail_msg)
					objective.stolen = FALSE

/datum/antagonist/traitor/internal_affairs/proc/forge_iaa_objectives()
	if(SSticker.mode.target_list.len && SSticker.mode.target_list[owner]) // Is a double agent
		// Assassinate
		var/datum/mind/target_mind = SSticker.mode.target_list[owner]
		if(issilicon(target_mind.current))
			var/datum/objective/destroy/internal/destroy_objective = new
			destroy_objective.owner = owner
			destroy_objective.target = target_mind
			destroy_objective.update_explanation_text()
			add_objective(destroy_objective)
		else
			var/datum/objective/assassinate/internal/kill_objective = new
			kill_objective.owner = owner
			kill_objective.target = target_mind
			kill_objective.update_explanation_text()
			add_objective(kill_objective)

		//Optional traitor objective
		if(prob(PROB_ACTUAL_TRAITOR))
			employer = "The Syndicate"
			owner.special_role = TRAITOR_AGENT_ROLE
			special_role = TRAITOR_AGENT_ROLE
			syndicate = TRUE
			forge_single_objective()

/datum/antagonist/traitor/internal_affairs/forge_traitor_objectives()
	forge_iaa_objectives()

	var/objtype = traitor_kind == TRAITOR_HUMAN ? /datum/objective/escape : /datum/objective/survive
	var/datum/objective/escape_objective = new objtype
	escape_objective.owner = owner
	add_objective(escape_objective)

/datum/antagonist/traitor/internal_affairs/proc/greet_iaa()
	var/crime = pick("distribution of contraband" , "unauthorized erotic action on duty", "embezzlement", "piloting under the influence", "dereliction of duty", "syndicate collaboration", "mutiny", "multiple homicides", "corporate espionage", "receiving bribes", "malpractice", "worship of prohibited life forms", "possession of profane texts", "murder", "arson", "insulting their manager", "grand theft", "conspiracy", "attempting to unionize", "vandalism", "gross incompetence")

	to_chat(owner.current, "<span class='userdanger'>You are the [special_role].</span>")
	if(syndicate)
		to_chat(owner.current, "<span class='userdanger'>Your target has been framed for [crime], and you have been tasked with eliminating them to prevent them defending themselves in court.</span>")
		to_chat(owner.current, "<B><font size=5 color=red>Any damage you cause will be a further embarrassment to Nanotrasen, so you have no limits on collateral damage.</font></B>")
		to_chat(owner.current, "<span class='userdanger'>You have been provided with a standard uplink to accomplish your task.</span>")
	else
		to_chat(owner.current, "<span class='userdanger'>Your target is suspected of [crime], and you have been tasked with eliminating them by any means necessary to avoid a costly and embarrassing public trial.</span>")
		to_chat(owner.current, "<B><font size=5 color=red>While you have a license to kill, unneeded property damage or loss of employee life will lead to your contract being terminated.</font></B>")
		to_chat(owner.current, "<span class='userdanger'>For the sake of plausible deniability, you have been equipped with an array of captured Syndicate weaponry available via uplink.</span>")

	to_chat(owner.current, "<span class='userdanger'>Finally, watch your back. Your target has friends in high places, and intel suggests someone may have taken out a contract of their own to protect them.</span>")
	owner.announce_objectives()

/datum/antagonist/traitor/internal_affairs/greet()
	greet_iaa()

#undef PROB_ACTUAL_TRAITOR
#undef PINPOINTER_EXTRA_RANDOM_RANGE
#undef PINPOINTER_MINIMUM_RANGE
#undef PINPOINTER_PING_TIME
