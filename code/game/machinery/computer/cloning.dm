#define AUTOCLONING_MINIMAL_LEVEL 3

/obj/machinery/computer/cloning
	name = "cloning console"
	desc = "Used to clone people and manage DNA."
	icon_screen = "dna"
	icon_keyboard = "med_key"
	circuit = /obj/item/circuitboard/computer/cloning
	req_access = list(ACCESS_GENETICS) //for modifying records
	var/obj/machinery/dna_scannernew/scanner //Linked scanner. For scanning.
	var/list/pods //Linked cloning pods
	var/temp = "Inactive"
	var/scantemp_ckey
	var/scantemp = "Ready to Scan"
	var/menu = 1 //Which menu screen to display
	var/list/records = list()
	var/datum/data/record/active_record
	var/obj/item/disk/data/diskette //Incompatible format to genetics machine
	//select which parts of the diskette to load
	var/include_se = FALSE //mutations
	var/include_ui = FALSE //appearance
	var/include_ue = FALSE //blood type, UE, and name

	var/loading = FALSE // Nice loading text
	var/autoprocess = FALSE

	light_color = LIGHT_COLOR_BLUE

/obj/machinery/computer/cloning/Initialize()
	. = ..()
	updatemodules(TRUE)

/obj/machinery/computer/cloning/Destroy()
	if(pods)
		for(var/P in pods)
			DetachCloner(P)
		pods = null
	return ..()

/obj/machinery/computer/cloning/proc/GetAvailablePod(mind = null)
	if(pods)
		for(var/P in pods)
			var/obj/machinery/clonepod/pod = P
			if(pod.occupant && mind && pod.clonemind == mind)
				return null
			if(pod.is_operational() && !(pod.occupant || pod.mess))
				return pod

/obj/machinery/computer/cloning/proc/HasEfficientPod()
	if(pods)
		for(var/P in pods)
			var/obj/machinery/clonepod/pod = P
			if(pod.is_operational() && pod.efficiency > 5)
				return TRUE

/obj/machinery/computer/cloning/proc/GetAvailableEfficientPod(mind = null)
	if(pods)
		for(var/P in pods)
			var/obj/machinery/clonepod/pod = P
			if(pod.occupant && pod.clonemind == mind)
				return pod
			else if(!. && pod.is_operational() && !(pod.occupant || pod.mess) && pod.efficiency > 5)
				. = pod

/proc/grow_clone_from_record(obj/machinery/clonepod/pod, datum/data/record/R, empty)
	return pod.growclone(R.fields["name"], R.fields["UI"], R.fields["SE"], R.fields["mindref"], R.fields["last_death"], R.fields["blood_type"], R.fields["mrace"], R.fields["features"], R.fields["factions"], R.fields["quirks"], R.fields["bank_account"], R.fields["traumas"], empty)

/obj/machinery/computer/cloning/process()
	if(!(scanner && LAZYLEN(pods) && autoprocess))
		return

	if(scanner.occupant && scanner.scan_level > 2)
		scan_occupant(scanner.occupant)

	for(var/datum/data/record/R in records)
		var/obj/machinery/clonepod/pod = GetAvailableEfficientPod(R.fields["mindref"])

		if(!pod)
			return

		if(pod.occupant)
			break

		var/result = grow_clone_from_record(pod, R)
		if(result & CLONING_SUCCESS)
			temp = "[R.fields["name"]] => <font class='good'>Cloning cycle in progress...</font>"
			log_cloning("Cloning of [key_name(R.fields["mindref"])] automatically started via autoprocess - [src] at [AREACOORD(src)]. Pod: [pod] at [AREACOORD(pod)].")
		if(result & CLONING_DELETE_RECORD)
			records -= R


/obj/machinery/computer/cloning/proc/updatemodules(findfirstcloner)
	scanner = findscanner()
	if(findfirstcloner && !LAZYLEN(pods))
		findcloner()
	if(!autoprocess)
		STOP_PROCESSING(SSmachines, src)
	else
		START_PROCESSING(SSmachines, src)

