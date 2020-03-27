

//Keeps track of the time for the ID console. Having it as a global variable prevents people from dismantling/reassembling it to
//increase the slots of many jobs.
GLOBAL_VAR_INIT(time_last_changed_position, 0)

#define JOB_ALLOWED 1
#define JOB_COOLDOWN -2
#define JOB_MAX_POSITIONS -1 // Trying to reduce the number of slots below that of current holders of that job, or trying to open more slots than allowed
#define JOB_DENIED 0

/obj/machinery/computer/card
	name = "identification console"
	desc = "You can use this to manage jobs and ID access."
	icon_screen = "id"
	icon_keyboard = "id_key"
	req_one_access = list(ACCESS_HEADS, ACCESS_CHANGE_IDS)
	circuit = /obj/item/circuitboard/computer/card
	var/mode = 0
	var/printing = null
	var/target_dept = 0 //Which department this computer has access to. 0=all departments

	//Cooldown for closing positions in seconds
	//if set to -1: No cooldown... probably a bad idea
	//if set to 0: Not able to close "original" positions. You can only close positions that you have opened before
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
	var/list/opened_positions = list()
	var/obj/item/card/id/inserted_scan_id
	var/obj/item/card/id/inserted_modify_id
	var/list/region_access = null
	var/list/head_subordinates = null

	light_color = LIGHT_COLOR_BLUE

/obj/machinery/computer/card/proc/get_jobs()
	return get_all_jobs()

/obj/machinery/computer/card/centcom/get_jobs()
	return get_all_centcom_jobs()

/obj/machinery/computer/card/Initialize()
	. = ..()
	change_position_cooldown = CONFIG_GET(number/id_console_jobslot_delay)

/obj/machinery/computer/card/examine(mob/user)
	. = ..()
	if(inserted_scan_id || inserted_modify_id)
		. += "<span class='notice'>Alt-click to eject the ID card.</span>"

/obj/machinery/computer/card/attackby(obj/I, mob/user, params)
	if(isidcard(I))
		if(check_access(I) && !inserted_scan_id)
			if(id_insert(user, I, inserted_scan_id))
				inserted_scan_id = I
				updateUsrDialog()
		else if(id_insert(user, I, inserted_modify_id))
			inserted_modify_id = I
			updateUsrDialog()
	else
		return ..()

/obj/machinery/computer/card/Destroy()
	if(inserted_scan_id)
		qdel(inserted_scan_id)
		inserted_scan_id = null
	if(inserted_modify_id)
		qdel(inserted_modify_id)
		inserted_modify_id = null
	return ..()

/obj/machinery/computer/card/handle_atom_del(atom/A)
	..()
	if(A == inserted_scan_id)
		inserted_scan_id = null
		updateUsrDialog()
	if(A == inserted_modify_id)
		inserted_modify_id = null
		updateUsrDialog()

/obj/machinery/computer/card/on_deconstruction()
	if(inserted_scan_id)
		inserted_scan_id.forceMove(drop_location())
		inserted_scan_id = null
	if(inserted_modify_id)
		inserted_modify_id.forceMove(drop_location())
		inserted_modify_id = null

//Check if you can't open a new position for a certain job
/obj/machinery/computer/card/proc/job_blacklisted(jobtitle)
	return (jobtitle in blacklisted)

//Logic check for Topic() if you can open the job
/obj/machinery/computer/card/proc/can_open_job(datum/job/job)
	if(job)
		if(!job_blacklisted(job.title))
			if((job.total_positions <= GLOB.player_list.len * (max_relative_positions / 100)))
				var/delta = (world.time / 10) - GLOB.time_last_changed_position
				if((change_position_cooldown < delta) || (opened_positions[job.title] < 0))
					return JOB_ALLOWED
				return JOB_COOLDOWN
			return JOB_MAX_POSITIONS
	return JOB_DENIED

//Logic check for Topic() if you can close the job
/obj/machinery/computer/card/proc/can_close_job(datum/job/job)
	if(job)
		if(!job_blacklisted(job.title))
			if(job.total_positions > job.current_positions)
				var/delta = (world.time / 10) - GLOB.time_last_changed_position
				if((change_position_cooldown < delta) || (opened_positions[job.title] > 0))
					return JOB_ALLOWED
				return JOB_COOLDOWN
			return JOB_MAX_POSITIONS
	return JOB_DENIED


