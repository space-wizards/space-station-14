/datum/round_event_control/falsealarm
	name 			= "False Alarm"
	typepath 		= /datum/round_event/falsealarm
	weight			= 20
	max_occurrences = 5
	var/forced_type //Admin abuse


/datum/round_event_control/falsealarm/admin_setup()
	if(!check_rights(R_FUN))
		return

	var/list/possible_types = list()

	for(var/datum/round_event_control/E in SSevents.control)
		var/datum/round_event/event = E.typepath
		if(!initial(event.fakeable))
			continue
		possible_types += E

	forced_type = input(usr, "Select the scare.","False event") as null|anything in sortNames(possible_types)

/datum/round_event_control/falsealarm/canSpawnEvent(players_amt, gamemode)
	return ..() && length(gather_false_events())

/datum/round_event/falsealarm
	announceWhen	= 0
	endWhen			= 1
	fakeable = FALSE

/datum/round_event/falsealarm/announce(fake)
	if(fake) //What are you doing
		return
	var/players_amt = get_active_player_count(alive_check = 1, afk_check = 1, human_check = 1)
	var/gamemode = SSticker.mode.config_tag

	var/events_list = gather_false_events(players_amt, gamemode)
	var/datum/round_event_control/event_control
	var/datum/round_event_control/falsealarm/C = control
	if(C.forced_type)
		event_control = C.forced_type
		C.forced_type = null
	else
		event_control = pick(events_list)
	if(event_control)
		var/datum/round_event/Event = new event_control.typepath()
		message_admins("False Alarm: [Event]")
		Event.kill() 		//do not process this event - no starts, no ticks, no ends
		Event.announce(TRUE) 	//just announce it like it's happening

/proc/gather_false_events(players_amt, gamemode)
	. = list()
	for(var/datum/round_event_control/E in SSevents.control)
		if(istype(E, /datum/round_event_control/falsealarm))
			continue
		if(!E.canSpawnEvent(players_amt, gamemode))
			continue

		var/datum/round_event/event = E.typepath
		if(!initial(event.fakeable))
			continue
		. += E