/obj/machinery/computer/cloning/proc/findscanner()
	var/obj/machinery/dna_scannernew/scannerf = null

	// Loop through every direction
	for(var/direction in GLOB.cardinals)

		// Try to find a scanner in that direction
		scannerf = locate(/obj/machinery/dna_scannernew, get_step(src, direction))

		// If found and operational, return the scanner
		if (!isnull(scannerf) && scannerf.is_operational())
			return scannerf

	// If no scanner was found, it will return null
	return null

/obj/machinery/computer/cloning/proc/findcloner()
	var/obj/machinery/clonepod/podf = null

	for(var/direction in GLOB.cardinals)

		podf = locate(/obj/machinery/clonepod, get_step(src, direction))
		if (!isnull(podf) && podf.is_operational())
			AttachCloner(podf)

/obj/machinery/computer/cloning/proc/AttachCloner(obj/machinery/clonepod/pod)
	if(!pod.connected)
		pod.connected = src
		LAZYADD(pods, pod)

/obj/machinery/computer/cloning/proc/DetachCloner(obj/machinery/clonepod/pod)
	pod.connected = null
	LAZYREMOVE(pods, pod)

/obj/machinery/computer/cloning/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/disk/data)) //INSERT SOME DISKETTES
		if (!diskette)
			if (!user.transferItemToLoc(W,src))
				return
			diskette = W
			to_chat(user, "<span class='notice'>You insert [W].</span>")
			playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
			updateUsrDialog()
	else if(W.tool_behaviour == TOOL_MULTITOOL)
		if(!multitool_check_buffer(user, W))
			return
		var/obj/item/multitool/P = W

		if(istype(P.buffer, /obj/machinery/clonepod))
			if(get_area(P.buffer) != get_area(src))
				to_chat(user, "<font color = #666633>-% Cannot link machines across power zones. Buffer cleared %-</font color>")
				P.buffer = null
				return
			to_chat(user, "<font color = #666633>-% Successfully linked [P.buffer] with [src] %-</font color>")
			var/obj/machinery/clonepod/pod = P.buffer
			if(pod.connected)
				pod.connected.DetachCloner(pod)
			AttachCloner(pod)
		else
			P.buffer = src
			to_chat(user, "<font color = #666633>-% Successfully stored [REF(P.buffer)] [P.buffer.name] in buffer %-</font color>")
		return
	else
		return ..()

