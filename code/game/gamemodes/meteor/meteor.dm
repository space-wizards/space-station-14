/datum/game_mode/meteor
	name = "meteor"
	config_tag = "meteor"
	report_type = "meteor"
	false_report_weight = 1
	var/meteordelay = 2000
	var/nometeors = 0
	var/rampupdelta = 5
	required_players = 0

	announce_span = "danger"
	announce_text = "A major meteor shower is bombarding the station! The crew needs to evacuate or survive the onslaught."


/datum/game_mode/meteor/process()
	if(nometeors || meteordelay > world.time - SSticker.round_start_time)
		return

	var/list/wavetype = GLOB.meteors_normal
	var/meteorminutes = (world.time - SSticker.round_start_time - meteordelay) / 10 / 60


	if (prob(meteorminutes))
		wavetype = GLOB.meteors_threatening

	if (prob(meteorminutes/2))
		wavetype = GLOB.meteors_catastrophic

	var/ramp_up_final = CLAMP(round(meteorminutes/rampupdelta), 1, 10)

	spawn_meteors(ramp_up_final, wavetype)


/datum/game_mode/meteor/special_report()
	var/survivors = 0
	var/list/survivor_list = list()

	for(var/mob/living/player in GLOB.player_list)
		if(player.stat != DEAD)
			++survivors

			if(player.onCentCom())
				survivor_list += "<span class='greentext'>[player.real_name] escaped to the safety of CentCom.</span>"
			else if(player.onSyndieBase())
				survivor_list += "<span class='greentext'>[player.real_name] escaped to the (relative) safety of Syndicate Space.</span>"
			else
				survivor_list += "<span class='neutraltext'>[player.real_name] survived but is stranded without any hope of rescue.</span>"

	if(survivors)
		return "<div class='panel greenborder'><span class='header'>The following survived the meteor storm:</span><br>[survivor_list.Join("<br>")]</div>"
	else
		return "<div class='panel redborder'><span class='redtext big'>Nobody survived the meteor storm!</span></div>"

/datum/game_mode/meteor/set_round_result()
	..()
	SSticker.mode_result = "end - evacuation"

/datum/game_mode/meteor/generate_report()
	return "[pick("Asteroids have", "Meteors have", "Large rocks have", "Stellar minerals have", "Space hail has", "Debris has")] been detected near your station, and a collision is possible, \
			though unlikely.  Be prepared for largescale impacts and destruction.  Please note that the debris will prevent the escape shuttle from arriving quickly."
