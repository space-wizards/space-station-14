/client/proc/forcerandomrotate()
	set category = "Server"
	set name = "Trigger Random Map Rotation"
	var/rotate = alert("Force a random map rotation to trigger?", "Rotate map?", "Yes", "Cancel")
	if (rotate != "Yes")
		return
	message_admins("[key_name_admin(usr)] is forcing a random map rotation.")
	log_admin("[key_name(usr)] is forcing a random map rotation.")
	SSmapping.maprotate()

/client/proc/adminchangemap()
	set category = "Server"
	set name = "Change Map"
	var/list/maprotatechoices = list()
	for (var/map in config.maplist)
		var/datum/map_config/VM = config.maplist[map]
		var/mapname = VM.map_name
		if (VM == config.defaultmap)
			mapname += " (Default)"

		if (VM.config_min_users > 0 || VM.config_max_users > 0)
			mapname += " \["
			if (VM.config_min_users > 0)
				mapname += "[VM.config_min_users]"
			else
				mapname += "0"
			mapname += "-"
			if (VM.config_max_users > 0)
				mapname += "[VM.config_max_users]"
			else
				mapname += "inf"
			mapname += "\]"

		maprotatechoices[mapname] = VM
	var/chosenmap = input("Choose a map to change to", "Change Map")  as null|anything in sortList(maprotatechoices)|"Custom"
	if (!chosenmap)
		return

	if(chosenmap == "Custom")
		message_admins("[key_name_admin(usr)] is changing the map to a custom map")
		log_admin("[key_name(usr)] is changing the map to a custom map")
		var/datum/map_config/VM = new

		VM.map_name = input("Choose the name for the map", "Map Name") as null|text
		if(isnull(VM.map_name))
			VM.map_name = "Custom"

		var/map_file = input("Pick file:", "Map File") as null|file
		if(isnull(map_file))
			return

		if(copytext("[map_file]", -4) != ".dmm")//4 == length(".dmm")
			to_chat(src, "<span class='warning'>Filename must end in '.dmm': [map_file]</span>")
			return

		if(!fcopy(map_file, "_maps/custom/[map_file]"))
			return

		// This is to make sure the map works so the server does not start without a map.
		var/datum/parsed_map/M = new (map_file)
		if(!M)
			to_chat(src, "<span class='warning'>Map '[map_file]' failed to parse properly.</span>")
			return

		if(!M.bounds)
			to_chat(src, "<span class='warning'>Map '[map_file]' has non-existant bounds.</span>")
			qdel(M)
			return

		qdel(M)

		var/shuttles = alert("Do you want to modify the shuttles?", "Map Shuttles", "Yes", "No")
		if(shuttles == "Yes")
			for(var/s in VM.shuttles)
				var/shuttle = input(s, "Map Shuttles") as null|text
				if(!shuttle)
					continue
				if(!SSmapping.shuttle_templates[shuttle])
					to_chat(usr, "<span class='warning'>No such shuttle as '[shuttle]' exists, using default.</span>")
					continue
				VM.shuttles[s] = shuttle

		VM.map_path = "custom"
		VM.map_file = "[map_file]"
		VM.config_filename = "data/next_map.json"
		var/json_value = list(
			"map_name" = VM.map_name,
			"map_path" = VM.map_path,
			"map_file" = VM.map_file,
			"shuttles" = VM.shuttles
		)

		// If the file isn't removed text2file will just append.
		if(fexists("data/next_map.json"))
			fdel("data/next_map.json")
		text2file(json_encode(json_value), "data/next_map.json")

		if(SSmapping.changemap(VM))
			message_admins("[key_name_admin(usr)] has changed the map to [VM.map_name]")
	else
		var/datum/map_config/VM = maprotatechoices[chosenmap]
		message_admins("[key_name_admin(usr)] is changing the map to [VM.map_name]")
		log_admin("[key_name(usr)] is changing the map to [VM.map_name]")
		if (SSmapping.changemap(VM))
			message_admins("[key_name_admin(usr)] has changed the map to [VM.map_name]")