/obj/machinery/computer/cloning/ui_interact(mob/user)
	. = ..()

	updatemodules(TRUE)

	var/dat = ""
	dat += "<a href='byond://?src=[REF(src)];refresh=1'>Refresh</a>"

	if(scanner && HasEfficientPod() && scanner.scan_level >= AUTOCLONING_MINIMAL_LEVEL)
		if(!autoprocess)
			dat += "<a href='byond://?src=[REF(src)];task=autoprocess'>Autoprocess</a>"
		else
			dat += "<a href='byond://?src=[REF(src)];task=stopautoprocess'>Stop autoprocess</a>"
	else
		dat += "<span class='linkOff'>Autoprocess</span>"
	dat += "<h3>Cloning Pod Status</h3>"
	dat += "<div class='statusDisplay'>[temp]&nbsp;</div>"
	switch(menu)
		if(1)
			// Modules
			if (isnull(scanner) || !LAZYLEN(pods))
				dat += "<h3>Modules</h3>"
				//dat += "<a href='byond://?src=[REF(src)];relmodules=1'>Reload Modules</a>"
				if (isnull(scanner))
					dat += "<font class='bad'>ERROR: No Scanner detected!</font><br>"
				if (!LAZYLEN(pods))
					dat += "<font class='bad'>ERROR: No Pod detected</font><br>"

			// Scanner
			if (!isnull(scanner))
				var/mob/living/scanner_occupant = get_mob_or_brainmob(scanner.occupant)

				dat += "<h3>Scanner Functions</h3>"

				dat += "<div class='statusDisplay'>"
				if(!scanner_occupant)
					dat += "Scanner Unoccupied"
				else if(loading)
					dat += "[scanner_occupant] => Scanning..."
				else
					if(scanner_occupant.ckey != scantemp_ckey)
						scantemp = "Ready to Scan"
						scantemp_ckey = scanner_occupant.ckey
					dat += "[scanner_occupant] => [scantemp]"
				dat += "</div>"

				if(scanner_occupant)
					dat += "<a href='byond://?src=[REF(src)];scan=1'>Start Scan</a>"
					dat += "<a href='byond://?src=[REF(src)];scan=1;body_only=1'>Body-Only Scan</a>"
					dat += "<br><a href='byond://?src=[REF(src)];lock=1'>[scanner.locked ? "Unlock Scanner" : "Lock Scanner"]</a>"
				else
					dat += "<span class='linkOff'>Start Scan</span>"

			// Database
			dat += "<h3>Database Functions</h3>"
			if (records.len && records.len > 0)
				dat += "<a href='byond://?src=[REF(src)];menu=2'>View Records ([records.len])</a><br>"
			else
				dat += "<span class='linkOff'>View Records (0)</span><br>"
			if (diskette)
				dat += "<a href='byond://?src=[REF(src)];disk=eject'>Eject Disk</a><br>"



		if(2)
			dat += "<h3>Current records</h3>"
			dat += "<a href='byond://?src=[REF(src)];menu=1'><< Back</a><br><br>"
			for(var/datum/data/record/R in records)
				dat += "<h4>[R.fields["name"]]</h4>Scan ID [R.fields["id"]] <a href='byond://?src=[REF(src)];view_rec=[R.fields["id"]]'>View Record</a>"
		if(3)
			dat += "<h3>Selected Record</h3>"
			dat += "<a href='byond://?src=[REF(src)];menu=2'><< Back</a><br>"

			if (!active_record)
				dat += "<font class='bad'>Record not found.</font>"
			else
				var/body_only = active_record.fields["body_only"]
				dat += "<h4>[active_record.fields["name"]][body_only ? " - BODY-ONLY" : ""]</h4>"
				dat += "Scan ID [active_record.fields["id"]] \
					[!body_only ? "<a href='byond://?src=[REF(src)];clone=[active_record.fields["id"]]'>Clone</a>" : "" ]\
				 	<a href='byond://?src=[REF(src)];clone=[active_record.fields["id"]];empty=TRUE'>Empty Clone</a><br>"

				var/obj/item/implant/health/H = locate(active_record.fields["imp"])

				if ((H) && (istype(H)))
					dat += "<b>Health Implant Data:</b><br />[H.sensehealth()]<br><br />"
				else
					dat += "<font class='bad'>Unable to locate Health Implant.</font><br /><br />"

				dat += "<b>Unique Identifier:</b><br /><span class='highlight'>[active_record.fields["UI"]]</span><br>"
				dat += "<b>Structural Enzymes:</b><br /><span class='highlight'>"
				for(var/key in active_record.fields["SE"])
					if(key != RACEMUT)
						var/val = active_record.fields["SE"][key]
						var/alias = GLOB.all_mutations[key].alias
						dat +="[alias]: [val]<br />"

				dat += "</span><br />"

				if(diskette && diskette.fields)
					dat += "<div class='block'>"
					dat += "<h4>Inserted Disk</h4>"
					dat += "<b>Contents:</b> "
					var/list/L = list()
					if(diskette.fields["UI"])
						L += "Unique Identifier"
					if(diskette.fields["UE"] && diskette.fields["name"] && diskette.fields["blood_type"])
						L += "Unique Enzymes"
					if(diskette.fields["SE"])
						L += "Structural Enzymes"
					dat += english_list(L, "Empty", " + ", " + ")
					var/can_load = FALSE
					var/obj/item/card/id/C = user.get_idcard(TRUE)
					if(C)
						if(check_access(C))
							can_load = TRUE
					if(can_load)
						dat += "<br /><a href='byond://?src=[REF(src)];disk=load'>Load From Disk</a>"
					else
						dat += "<span class='linkOff'>Cannot Load From Disk: Access Denied</span>"
					if(diskette.fields["SE"])
						if(!include_se)
							dat += "<br /><a href='byond://?src=[REF(src)];task=include_se'>Currently Excluding SE</a>"
						else
							dat += "<br /><a href='byond://?src=[REF(src)];task=exclude_se'>Currently Including SE</a>"
					if(diskette.fields["UI"])
						if(!include_ui)
							dat += "<br /><a href='byond://?src=[REF(src)];task=include_ui'>Currently Excluding UI</a>"
						else
							dat += "<br /><a href='byond://?src=[REF(src)];task=exclude_ui'>Currently Including UI</a>"
					if(diskette.fields["UE"])
						if(!include_ue)
							dat += "<br /><a href='byond://?src=[REF(src)];task=include_ue'>Currently Excluding UE</a>"
						else
							dat += "<br /><a href='byond://?src=[REF(src)];task=exclude_ue'>Currently Including UE</a>"


					dat += "<br /><a href='byond://?src=[REF(src)];disk=save'>Save to Disk</a>"
					dat += "</div>"

				dat += "<font size=1><a href='byond://?src=[REF(src)];del_rec=1'>Delete Record</a></font>"

		if(4)
			if (!active_record)
				menu = 2
				ui_interact(user)
				return
			dat += "<b><a href='byond://?src=[REF(src)];del_rec=1'>Please confirm.</a></b><br>"
			dat += "<b><a href='byond://?src=[REF(src)];menu=3'>Cancel</a></b>"

	var/datum/browser/popup = new(user, "cloning", "Cloning System Control")
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()

