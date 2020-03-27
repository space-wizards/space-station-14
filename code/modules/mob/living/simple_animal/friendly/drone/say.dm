//Base proc for anything to call
/proc/_alert_drones(msg, dead_can_hear = 0, atom/source, mob/living/faction_checked_mob, exact_faction_match)
	if (dead_can_hear && source)
		for (var/mob/M in GLOB.dead_mob_list)
			var/link = FOLLOW_LINK(M, source)
			to_chat(M, "[link] [msg]")
	for(var/i in GLOB.drones_list)
		var/mob/living/simple_animal/drone/D = i
		if(istype(D) && D.stat != DEAD)
			if(faction_checked_mob)
				if(D.faction_check_mob(faction_checked_mob, exact_faction_match))
					to_chat(D, msg)
			else
				to_chat(D, msg)



//Wrapper for drones to handle factions
/mob/living/simple_animal/drone/proc/alert_drones(msg, dead_can_hear = FALSE)
	_alert_drones(msg, dead_can_hear, src, src, TRUE)


/mob/living/simple_animal/drone/proc/drone_chat(msg)
	alert_drones("<i>Drone Chat: <span class='name'>[name]</span> <span class='message'>[say_quote(msg)]</span></i>", TRUE)

/mob/living/simple_animal/drone/binarycheck()
	return TRUE
