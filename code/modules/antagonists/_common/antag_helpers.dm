//Returns MINDS of the assigned antags of given type/subtypes
/proc/get_antag_minds(antag_type,specific = FALSE)
	. = list()
	for(var/datum/antagonist/A in GLOB.antagonists)
		if(!A.owner)
			continue
		if(!antag_type || !specific && istype(A,antag_type) || specific && A.type == antag_type)
			. += A.owner

//Get all teams [of type team_type]
/proc/get_all_teams(team_type)
	. = list()
	for(var/V in GLOB.antagonists)
		var/datum/antagonist/A = V
		if(!A.owner)
			continue
		var/datum/team/T = A.get_team()
		if(!team_type || istype(T,team_type))
			. |= T
