/datum/antagonist/greentext
	name = "winner"
	show_in_antagpanel = FALSE
	show_name_in_check_antagonists = TRUE //Not that it will be there for long

/datum/antagonist/greentext/proc/forge_objectives()
	var/datum/objective/O = new /datum/objective("Succeed")
	O.completed = TRUE //YES!
	O.owner = owner
	objectives += O

/datum/antagonist/greentext/on_gain()
	forge_objectives()
	. = ..()
