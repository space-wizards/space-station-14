/datum/computer_file/program/card_mod
	filename = "cardmod"
	filedesc = "ID Card Modification"
	program_icon_state = "id"
	extended_desc = "Program for programming employee ID cards to access parts of the station."
	transfer_access = ACCESS_HEADS
	requires_ntnet = 0
	size = 8
	tgui_id = "ntos_card"
	ui_x = 600
	ui_y = 700

	var/mod_mode = 1
	var/is_centcom = 0
	var/show_assignments = 0
	var/minor = 0
	var/authenticated = 0
	var/list/reg_ids = list()
	var/list/region_access = null
	var/list/head_subordinates = null
	var/target_dept = 0 //Which department this computer has access to. 0=all departments
	var/change_position_cooldown = 30
	//Jobs you cannot open new positions for
	var/list/blacklisted = list(
		"AI",
		"Assistant",
		"Cyborg",
		"Captain",
		"Head of Personnel",
		"Head of Security",
		"Chief Engineer",
		"Research Director",
		"Chief Medical Officer")

	//The scaling factor of max total positions in relation to the total amount of people on board the station in %
	var/max_relative_positions = 30 //30%: Seems reasonable, limit of 6 @ 20 players

	//This is used to keep track of opened positions for jobs to allow instant closing
	//Assoc array: "JobName" = (int)<Opened Positions>
	var/list/opened_positions = list();

/datum/computer_file/program/card_mod/New()
	..()
	addtimer(CALLBACK(src, .proc/SetConfigCooldown), 0)

/datum/computer_file/program/card_mod/proc/SetConfigCooldown()
	change_position_cooldown = CONFIG_GET(number/id_console_jobslot_delay)

/datum/computer_file/program/card_mod/event_idremoved(background, slot)
	if(!slot || slot == 2)// slot being false means both are removed
		minor = 0
		authenticated = 0
		head_subordinates = null
		region_access = null


/datum/computer_file/program/card_mod/proc/job_blacklisted(jobtitle)
	return (jobtitle in blacklisted)


//Logic check for if you can open the job
/datum/computer_file/program/card_mod/proc/can_open_job(datum/job/job)
	if(job)
		if(!job_blacklisted(job.title))
			if((job.total_positions <= GLOB.player_list.len * (max_relative_positions / 100)))
				var/delta = (world.time / 10) - GLOB.time_last_changed_position
				if((change_position_cooldown < delta) || (opened_positions[job.title] < 0))
					return 1
				return -2
			return 0
	return 0

//Logic check for if you can close the job
/datum/computer_file/program/card_mod/proc/can_close_job(datum/job/job)
	if(job)
		if(!job_blacklisted(job.title))
			if(job.total_positions > job.current_positions)
				var/delta = (world.time / 10) - GLOB.time_last_changed_position
				if((change_position_cooldown < delta) || (opened_positions[job.title] > 0))
					return 1
				return -2
			return 0
	return 0

/datum/computer_file/program/card_mod/proc/format_jobs(list/jobs)
	var/obj/item/computer_hardware/card_slot/card_slot = computer.all_components[MC_CARD]
	var/obj/item/card/id/id_card = card_slot.stored_card
	var/list/formatted = list()
	for(var/job in jobs)
		formatted.Add(list(list(
			"display_name" = replacetext(job, "&nbsp", " "),
			"target_rank" = id_card && id_card.assignment ? id_card.assignment : "Unassigned",
			"job" = job)))

	return formatted

