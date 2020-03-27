/datum/game_mode/sandbox
	name = "sandbox"
	config_tag = "sandbox"
	report_type = "sandbox"
	required_players = 0

	announce_span = "info"
	announce_text = "Build your own station... or just shoot each other!"
	
	allow_persistence_save = FALSE

/datum/game_mode/sandbox/pre_setup()
	for(var/mob/M in GLOB.player_list)
		M.CanBuild()
	return 1

/datum/game_mode/sandbox/post_setup()
	..()
	SSshuttle.registerHostileEnvironment(src)

/datum/game_mode/sandbox/generate_report()
	return "Sensors indicate that crewmembers have been all given psychic powers from which they can manifest various objects.<br><br>This can only end poorly."
