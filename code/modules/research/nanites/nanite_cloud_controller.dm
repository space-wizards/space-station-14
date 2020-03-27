/obj/machinery/computer/nanite_cloud_controller
	name = "nanite cloud controller"
	desc = "Stores and controls nanite cloud backups."
	icon = 'icons/obj/machines/research.dmi'
	icon_state = "nanite_cloud_controller"
	circuit = /obj/item/circuitboard/computer/nanite_cloud_controller
	ui_x = 375
	ui_y = 700

	var/obj/item/disk/nanite_program/disk
	var/list/datum/nanite_cloud_backup/cloud_backups = list()
	var/current_view = 0 //0 is the main menu, any other number is the page of the backup with that ID
	var/new_backup_id = 1

/obj/machinery/computer/nanite_cloud_controller/Destroy()
	QDEL_LIST(cloud_backups) //rip backups
	eject()
	return ..()

/obj/machinery/computer/nanite_cloud_controller/attackby(obj/item/I, mob/user)
	if(istype(I, /obj/item/disk/nanite_program))
		var/obj/item/disk/nanite_program/N = I
		if(disk)
			eject(user)
		if(user.transferItemToLoc(N, src))
			to_chat(user, "<span class='notice'>You insert [N] into [src].</span>")
			playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
			disk = N
	else
		..()

/obj/machinery/computer/nanite_cloud_controller/proc/eject(mob/living/user)
	if(!disk)
		return
	if(!istype(user) || !Adjacent(user) ||!user.put_in_active_hand(disk))
		disk.forceMove(drop_location())
	disk = null

/obj/machinery/computer/nanite_cloud_controller/proc/get_backup(cloud_id)
	for(var/I in cloud_backups)
		var/datum/nanite_cloud_backup/backup = I
		if(backup.cloud_id == cloud_id)
			return backup

/obj/machinery/computer/nanite_cloud_controller/proc/generate_backup(cloud_id, mob/user)
	if(SSnanites.get_cloud_backup(cloud_id, TRUE))
		to_chat(user, "<span class='warning'>Cloud ID already registered.</span>")
		return

	var/datum/nanite_cloud_backup/backup = new(src)
	var/datum/component/nanites/cloud_copy = new(backup)
	backup.cloud_id = cloud_id
	backup.nanites = cloud_copy
	investigate_log("[key_name(user)] created a new nanite cloud backup with id #[cloud_id]", INVESTIGATE_NANITES)

/obj/machinery/computer/nanite_cloud_controller/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "nanite_cloud_control", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/computer/nanite_cloud_controller/ui_data()
	var/list/data = list()

	if(disk)
		data["has_disk"] = TRUE
		var/list/disk_data = list()
		var/datum/nanite_program/P = disk.program
		if(P)
			data["has_program"] = TRUE
			disk_data["name"] = P.name
			disk_data["desc"] = P.desc
			disk_data["use_rate"] = P.use_rate
			disk_data["can_trigger"] = P.can_trigger
			disk_data["trigger_cost"] = P.trigger_cost
			disk_data["trigger_cooldown"] = P.trigger_cooldown / 10

			disk_data["activated"] = P.activated
			disk_data["activation_code"] = P.activation_code
			disk_data["deactivation_code"] = P.deactivation_code
			disk_data["kill_code"] = P.kill_code
			disk_data["trigger_code"] = P.trigger_code
			disk_data["timer_restart"] = P.timer_restart / 10
			disk_data["timer_shutdown"] = P.timer_shutdown / 10
			disk_data["timer_trigger"] = P.timer_trigger / 10
			disk_data["timer_trigger_delay"] = P.timer_trigger_delay / 10

			var/list/extra_settings = P.get_extra_settings_frontend()
			disk_data["extra_settings"] = extra_settings
			if(LAZYLEN(extra_settings))
				disk_data["has_extra_settings"] = TRUE
			if(istype(P, /datum/nanite_program/sensor))
				var/datum/nanite_program/sensor/sensor = P
				if(sensor.can_rule)
					disk_data["can_rule"] = TRUE
		data["disk"] = disk_data
	else
		data["has_disk"] = FALSE

	data["new_backup_id"] = new_backup_id

	data["current_view"] = current_view
	if(current_view)
		var/datum/nanite_cloud_backup/backup = get_backup(current_view)
		if(backup)
			var/datum/component/nanites/nanites = backup.nanites
			data["cloud_backup"] = TRUE
			var/list/cloud_programs = list()
			var/id = 1
			for(var/datum/nanite_program/P in nanites.programs)
				var/list/cloud_program = list()
				cloud_program["name"] = P.name
				cloud_program["desc"] = P.desc
				cloud_program["id"] = id
				cloud_program["use_rate"] = P.use_rate
				cloud_program["can_trigger"] = P.can_trigger
				cloud_program["trigger_cost"] = P.trigger_cost
				cloud_program["trigger_cooldown"] = P.trigger_cooldown / 10
				cloud_program["activated"] = P.activated
				cloud_program["timer_restart"] = P.timer_restart / 10
				cloud_program["timer_shutdown"] = P.timer_shutdown / 10
				cloud_program["timer_trigger"] = P.timer_trigger / 10
				cloud_program["timer_trigger_delay"] = P.timer_trigger_delay / 10

				cloud_program["activation_code"] = P.activation_code
				cloud_program["deactivation_code"] = P.deactivation_code
				cloud_program["kill_code"] = P.kill_code
				cloud_program["trigger_code"] = P.trigger_code
				var/list/rules = list()
				var/rule_id = 1
				for(var/X in P.rules)
					var/datum/nanite_rule/nanite_rule = X
					var/list/rule = list()
					rule["display"] = nanite_rule.display()
					rule["program_id"] = id
					rule["id"] = rule_id
					rules += list(rule)
					rule_id++
				cloud_program["rules"] = rules
				if(LAZYLEN(rules))
					cloud_program["has_rules"] = TRUE

				var/list/extra_settings = P.get_extra_settings_frontend()
				cloud_program["extra_settings"] = extra_settings
				if(LAZYLEN(extra_settings))
					cloud_program["has_extra_settings"] = TRUE
				id++
				cloud_programs += list(cloud_program)
			data["cloud_programs"] = cloud_programs
	else
		var/list/backup_list = list()
		for(var/X in cloud_backups)
			var/datum/nanite_cloud_backup/backup = X
			var/list/cloud_backup = list()
			cloud_backup["cloud_id"] = backup.cloud_id
			backup_list += list(cloud_backup)
		data["cloud_backups"] = backup_list
	return data

