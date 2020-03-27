//computer that handle the points and teleports the prisoner
/obj/machinery/computer/prisoner/gulag_teleporter_computer
	name = "labor camp teleporter console"
	desc = "Used to send criminals to the Labor Camp."
	icon_screen = "explosive"
	icon_keyboard = "security_key"
	req_access = list(ACCESS_ARMORY)
	circuit = /obj/item/circuitboard/computer/gulag_teleporter_console
	ui_x = 350
	ui_y = 295

	var/default_goal = 200
	var/obj/machinery/gulag_teleporter/teleporter = null
	var/obj/structure/gulag_beacon/beacon = null
	var/mob/living/carbon/human/prisoner = null
	var/datum/data/record/temporary_record = null

	light_color = LIGHT_COLOR_RED

/obj/machinery/computer/prisoner/gulag_teleporter_computer/Initialize()
	. = ..()
	scan_machinery()

/obj/machinery/computer/prisoner/gulag_teleporter_computer/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
									datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "gulag_console", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/computer/prisoner/gulag_teleporter_computer/ui_data(mob/user)
	var/list/data = list()

	var/list/prisoner_list = list()
	var/can_teleport = FALSE

	if(teleporter && (teleporter.occupant && ishuman(teleporter.occupant)))
		prisoner = teleporter.occupant
		prisoner_list["name"] = prisoner.real_name
		if(contained_id)
			can_teleport = TRUE
		if(!isnull(GLOB.data_core.general))
			for(var/r in GLOB.data_core.security)
				var/datum/data/record/R = r
				if(R.fields["name"] == prisoner_list["name"])
					temporary_record = R
					prisoner_list["crimstat"] = temporary_record.fields["criminal"]

	data["prisoner"] = prisoner_list

	if(teleporter)
		data["teleporter"] = teleporter
		data["teleporter_location"] = "([teleporter.x], [teleporter.y], [teleporter.z])"
		data["teleporter_lock"] = teleporter.locked
		data["teleporter_state_open"] = teleporter.state_open
	else
		data["teleporter"] = null
	if(beacon)
		data["beacon"] = beacon
		data["beacon_location"] = "([beacon.x], [beacon.y], [beacon.z])"
	else
		data["beacon"] = null
	if(contained_id)
		data["id"] = contained_id
		data["id_name"] = contained_id.registered_name
		data["goal"] = contained_id.goal
	else
		data["id"] = null
	data["can_teleport"] = can_teleport

	return data

/obj/machinery/computer/prisoner/gulag_teleporter_computer/ui_act(action, list/params)
	if(isliving(usr))
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
	if(..())
		return
	if(!allowed(usr))
		to_chat(usr, "<span class='warning'>Access denied.</span>")
		return
	switch(action)
		if("scan_teleporter")
			teleporter = findteleporter()
			return TRUE
		if("scan_beacon")
			beacon = findbeacon()
			return TRUE
		if("handle_id")
			if(contained_id)
				id_eject(usr)
			else
				id_insert(usr)
			return TRUE
		if("set_goal")
			var/new_goal = text2num(params["value"])
			if(!isnum(new_goal))
				return
			if(!new_goal)
				new_goal = default_goal
			contained_id.goal = CLAMP(new_goal, 0, 1000) //maximum 1000 points
			return TRUE
		if("toggle_open")
			if(teleporter.locked)
				to_chat(usr, "<span class='alert'>The teleporter must be unlocked first.</span>")
				return
			teleporter.toggle_open()
			return TRUE
		if("teleporter_lock")
			if(teleporter.state_open)
				to_chat(usr, "<span class='alert'>The teleporter must be closed first.</span>")
				return
			teleporter.locked = !teleporter.locked
			return TRUE
		if("teleport")
			if(!teleporter || !beacon)
				return
			addtimer(CALLBACK(src, .proc/teleport, usr), 5)
			return TRUE

/obj/machinery/computer/prisoner/gulag_teleporter_computer/proc/scan_machinery()
	teleporter = findteleporter()
	beacon = findbeacon()

/obj/machinery/computer/prisoner/gulag_teleporter_computer/proc/findteleporter()
	var/obj/machinery/gulag_teleporter/teleporterf = null

	for(var/direction in GLOB.cardinals)
		teleporterf = locate(/obj/machinery/gulag_teleporter, get_step(src, direction))
		if(teleporterf && teleporterf.is_operational())
			return teleporterf

/obj/machinery/computer/prisoner/gulag_teleporter_computer/proc/findbeacon()
	return locate(/obj/structure/gulag_beacon)

/obj/machinery/computer/prisoner/gulag_teleporter_computer/proc/teleport(mob/user)
	if(!contained_id) //incase the ID was removed after the transfer timer was set.
		say("Warning: Unable to transfer prisoner without a valid Prisoner ID inserted!")
		return
	var/id_goal_not_set
	if(!contained_id.goal)
		id_goal_not_set = TRUE
		contained_id.goal = default_goal
		say("[contained_id]'s ID card goal defaulting to [contained_id.goal] points.")
	log_game("[key_name(user)] teleported [key_name(prisoner)] to the Labor Camp [COORD(beacon)] for [id_goal_not_set ? "default goal of ":""][contained_id.goal] points.")
	teleporter.handle_prisoner(contained_id, temporary_record)
	playsound(src, 'sound/weapons/emitter.ogg', 50, TRUE)
	prisoner.forceMove(get_turf(beacon))
	prisoner.Paralyze(40) // small travel dizziness
	to_chat(prisoner, "<span class='warning'>The teleportation makes you a little dizzy.</span>")
	new /obj/effect/particle_effect/sparks(get_turf(prisoner))
	playsound(src, "sparks", 50, TRUE)
	if(teleporter.locked)
		teleporter.locked = FALSE
	teleporter.toggle_open()
	contained_id = null
	temporary_record = null