/obj/machinery/computer/card/proc/id_insert(mob/user, obj/item/inserting_item, obj/item/target)
	var/obj/item/card/id/card_to_insert = inserting_item
	var/holder_item = FALSE

	if(!isidcard(card_to_insert))
		card_to_insert = inserting_item.RemoveID()
		holder_item = TRUE

	if(!card_to_insert || !user.transferItemToLoc(card_to_insert, src))
		return FALSE

	if(target)
		if(holder_item && inserting_item.InsertID(target))
			playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
		else
			id_eject(user, target)

	user.visible_message("<span class='notice'>[user] inserts \the [card_to_insert] into \the [src].</span>",
						"<span class='notice'>You insert \the [card_to_insert] into \the [src].</span>")
	playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
	updateUsrDialog()
	return TRUE

/obj/machinery/computer/card/proc/id_eject(mob/user, obj/target)
	if(!target)
		to_chat(user, "<span class='warning'>That slot is empty!</span>")
		return FALSE
	else
		target.forceMove(drop_location())
		if(!issilicon(user) && Adjacent(user))
			user.put_in_hands(target)
		user.visible_message("<span class='notice'>[user] gets \the [target] from \the [src].</span>", \
							"<span class='notice'>You get \the [target] from \the [src].</span>")
		playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
		updateUsrDialog()
		return TRUE

/obj/machinery/computer/card/AltClick(mob/user)
	..()
	if(!user.canUseTopic(src, !issilicon(user)) || !is_operational())
		return
	if(inserted_modify_id)
		if(id_eject(user, inserted_modify_id))
			inserted_modify_id = null
			updateUsrDialog()
			return
	if(inserted_scan_id)
		if(id_eject(user, inserted_scan_id))
			inserted_scan_id = null
			updateUsrDialog()
			return