/obj/machinery/computer/cloning/Topic(href, href_list)
	if(..())
		return

	if(loading)
		return

	if(href_list["task"])
		switch(href_list["task"])
			if("autoprocess")
				if(scanner && HasEfficientPod() && scanner.scan_level >= AUTOCLONING_MINIMAL_LEVEL)
					autoprocess = TRUE
					START_PROCESSING(SSmachines, src)
					playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
			if("stopautoprocess")
				autoprocess = FALSE
				STOP_PROCESSING(SSmachines, src)
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			if("include_se")
				include_se = TRUE
			if("exclude_se")
				include_se = FALSE
			if("include_ui")
				include_ui = TRUE
			if("exclude_ui")
				include_ui = FALSE
			if("include_ue")
				include_ue = TRUE
			if("exclude_ue")
				include_ue = FALSE

	else if ((href_list["scan"]) && !isnull(scanner) && scanner.is_operational())
		scantemp = ""
		var/body_only = href_list["body_only"]
		loading = TRUE
		updateUsrDialog()
		playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
		say("Initiating scan...")

		addtimer(CALLBACK(src, .proc/do_scan, usr, body_only), 2 SECONDS)

		//No locking an open scanner.
	else if ((href_list["lock"]) && !isnull(scanner) && scanner.is_operational())
		if ((!scanner.locked) && (scanner.occupant))
			scanner.locked = TRUE
			playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
		else
			scanner.locked = FALSE
			playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

	else if(href_list["view_rec"])
		playsound(src, "terminal_type", 25, FALSE)
		active_record = find_record("id", href_list["view_rec"], records)
		if(active_record)
			menu = 3
		else
			temp = "Record missing."

	else if (href_list["del_rec"])
		if ((!active_record) || (menu < 3))
			return
		if (menu == 3) //If we are viewing a record, confirm deletion
			var/has_access = FALSE
			if(ishuman(usr))
				var/mob/living/carbon/human/user = usr
				var/obj/item/card/id/C = user.get_idcard(TRUE)
				if(C)
					if(check_access(C))
						has_access = TRUE
			if(active_record.fields["body_only"]) //Body-only scans are not as important and can be deleted freely
				has_access = TRUE
			if(has_access)
				temp = "Delete record?"
				menu = 4
				playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
			else
				temp = "Access Denied"
				menu = 2
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)


		else if (menu == 4)
			log_cloning("[key_name(usr)] deleted [key_name(active_record.fields["mindref"])]'s cloning records from [src] at [AREACOORD(src)].")
			temp = "[active_record.fields["name"]] => Record deleted."
			records.Remove(active_record)
			active_record = null
			playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
			menu = 2

	else if (href_list["disk"]) //Load or eject.
		switch(href_list["disk"])
			if("load")


				if (!diskette || !istype(diskette.fields))
					temp = "<font class='bad'>Load error.</font>"
					updateUsrDialog()
					playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
					return
				if (!active_record)
					temp = "<font class='bad'>Record error.</font>"
					menu = 1
					updateUsrDialog()
					playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
					return

				if(include_ue)
					overwrite_field_if_available(active_record, diskette, "UE")
					overwrite_field_if_available(active_record, diskette, "name")
					overwrite_field_if_available(active_record, diskette, "blood_type")
				if(include_ui)
					overwrite_field_if_available(active_record, diskette, "UI")
				if(include_se)
					overwrite_field_if_available(active_record, diskette, "SE")

				log_cloning("[key_name(usr)] uploaded [key_name(active_record.fields["mindref"])]'s cloning records to [src] at [AREACOORD(src)] via [diskette].")
				temp = "Load successful."
				playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

			if("eject")
				if(diskette)
					diskette.forceMove(drop_location())
					diskette = null
					playsound(src, 'sound/machines/terminal_insert_disc.ogg', 50, FALSE)
			if("save")
				if(!diskette || diskette.read_only || !active_record || !active_record.fields)
					temp = "<font class='bad'>Save error.</font>"
					updateUsrDialog()
					playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
					return

				log_cloning("[key_name(usr)] added [key_name(active_record.fields["mindref"])]'s cloning records to [diskette] via [src] at [AREACOORD(src)].")
				diskette.fields = active_record.fields.Copy()
				diskette.name = "data disk - '[diskette.fields["name"]]'"
				temp = "Save successful."
				playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

	else if (href_list["refresh"])
		updateUsrDialog()
		playsound(src, "terminal_type", 25, FALSE)

	else if (href_list["clone"])
		var/datum/data/record/C = find_record("id", href_list["clone"], records)
		var/empty = href_list["empty"]
		//Look for that player! They better be dead!
		if(C)
			if(C.fields["body_only"] && !empty)
				temp = "<font class='bad'>Cannot initiate regular cloning with body-only scans.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			var/obj/machinery/clonepod/pod = GetAvailablePod()
			var/success = FALSE
			//Can't clone without someone to clone.  Or a pod.  Or if the pod is busy. Or full of gibs.
			if(!LAZYLEN(pods))
				temp = "<font class='bad'>No Clonepods detected.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			else if(!pod)
				temp = "<font class='bad'>No Clonepods available.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			else if(!CONFIG_GET(flag/revival_cloning) && !empty)
				temp = "<font class='bad'>Unable to initiate cloning cycle.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			else if(pod.occupant)
				temp = "<font class='bad'>Cloning cycle already in progress.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			else
				var/result = grow_clone_from_record(pod, C, empty)
				if(result & CLONING_SUCCESS)
					temp = "[C.fields["name"]] => <font class='good'>Cloning cycle in progress...</font>"
					playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
					if(active_record == C)
						active_record = null
					menu = 1
					success = TRUE
					if(!empty)
						log_cloning("[key_name(usr)] initiated cloning of [key_name(C.fields["mindref"])] via [src] at [AREACOORD(src)]. Pod: [pod] at [AREACOORD(pod)].")
					else
						log_cloning("[key_name(usr)] initiated EMPTY cloning of [key_name(C.fields["mindref"])] via [src] at [AREACOORD(src)]. Pod: [pod] at [AREACOORD(pod)].")
				if(result &	CLONING_DELETE_RECORD)
					if(active_record == C)
						active_record = null
					menu = 1
					records -= C

			if(!success)
				temp = "[C.fields["name"]] => <font class='bad'>Initialisation failure.</font>"
				playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)

		else
			temp = "<font class='bad'>Data corruption.</font>"
			playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)

	else if (href_list["menu"])
		menu = text2num(href_list["menu"])
		playsound(src, "terminal_type", 25, FALSE)

	add_fingerprint(usr)
	updateUsrDialog()
	return