/obj/machinery/computer/nanite_cloud_controller/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("eject")
			eject(usr)
			. = TRUE
		if("set_view")
			current_view = text2num(params["view"])
			. = TRUE
		if("update_new_backup_value")
			var/backup_value = text2num(params["value"])
			new_backup_id = backup_value
		if("create_backup")
			var/cloud_id = new_backup_id
			if(!isnull(cloud_id))
				playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
				cloud_id = CLAMP(round(cloud_id, 1),1,100)
				generate_backup(cloud_id, usr)
			. = TRUE
		if("delete_backup")
			var/datum/nanite_cloud_backup/backup = get_backup(current_view)
			if(backup)
				playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
				qdel(backup)
				investigate_log("[key_name(usr)] deleted the nanite cloud backup #[current_view]", INVESTIGATE_NANITES)
			. = TRUE
		if("upload_program")
			if(disk && disk.program)
				var/datum/nanite_cloud_backup/backup = get_backup(current_view)
				if(backup)
					playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
					var/datum/component/nanites/nanites = backup.nanites
					nanites.add_program(null, disk.program.copy())
					investigate_log("[key_name(usr)] uploaded program [disk.program.name] to cloud #[current_view]", INVESTIGATE_NANITES)
			. = TRUE
		if("remove_program")
			var/datum/nanite_cloud_backup/backup = get_backup(current_view)
			if(backup)
				playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
				var/datum/component/nanites/nanites = backup.nanites
				var/datum/nanite_program/P = nanites.programs[text2num(params["program_id"])]
				investigate_log("[key_name(usr)] deleted program [P.name] from cloud #[current_view]", INVESTIGATE_NANITES)
				qdel(P)
			. = TRUE
		if("add_rule")
			if(disk && disk.program && istype(disk.program, /datum/nanite_program/sensor))
				var/datum/nanite_program/sensor/rule_template = disk.program
				if(!rule_template.can_rule)
					return
				var/datum/nanite_cloud_backup/backup = get_backup(current_view)
				if(backup)
					playsound(src, 'sound/machines/terminal_prompt.ogg', 50, 0)
					var/datum/component/nanites/nanites = backup.nanites
					var/datum/nanite_program/P = nanites.programs[text2num(params["program_id"])]
					var/datum/nanite_rule/rule = rule_template.make_rule(P)
					
					investigate_log("[key_name(usr)] added rule [rule.display()] to program [P.name] in cloud #[current_view]", INVESTIGATE_NANITES)
			. = TRUE
		if("remove_rule")
			var/datum/nanite_cloud_backup/backup = get_backup(current_view)
			if(backup)
				playsound(src, 'sound/machines/terminal_prompt.ogg', 50, 0)
				var/datum/component/nanites/nanites = backup.nanites
				var/datum/nanite_program/P = nanites.programs[text2num(params["program_id"])]
				var/datum/nanite_rule/rule = P.rules[text2num(params["rule_id"])]
				rule.remove()
				
				investigate_log("[key_name(usr)] removed rule [rule.display()] from program [P.name] in cloud #[current_view]", INVESTIGATE_NANITES)
			. = TRUE

/datum/nanite_cloud_backup
	var/cloud_id = 0
	var/datum/component/nanites/nanites
	var/obj/machinery/computer/nanite_cloud_controller/storage

/datum/nanite_cloud_backup/New(obj/machinery/computer/nanite_cloud_controller/_storage)
	storage = _storage
	storage.cloud_backups += src
	SSnanites.cloud_backups += src

/datum/nanite_cloud_backup/Destroy()
	storage.cloud_backups -= src
	SSnanites.cloud_backups -= src
	return ..()