/obj/machinery/computer/card/ui_interact(mob/user)
	. = ..()
	var/list/dat = list()
	if (mode == 1) // accessing crew manifest
		dat += "<tt><b>Crew Manifest:</b><br>Please use security record computer to modify entries.<br><br>"
		for(var/datum/data/record/t in sortRecord(GLOB.data_core.general))
			dat += {"[t.fields["name"]] - [t.fields["rank"]]<br>"}
		dat += "<a href='?src=[REF(src)];choice=print'>Print</a><br><br><a href='?src=[REF(src)];choice=mode;mode_target=0'>Access ID modification console.</a><br></tt>"

	else if(mode == 2)
		// JOB MANAGEMENT
		dat += {"<a href='?src=[REF(src)];choice=return'>Return</a>
		<table><tr><td style='width:25%'><b>Job</b></td><td style='width:25%'><b>Slots</b></td>
		<td style='width:25%'><b>Open job</b></td><td style='width:25%'><b>Close job</b><td style='width:25%'><b>Prioritize</b></td></td></tr>"}
		for(var/datum/job/job in SSjob.occupations)
			dat += "<tr>"
			if(job.title in blacklisted)
				continue
			dat += {"<td>[job.title]</td>
				<td>[job.current_positions]/[job.total_positions]</td>
				<td>"}
			switch(can_open_job(job))
				if(JOB_ALLOWED)
					if(authenticated == 2)
						dat += "<a href='?src=[REF(src)];choice=make_job_available;job=[job.title]'>Open Position</a><br>"
					else
						dat += "Open Position"
				if(JOB_COOLDOWN)
					var/time_to_wait = round(change_position_cooldown - ((world.time / 10) - GLOB.time_last_changed_position), 1)
					var/mins = round(time_to_wait / 60)
					var/seconds = time_to_wait - (60*mins)
					dat += "Cooldown ongoing: [mins]:[(seconds < 10) ? "0[seconds]" : "[seconds]"]"
				else
					dat += "Denied"
			dat += "</td><td>"
			switch(can_close_job(job))
				if(JOB_ALLOWED)
					if(authenticated == 2)
						dat += "<a href='?src=[REF(src)];choice=make_job_unavailable;job=[job.title]'>Close Position</a>"
					else
						dat += "Close Position"
				if(JOB_COOLDOWN)
					var/time_to_wait = round(change_position_cooldown - ((world.time / 10) - GLOB.time_last_changed_position), 1)
					var/mins = round(time_to_wait / 60)
					var/seconds = time_to_wait - (60*mins)
					dat += "Cooldown ongoing: [mins]:[(seconds < 10) ? "0[seconds]" : "[seconds]"]"
				else
					dat += "Denied"
			dat += "</td><td>"
			switch(job.total_positions)
				if(0)
					dat += "Denied"
				else
					if(authenticated == 2)
						if(job in SSjob.prioritized_jobs)
							dat += "<a href='?src=[REF(src)];choice=prioritize_job;job=[job.title]'>Deprioritize</a>"
						else
							if(SSjob.prioritized_jobs.len < 5)
								dat += "<a href='?src=[REF(src)];choice=prioritize_job;job=[job.title]'>Prioritize</a>"
							else
								dat += "Denied"
					else
						dat += "Prioritize"

			dat += "</td></tr>"
		dat += "</table>"
	else
		var/list/header = list()

		var/scan_name = inserted_scan_id ? html_encode(inserted_scan_id.name) : "--------"
		var/target_name = inserted_modify_id ? html_encode(inserted_modify_id.name) : "--------"
		var/target_owner = (inserted_modify_id && inserted_modify_id.registered_name) ? html_encode(inserted_modify_id.registered_name) : "--------"
		var/target_rank = (inserted_modify_id && inserted_modify_id.assignment) ? html_encode(inserted_modify_id.assignment) : "Unassigned"

		if(!authenticated)
			header += {"<br><i>Please insert the cards into the slots</i><br>
				Target: <a href='?src=[REF(src)];choice=inserted_modify_id'>[target_name]</a><br>
				Confirm Identity: <a href='?src=[REF(src)];choice=inserted_scan_id'>[scan_name]</a><br>"}
		else
			header += {"<div align='center'><br>
				Target: <a href='?src=[REF(src)];choice=inserted_modify_id'>Remove [target_name]</a> ||
				Confirm Identity: <a href='?src=[REF(src)];choice=inserted_scan_id'>Remove [scan_name]</a><br>
				<a href='?src=[REF(src)];choice=mode;mode_target=1'>Access Crew Manifest</a><br>
				[!target_dept ? "<a href='?src=[REF(src)];choice=mode;mode_target=2'>Job Management</a><br>" : ""]
				<a href='?src=[REF(src)];choice=logout'>Log Out</a></div>"}

		header += "<hr>"

		var/body

		if (authenticated && inserted_modify_id)
			var/list/carddesc = list()
			var/list/jobs = list()
			if (authenticated == 2)
				var/list/jobs_all = list()
				for(var/job in (list("Unassigned") + get_jobs() + "Custom"))
					jobs_all += "<a href='?src=[REF(src)];choice=assign;assign_target=[job]'>[replacetext(job, " ", "&nbsp;")]</a> " //make sure there isn't a line break in the middle of a job
				carddesc += {"<script type="text/javascript">
									function markRed(){
										var nameField = document.getElementById('namefield');
										nameField.style.backgroundColor = "#FFDDDD";
									}
									function markGreen(){
										var nameField = document.getElementById('namefield');
										nameField.style.backgroundColor = "#DDFFDD";
									}
									function showAll(){
										var allJobsSlot = document.getElementById('alljobsslot');
										allJobsSlot.innerHTML = "<a href='#' onclick='hideAll()'>hide</a><br>"+ "[jobs_all.Join()]";
									}
									function hideAll(){
										var allJobsSlot = document.getElementById('alljobsslot');
										allJobsSlot.innerHTML = "<a href='#' onclick='showAll()'>show</a>";
									}
								</script>"}
				carddesc += {"<form name='cardcomp' action='?src=[REF(src)]' method='get'>
					<input type='hidden' name='src' value='[REF(src)]'>
					<input type='hidden' name='choice' value='reg'>
					<b>registered name:</b> <input type='text' id='namefield' name='reg' value='[target_owner]' style='width:250px; background-color:white;' onchange='markRed()'>
					<input type='submit' value='Rename' onclick='markGreen()'>
					</form>
					<b>Assignment:</b> "}

				jobs += "<span id='alljobsslot'><a href='#' onclick='showAll()'>[target_rank]</a></span>" //CHECK THIS

			else
				carddesc += "<b>registered_name:</b> [target_owner]</span>"
				jobs += "<b>Assignment:</b> [target_rank] (<a href='?src=[REF(src)];choice=demote'>Demote</a>)</span>"

			var/list/accesses = list()
			if(istype(src, /obj/machinery/computer/card/centcom)) // REE
				accesses += "<h5>Central Command:</h5>"
				for(var/A in get_all_centcom_access())
					if(A in inserted_modify_id.access)
						accesses += "<a href='?src=[REF(src)];choice=access;access_target=[A];allowed=0'><font color=\"6bc473\">[replacetext(get_centcom_access_desc(A), " ", "&nbsp")]</font></a> "
					else
						accesses += "<a href='?src=[REF(src)];choice=access;access_target=[A];allowed=1'>[replacetext(get_centcom_access_desc(A), " ", "&nbsp")]</a> "
			else
				accesses += {"<div align='center'><b>Access</b></div>
					<table style='width:100%'>
					<tr>"}
				for(var/i = 1; i <= 7; i++)
					if(authenticated == 1 && !(i in region_access))
						continue
					accesses += "<td style='width:14%'><b>[get_region_accesses_name(i)]:</b></td>"
				accesses += "</tr><tr>"
				for(var/i = 1; i <= 7; i++)
					if(authenticated == 1 && !(i in region_access))
						continue
					accesses += "<td style='width:14%' valign='top'>"
					for(var/A in get_region_accesses(i))
						if(A in inserted_modify_id.access)
							accesses += "<a href='?src=[REF(src)];choice=access;access_target=[A];allowed=0'><font color=\"6bc473\">[replacetext(get_access_desc(A), " ", "&nbsp")]</font></a> "
						else
							accesses += "<a href='?src=[REF(src)];choice=access;access_target=[A];allowed=1'>[replacetext(get_access_desc(A), " ", "&nbsp")]</a> "
						accesses += "<br>"
					accesses += "</td>"
				accesses += "</tr></table>"
			body = "[carddesc.Join()]<br>[jobs.Join()]<br><br>[accesses.Join()]<hr>" //CHECK THIS

		else if (!authenticated)
			body = {"<a href='?src=[REF(src)];choice=auth'>Log In</a><br><hr>
				<a href='?src=[REF(src)];choice=mode;mode_target=1'>Access Crew Manifest</a><br><hr>"}
			if(!target_dept)
				body += "<a href='?src=[REF(src)];choice=mode;mode_target=2'>Job Management</a><hr>"

		dat = list("<tt>", header.Join(), body, "<br></tt>")
	var/datum/browser/popup = new(user, "id_com", src.name, 900, 620)
	popup.set_content(dat.Join())
	popup.set_title_image(user.browse_rsc_icon(src.icon, src.icon_state))
	popup.open()

/obj/machinery/computer/card/Topic(href, href_list)
	if(..())
		return

	if(!usr.canUseTopic(src, !issilicon(usr)) || !is_operational())
		usr.unset_machine()
		usr << browse(null, "window=id_com")
		return

	usr.set_machine(src)
	switch(href_list["choice"])
		if ("inserted_modify_id")
			if(inserted_modify_id && !usr.get_active_held_item())
				if(id_eject(usr, inserted_modify_id))
					inserted_modify_id = null
					updateUsrDialog()
					return
			if(usr.get_id_in_hand())
				var/obj/item/held_item = usr.get_active_held_item()
				var/obj/item/card/id/id_to_insert = held_item.GetID()
				if(id_insert(usr, held_item, inserted_modify_id))
					inserted_modify_id = id_to_insert
					updateUsrDialog()
		if ("inserted_scan_id")
			if(inserted_scan_id && !usr.get_active_held_item())
				if(id_eject(usr, inserted_scan_id))
					inserted_scan_id = null
					updateUsrDialog()
					return
			if(usr.get_id_in_hand())
				var/obj/item/held_item = usr.get_active_held_item()
				var/obj/item/card/id/id_to_insert = held_item.GetID()
				if(id_insert(usr, held_item, inserted_scan_id))
					inserted_scan_id = id_to_insert
					updateUsrDialog()
		if ("auth")
			if ((!( authenticated ) && (inserted_scan_id || issilicon(usr)) || mode))
				if (check_access(inserted_scan_id))
					region_access = list()
					head_subordinates = list()
					if(ACCESS_CHANGE_IDS in inserted_scan_id.access)
						if(target_dept)
							head_subordinates = get_all_jobs()
							region_access |= target_dept
							authenticated = 1
						else
							authenticated = 2
						playsound(src, 'sound/machines/terminal_on.ogg', 50, FALSE)

					else
						if((ACCESS_HOP in inserted_scan_id.access) && ((target_dept==1) || !target_dept))
							region_access |= 1
							region_access |= 6
							get_subordinates("Head of Personnel")
						if((ACCESS_HOS in inserted_scan_id.access) && ((target_dept==2) || !target_dept))
							region_access |= 2
							get_subordinates("Head of Security")
						if((ACCESS_CMO in inserted_scan_id.access) && ((target_dept==3) || !target_dept))
							region_access |= 3
							get_subordinates("Chief Medical Officer")
						if((ACCESS_RD in inserted_scan_id.access) && ((target_dept==4) || !target_dept))
							region_access |= 4
							get_subordinates("Research Director")
						if((ACCESS_CE in inserted_scan_id.access) && ((target_dept==5) || !target_dept))
							region_access |= 5
							get_subordinates("Chief Engineer")
						if(region_access)
							authenticated = 1
			else if ((!( authenticated ) && issilicon(usr)) && (!inserted_modify_id))
				to_chat(usr, "<span class='warning'>You can't modify an ID without an ID inserted to modify! Once one is in the modify slot on the computer, you can log in.</span>")
		if ("logout")
			region_access = null
			head_subordinates = null
			authenticated = 0
			playsound(src, 'sound/machines/terminal_off.ogg', 50, FALSE)

		if("access")
			if(href_list["allowed"])
				if(authenticated)
					var/access_type = text2num(href_list["access_target"])
					var/access_allowed = text2num(href_list["allowed"])
					if(access_type in (istype(src, /obj/machinery/computer/card/centcom)?get_all_centcom_access() : get_all_accesses()))
						inserted_modify_id.access -= access_type
						if(access_allowed == 1)
							inserted_modify_id.access += access_type
						playsound(src, "terminal_type", 50, FALSE)
		if ("assign")
			if (authenticated == 2)
				var/t1 = href_list["assign_target"]
				if(t1 == "Custom")
					var/newJob = reject_bad_text(input("Enter a custom job assignment.", "Assignment", inserted_modify_id ? inserted_modify_id.assignment : "Unassigned"), MAX_NAME_LEN)
					if(newJob)
						t1 = newJob

				else if(t1 == "Unassigned")
					inserted_modify_id.access -= get_all_accesses()

				else
					var/datum/job/jobdatum
					for(var/jobtype in typesof(/datum/job))
						var/datum/job/J = new jobtype
						if(ckey(J.title) == ckey(t1))
							jobdatum = J
							updateUsrDialog()
							break
					if(!jobdatum)
						to_chat(usr, "<span class='alert'>No log exists for this job.</span>")
						updateUsrDialog()
						return
					if(inserted_modify_id.registered_account)
						inserted_modify_id.registered_account.account_job = jobdatum // this is a terrible idea and people will grief but sure whatever

					inserted_modify_id.access = ( istype(src, /obj/machinery/computer/card/centcom) ? get_centcom_access(t1) : jobdatum.get_access() )
				if (inserted_modify_id)
					inserted_modify_id.assignment = t1
					playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
		if ("demote")
			if(inserted_modify_id.assignment in head_subordinates || inserted_modify_id.assignment == "Assistant")
				inserted_modify_id.assignment = "Unassigned"
				playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
			else
				to_chat(usr, "<span class='alert'>You are not authorized to demote this position.</span>")
		if ("reg")
			if (authenticated)
				var/t2 = inserted_modify_id
				if ((authenticated && inserted_modify_id == t2 && (in_range(src, usr) || issilicon(usr)) && isturf(loc)))
					var/newName = reject_bad_name(href_list["reg"])
					if(newName)
						inserted_modify_id.registered_name = newName
						playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
					else
						to_chat(usr, "<span class='alert'>Invalid name entered.</span>")
						updateUsrDialog()
						return
		if ("mode")
			mode = text2num(href_list["mode_target"])

		if("return")
			//DISPLAY MAIN MENU
			mode = 3;
			playsound(src, "terminal_type", 25, FALSE)

		if("make_job_available")
			// MAKE ANOTHER JOB POSITION AVAILABLE FOR LATE JOINERS
			if(authenticated && !target_dept)
				var/edit_job_target = href_list["job"]
				var/datum/job/j = SSjob.GetJob(edit_job_target)
				if(!j)
					updateUsrDialog()
					return 0
				if(can_open_job(j) != 1)
					updateUsrDialog()
					return 0
				if(opened_positions[edit_job_target] >= 0)
					GLOB.time_last_changed_position = world.time / 10
				j.total_positions++
				opened_positions[edit_job_target]++
				playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

		if("make_job_unavailable")
			// MAKE JOB POSITION UNAVAILABLE FOR LATE JOINERS
			if(authenticated && !target_dept)
				var/edit_job_target = href_list["job"]
				var/datum/job/j = SSjob.GetJob(edit_job_target)
				if(!j)
					updateUsrDialog()
					return 0
				if(can_close_job(j) != 1)
					updateUsrDialog()
					return 0
				//Allow instant closing without cooldown if a position has been opened before
				if(opened_positions[edit_job_target] <= 0)
					GLOB.time_last_changed_position = world.time / 10
				j.total_positions--
				opened_positions[edit_job_target]--
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)

		if ("prioritize_job")
			// TOGGLE WHETHER JOB APPEARS AS PRIORITIZED IN THE LOBBY
			if(authenticated && !target_dept)
				var/priority_target = href_list["job"]
				var/datum/job/j = SSjob.GetJob(priority_target)
				if(!j)
					updateUsrDialog()
					return 0
				var/priority = TRUE
				if(j in SSjob.prioritized_jobs)
					SSjob.prioritized_jobs -= j
					priority = FALSE
				else if(j.total_positions <= j.current_positions)
					to_chat(usr, "<span class='notice'>[j.title] has had all positions filled. Open up more slots before prioritizing it.</span>")
					updateUsrDialog()
					return
				else
					SSjob.prioritized_jobs += j
				to_chat(usr, "<span class='notice'>[j.title] has been successfully [priority ? "prioritized" : "unprioritized"]. Potential employees will notice your request.</span>")
				playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

		if ("print")
			if (!( printing ))
				printing = 1
				sleep(50)
				var/obj/item/paper/P = new /obj/item/paper( loc )
				var/t1 = "<B>Crew Manifest:</B><BR>"
				for(var/datum/data/record/t in sortRecord(GLOB.data_core.general))
					t1 += t.fields["name"] + " - " + t.fields["rank"] + "<br>"
				P.info = t1
				P.name = "paper- 'Crew Manifest'"
				printing = null
				playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
	if (inserted_modify_id)
		inserted_modify_id.update_label()
	updateUsrDialog()

/obj/machinery/computer/card/proc/get_subordinates(rank)
	for(var/datum/job/job in SSjob.occupations)
		if(rank in job.department_head)
			head_subordinates += job.title

/obj/machinery/computer/card/centcom
	name = "\improper CentCom identification console"
	circuit = /obj/item/circuitboard/computer/card/centcom
	req_access = list(ACCESS_CENT_CAPTAIN)

/obj/machinery/computer/card/minor
	name = "department management console"
	desc = "You can use this to change ID's for specific departments."
	icon_screen = "idminor"
	circuit = /obj/item/circuitboard/computer/card/minor

/obj/machinery/computer/card/minor/Initialize()
	. = ..()
	var/obj/item/circuitboard/computer/card/minor/typed_circuit = circuit
	if(target_dept)
		typed_circuit.target_dept = target_dept
	else
		target_dept = typed_circuit.target_dept
	var/list/dept_list = list("general","security","medical","science","engineering")
	name = "[dept_list[target_dept]] department console"

/obj/machinery/computer/card/minor/hos
	target_dept = 2
	icon_screen = "idhos"

	light_color = LIGHT_COLOR_RED

/obj/machinery/computer/card/minor/cmo
	target_dept = 3
	icon_screen = "idcmo"

/obj/machinery/computer/card/minor/rd
	target_dept = 4
	icon_screen = "idrd"

	light_color = LIGHT_COLOR_PINK

/obj/machinery/computer/card/minor/ce
	target_dept = 5
	icon_screen = "idce"

	light_color = LIGHT_COLOR_YELLOW

#undef JOB_ALLOWED
#undef JOB_COOLDOWN
#undef JOB_MAX_POSITIONS
#undef JOB_DENIED