/obj/machinery/computer/cloning/proc/do_scan(mob/user, body_only)
	scan_occupant(scanner.occupant, user, body_only)

	loading = FALSE
	updateUsrDialog()
	playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)

/obj/machinery/computer/cloning/proc/scan_occupant(occupant, mob/M, body_only)
	var/mob/living/mob_occupant = get_mob_or_brainmob(occupant)
	var/datum/dna/dna
	var/datum/bank_account/has_bank_account

	// Do not use unless you know what they are.
	var/mob/living/carbon/C = mob_occupant
	var/mob/living/brain/B = mob_occupant

	if(ishuman(mob_occupant))
		dna = C.has_dna()
		var/obj/item/card/id/I = C.get_idcard(TRUE)
		if(I)
			has_bank_account = I.registered_account
	if(isbrain(mob_occupant))
		dna = B.stored_dna

	if(!istype(dna))
		scantemp = "<font class='bad'>Unable to locate valid genetic data.</font>"
		playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
		return
	if(!body_only && (mob_occupant.suiciding || mob_occupant.hellbound))
		scantemp = "<font class='bad'>Subject's brain is not responding to scanning stimuli.</font>"
		playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
		return
	if((HAS_TRAIT(mob_occupant, TRAIT_HUSK)) && (src.scanner.scan_level < 2))
		scantemp = "<font class='bad'>Subject's body is too damaged to scan properly.</font>"
		playsound(src, 'sound/machines/terminal_alert.ogg', 50, FALSE)
		return
	if(HAS_TRAIT(mob_occupant, TRAIT_BADDNA))
		scantemp = "<font class='bad'>Subject's DNA is damaged beyond any hope of recovery.</font>"
		playsound(src, 'sound/machines/terminal_alert.ogg', 50, FALSE)
		return
	if (!body_only && isnull(mob_occupant.mind))
		scantemp = "<font class='bad'>Mental interface failure.</font>"
		playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
		return
	if(!body_only && SSeconomy.full_ancap)
		if(!has_bank_account)
			scantemp = "<font class='average'>Subject is either missing an ID card with a bank account on it, or does not have an account to begin with. Please ensure the ID card is on the body before attempting to scan.</font>"
			playsound(src, 'sound/machines/terminal_prompt_deny.ogg', 50, FALSE)
			return
	var/datum/data/record/R = new()
	if(dna.species)
		// We store the instance rather than the path, because some
		// species (abductors, slimepeople) store state in their
		// species datums
		dna.delete_species = FALSE
		R.fields["mrace"] = dna.species
	else
		var/datum/species/rando_race = pick(GLOB.roundstart_races)
		R.fields["mrace"] = rando_race.type

	R.fields["name"] = mob_occupant.real_name
	R.fields["id"] = copytext_char(md5(mob_occupant.real_name), 2, 6)
	R.fields["UE"] = dna.unique_enzymes
	R.fields["UI"] = dna.uni_identity
	R.fields["SE"] = dna.mutation_index
	R.fields["blood_type"] = dna.blood_type
	R.fields["features"] = dna.features
	R.fields["factions"] = mob_occupant.faction
	R.fields["quirks"] = list()
	for(var/V in mob_occupant.roundstart_quirks)
		var/datum/quirk/T = V
		R.fields["quirks"][T.type] = T.clone_data()

	R.fields["traumas"] = list()
	if(ishuman(mob_occupant))
		R.fields["traumas"] = C.get_traumas()
	if(isbrain(mob_occupant))
		R.fields["traumas"] = B.get_traumas()

	R.fields["bank_account"] = has_bank_account
	R.fields["mindref"] = "[REF(mob_occupant.mind)]"
	R.fields["last_death"] = mob_occupant.stat == DEAD && mob_occupant.mind ? mob_occupant.mind.last_death : -1
	R.fields["body_only"] = body_only

	if(!body_only)
	    //Add an implant if needed
		var/obj/item/implant/health/imp
		for(var/obj/item/implant/health/HI in mob_occupant.implants)
			imp = HI
			break
		if(!imp)
			imp = new /obj/item/implant/health(mob_occupant)
			imp.implant(mob_occupant)
		R.fields["imp"] = "[REF(imp)]"

	var/datum/data/record/old_record = find_record("mindref", REF(mob_occupant.mind), records)
	if(body_only)
		old_record = find_record("UE", dna.unique_enzymes, records) //Body-only records cannot be identified by mind, so we use the DNA
		if(old_record && ((old_record.fields["UI"] != dna.uni_identity) || (!old_record.fields["body_only"]))) //Never overwrite a mind-and-body record if it exists
			old_record = null
	if(old_record)
		records -= old_record
		scantemp = "Record updated."
	else
		scantemp = "Subject successfully scanned."
	records += R
	log_cloning("[M ? key_name(M) : "Autoprocess"] added the [body_only ? "body-only " : ""]record of [key_name(mob_occupant)] to [src] at [AREACOORD(src)].")
	playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50)
