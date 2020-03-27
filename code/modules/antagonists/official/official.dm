/datum/antagonist/official
	name = "CentCom Official"
	show_name_in_check_antagonists = TRUE
	show_in_antagpanel = FALSE
	var/datum/objective/mission
	var/datum/team/ert/ert_team
	can_hijack = HIJACK_PREVENT

/datum/antagonist/official/greet()
	to_chat(owner, "<B><font size=3 color=red>You are a CentCom Official.</font></B>")
	if (ert_team)
		to_chat(owner, "Central Command is sending you to [station_name()] with the task: [ert_team.mission.explanation_text]")
	else
		to_chat(owner, "Central Command is sending you to [station_name()] with the task: [mission.explanation_text]")

/datum/antagonist/official/proc/equip_official()
	var/mob/living/carbon/human/H = owner.current
	if(!istype(H))
		return
	H.equipOutfit(/datum/outfit/centcom/centcom_official)

	if(CONFIG_GET(flag/enforce_human_authority))
		H.set_species(/datum/species/human)

/datum/antagonist/official/create_team(datum/team/new_team)
	if(istype(new_team))
		ert_team = new_team

/datum/antagonist/official/proc/forge_objectives()
	if (ert_team)
		objectives |= ert_team.objectives
	else if (!mission)
		var/datum/objective/missionobj = new
		missionobj.owner = owner
		missionobj.explanation_text = "Conduct a routine performance review of [station_name()] and its Captain."
		missionobj.completed = 1
		mission = missionobj
		objectives |= mission


/datum/antagonist/official/on_gain()
	forge_objectives()
	. = ..()
	equip_official()