/datum/computer_file/program/card_mod/ui_act(action, params)
	if(..())
		return 1

	var/obj/item/computer_hardware/card_slot/card_slot
	var/obj/item/computer_hardware/printer/printer
	if(computer)
		card_slot = computer.all_components[MC_CARD]
		printer = computer.all_components[MC_PRINT]
		if(!card_slot)
			return

	var/obj/item/card/id/user_id_card = null
	var/mob/user = usr

	var/obj/item/card/id/id_card = card_slot.stored_card
	var/obj/item/card/id/auth_card = card_slot.stored_card2

	if(auth_card)
		user_id_card = auth_card
	else
		if(ishuman(user))
			var/mob/living/carbon/human/h = user
			user_id_card = h.get_idcard(TRUE)

	switch(action)
		if("PRG_switchm")
			if(params["target"] == "mod")
				mod_mode = 1
			else if (params["target"] == "manifest")
				mod_mode = 0
			else if (params["target"] == "manage")
				mod_mode = 2
		if("PRG_togglea")
			if(show_assignments)
				show_assignments = 0
			else
				show_assignments = 1
		if("PRG_print")
			if(computer && printer) //This option should never be called if there is no printer
				if(mod_mode)
					if(authorized())
						var/contents = {"<h4>Access Report</h4>
									<u>Prepared By:</u> [user_id_card && user_id_card.registered_name ? user_id_card.registered_name : "Unknown"]<br>
									<u>For:</u> [id_card.registered_name ? id_card.registered_name : "Unregistered"]<br>
									<hr>
									<u>Assignment:</u> [id_card.assignment]<br>
									<u>Access:</u><br>
								"}

						var/known_access_rights = get_all_accesses()
						for(var/A in id_card.access)
							if(A in known_access_rights)
								contents += "  [get_access_desc(A)]"

						if(!printer.print_text(contents,"access report"))
							to_chat(usr, "<span class='notice'>Hardware error: Printer was unable to print the file. It may be out of paper.</span>")
							return
						else
							computer.visible_message("<span class='notice'>\The [computer] prints out paper.</span>")
				else
					var/contents = {"<h4>Crew Manifest</h4>
									<br>
									[GLOB.data_core ? GLOB.data_core.get_manifest(0) : ""]
									"}
					if(!printer.print_text(contents,text("crew manifest ([])", station_time_timestamp())))
						to_chat(usr, "<span class='notice'>Hardware error: Printer was unable to print the file. It may be out of paper.</span>")
						return
					else
						computer.visible_message("<span class='notice'>\The [computer] prints out paper.</span>")
		if("PRG_eject")
			if(computer && card_slot)
				var/select = params["target"]
				switch(select)
					if("id")
						if(id_card)
							GLOB.data_core.manifest_modify(id_card.registered_name, id_card.assignment)
							card_slot.try_eject(1, user)
						else
							var/obj/item/I = usr.get_active_held_item()
							if (istype(I, /obj/item/card/id))
								if(!usr.transferItemToLoc(I, computer))
									return
								card_slot.stored_card = I
					if("auth")
						if(auth_card)
							if(id_card)
								GLOB.data_core.manifest_modify(id_card.registered_name, id_card.assignment)
							head_subordinates = null
							region_access = null
							authenticated = 0
							minor = 0
							card_slot.try_eject(2, user)
						else
							var/obj/item/I = usr.get_active_held_item()
							if (istype(I, /obj/item/card/id))
								if(!usr.transferItemToLoc(I, computer))
									return
								card_slot.stored_card2 = I
		if("PRG_terminate")
			if(computer && ((id_card.assignment in head_subordinates) || id_card.assignment == "Assistant"))
				id_card.assignment = "Unassigned"
				remove_nt_access(id_card)
				id_card.update_label()

		if("PRG_edit")
			if(computer && authorized())
				if(params["name"])
					var/temp_name = reject_bad_name(input("Enter name.", "Name", id_card.registered_name))
					if(temp_name)
						id_card.registered_name = temp_name
						id_card.update_label()
					else
						computer.visible_message("<span class='notice'>[computer] buzzes rudely.</span>")
				//else if(params["account"])
				//	var/account_num = text2num(input("Enter account number.", "Account", id_card.associated_account_number))
				//	id_card.associated_account_number = account_num
		if("PRG_assign")
			if(computer && authorized() && id_card)
				var/t1 = params["assign_target"]
				if(t1 == "Custom")
					var/temp_t = reject_bad_text(input("Enter a custom job assignment.","Assignment", id_card.assignment), 45)
					//let custom jobs function as an impromptu alt title, mainly for sechuds
					if(temp_t)
						id_card.assignment = temp_t
				else
					var/list/access = list()
					if(is_centcom)
						access = get_centcom_access(t1)
					else
						var/datum/job/jobdatum
						for(var/jobtype in typesof(/datum/job))
							var/datum/job/J = new jobtype
							if(ckey(J.title) == ckey(t1))
								jobdatum = J
								break
						if(!jobdatum)
							to_chat(usr, "<span class='warning'>No log exists for this job: [t1]</span>")
							return

						access = jobdatum.get_access()

					remove_nt_access(id_card)
					apply_access(id_card, access)
					id_card.assignment = t1
					id_card.update_label()

		if("PRG_access")
			if(params["allowed"] && computer && authorized())
				var/access_type = text2num(params["access_target"])
				var/access_allowed = text2num(params["allowed"])
				if(access_type in (is_centcom ? get_all_centcom_access() : get_all_accesses()))
					id_card.access -= access_type
					if(!access_allowed)
						id_card.access += access_type
		if("PRG_open_job")
			var/edit_job_target = params["target"]
			var/datum/job/j = SSjob.GetJob(edit_job_target)
			if(!j)
				return 0
			if(can_open_job(j) != 1)
				return 0
			if(opened_positions[edit_job_target] >= 0)
				GLOB.time_last_changed_position = world.time / 10
			j.total_positions++
			opened_positions[edit_job_target]++
		if("PRG_close_job")
			var/edit_job_target = params["target"]
			var/datum/job/j = SSjob.GetJob(edit_job_target)
			if(!j)
				return 0
			if(can_close_job(j) != 1)
				return 0
			//Allow instant closing without cooldown if a position has been opened before
			if(opened_positions[edit_job_target] <= 0)
				GLOB.time_last_changed_position = world.time / 10
			j.total_positions--
			opened_positions[edit_job_target]--
		if("PRG_regsel")
			if(!reg_ids)
				reg_ids = list()
			var/regsel = text2num(params["region"])
			if(regsel in reg_ids)
				reg_ids -= regsel
			else
				reg_ids += regsel

	return 1

/datum/computer_file/program/card_mod/proc/remove_nt_access(obj/item/card/id/id_card)
	id_card.access -= get_all_accesses()
	id_card.access -= get_all_centcom_access()

/datum/computer_file/program/card_mod/proc/apply_access(obj/item/card/id/id_card, list/accesses)
	id_card.access |= accesses

/datum/computer_file/program/card_mod/ui_data(mob/user)

	var/list/data = get_header_data()

	var/obj/item/computer_hardware/card_slot/card_slot
	var/obj/item/computer_hardware/printer/printer

	if(computer)
		card_slot = computer.all_components[MC_CARD]
		printer = computer.all_components[MC_PRINT]

	data["mmode"] = mod_mode

	var/authed = 0
	if(computer)
		if(card_slot)
			var/obj/item/card/id/auth_card = card_slot.stored_card2
			data["auth_name"] = auth_card ? strip_html_simple(auth_card.name) : "-----"
			authed = authorized()


	if(mod_mode == 2)
		data["slots"] = list()
		var/list/pos = list()
		for(var/datum/job/job in SSjob.occupations)
			if(job.title in blacklisted)
				continue

			var/list/status_open = build_manage(job,1)
			var/list/status_close = build_manage(job,0)

			pos.Add(list(list(
				"title" = job.title,
				"current" = job.current_positions,
				"total" = job.total_positions,
				"status_open" = (authed && !minor) ? status_open["enable"]: 0,
				"status_close" = (authed && !minor) ? status_close["enable"] : 0,
				"desc_open" = status_open["desc"],
				"desc_close" = status_close["desc"])))
		data["slots"] = pos

	data["src"] = "[REF(src)]"
	data["station_name"] = station_name()


	if(!mod_mode)
		data["manifest"] = list()
		var/list/crew = list()
		for(var/datum/data/record/t in sortRecord(GLOB.data_core.general))
			crew.Add(list(list(
				"name" = t.fields["name"],
				"rank" = t.fields["rank"])))

		data["manifest"] = crew
	data["assignments"] = show_assignments
	if(computer)
		data["have_id_slot"] = !!card_slot
		data["have_printer"] = !!printer
		if(!card_slot && mod_mode == 1)
			mod_mode = 0 //We can't modify IDs when there is no card reader
	else
		data["have_id_slot"] = 0
		data["have_printer"] = 0

	data["centcom_access"] = is_centcom


	data["authenticated"] = authed


	if(mod_mode == 1 && computer)
		if(card_slot)
			var/obj/item/card/id/id_card = card_slot.stored_card

			data["has_id"] = !!id_card
			data["id_rank"] = id_card && id_card.assignment ? html_encode(id_card.assignment) : "Unassigned"
			data["id_owner"] = id_card && id_card.registered_name ? html_encode(id_card.registered_name) : "-----"
			data["id_name"] = id_card ? strip_html_simple(id_card.name) : "-----"

			if(show_assignments)
				data["engineering_jobs"] = format_jobs(GLOB.engineering_positions)
				data["medical_jobs"] = format_jobs(GLOB.medical_positions)
				data["science_jobs"] = format_jobs(GLOB.science_positions)
				data["security_jobs"] = format_jobs(GLOB.security_positions)
				data["cargo_jobs"] = format_jobs(GLOB.supply_positions)
				data["civilian_jobs"] = format_jobs(GLOB.civilian_positions)
				data["centcom_jobs"] = format_jobs(get_all_centcom_jobs())


		if(card_slot.stored_card)
			var/obj/item/card/id/id_card = card_slot.stored_card
			if(is_centcom)
				var/list/all_centcom_access = list()
				for(var/access in get_all_centcom_access())
					all_centcom_access.Add(list(list(
						"desc" = replacetext(get_centcom_access_desc(access), "&nbsp", " "),
						"ref" = access,
						"allowed" = (access in id_card.access) ? 1 : 0)))
				data["all_centcom_access"] = all_centcom_access
			else
				var/list/regions = list()
				for(var/i = 1; i <= 7; i++)
					if((minor || target_dept) && !(i in region_access))
						continue

					var/list/accesses = list()
					if(i in reg_ids)
						for(var/access in get_region_accesses(i))
							if (get_access_desc(access))
								accesses.Add(list(list(
								"desc" = replacetext(get_access_desc(access), "&nbsp", " "),
								"ref" = access,
								"allowed" = (access in id_card.access) ? 1 : 0)))

					regions.Add(list(list(
						"name" = get_region_accesses_name(i),
						"regid" = i,
						"selected" = (i in reg_ids) ? 1 : null,
						"accesses" = accesses)))
				data["regions"] = regions

	data["minor"] = target_dept || minor ? 1 : 0


	return data


/datum/computer_file/program/card_mod/proc/build_manage(datum/job,open = FALSE)
	var/out = "Denied"
	var/can_change= 0
	if(open)
		can_change = can_open_job(job)
	else
		can_change = can_close_job(job)
	var/enable = 0
	if(can_change == 1)
		out = "[open ? "Open Position" : "Close Position"]"
		enable = 1
	else if(can_change == -2)
		var/time_to_wait = round(change_position_cooldown - ((world.time / 10) - GLOB.time_last_changed_position), 1)
		var/mins = round(time_to_wait / 60)
		var/seconds = time_to_wait - (60*mins)
		out = "Cooldown ongoing: [mins]:[(seconds < 10) ? "0[seconds]" : "[seconds]"]"
	else
		out = "Denied"

	return list("enable" = enable, "desc" = out)


/datum/computer_file/program/card_mod/proc/authorized()
	if(!authenticated && computer)
		var/obj/item/computer_hardware/card_slot/card_slot = computer.all_components[MC_CARD]
		if(card_slot)
			var/obj/item/card/id/auth_card = card_slot.stored_card2
			if(auth_card)
				region_access = list()
				if(ACCESS_CHANGE_IDS in auth_card.GetAccess())
					minor = 0
					authenticated = 1
					return 1
				else
					if((ACCESS_HOP in auth_card.access) && ((target_dept==1) || !target_dept))
						region_access |= 1
						region_access |= 6
						get_subordinates("Head of Personnel")
					if((ACCESS_HOS in auth_card.access) && ((target_dept==2) || !target_dept))
						region_access |= 2
						get_subordinates("Head of Security")
					if((ACCESS_CMO in auth_card.access) && ((target_dept==3) || !target_dept))
						region_access |= 3
						get_subordinates("Chief Medical Officer")
					if((ACCESS_RD in auth_card.access) && ((target_dept==4) || !target_dept))
						region_access |= 4
						get_subordinates("Research Director")
					if((ACCESS_CE in auth_card.access) && ((target_dept==5) || !target_dept))
						region_access |= 5
						get_subordinates("Chief Engineer")
					if(region_access.len)
						minor = 1
						authenticated = 1
						return 1
	else
		return authenticated

/datum/computer_file/program/card_mod/proc/get_subordinates(rank)
	head_subordinates = list()
	for(var/datum/job/job in SSjob.occupations)
		if(rank in job.department_head)
			head_subordinates += job.title
