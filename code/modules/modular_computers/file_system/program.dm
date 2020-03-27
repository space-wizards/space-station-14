// /program/ files are executable programs that do things.
/datum/computer_file/program
	filetype = "PRG"
	filename = "UnknownProgram"				// File name. FILE NAME MUST BE UNIQUE IF YOU WANT THE PROGRAM TO BE DOWNLOADABLE FROM NTNET!
	var/required_access = null				// List of required accesses to *run* the program.
	var/transfer_access = null				// List of required access to download or file host the program
	var/program_state = PROGRAM_STATE_KILLED// PROGRAM_STATE_KILLED or PROGRAM_STATE_BACKGROUND or PROGRAM_STATE_ACTIVE - specifies whether this program is running.
	var/obj/item/modular_computer/computer	// Device that runs this program.
	var/filedesc = "Unknown Program"		// User-friendly name of this program.
	var/extended_desc = "N/A"				// Short description of this program's function.
	var/program_icon_state = null			// Program-specific screen icon state
	var/requires_ntnet = 0					// Set to 1 for program to require nonstop NTNet connection to run. If NTNet connection is lost program crashes.
	var/requires_ntnet_feature = 0			// Optional, if above is set to 1 checks for specific function of NTNet (currently NTNET_SOFTWAREDOWNLOAD, NTNET_PEERTOPEER, NTNET_SYSTEMCONTROL and NTNET_COMMUNICATION)
	var/ntnet_status = 1					// NTNet status, updated every tick by computer running this program. Don't use this for checks if NTNet works, computers do that. Use this for calculations, etc.
	var/usage_flags = PROGRAM_ALL			// Bitflags (PROGRAM_CONSOLE, PROGRAM_LAPTOP, PROGRAM_TABLET combination) or PROGRAM_ALL
	var/network_destination = null			// Optional string that describes what NTNet server/system this program connects to. Used in default logging.
	var/available_on_ntnet = 1				// Whether the program can be downloaded from NTNet. Set to 0 to disable.
	var/available_on_syndinet = 0			// Whether the program can be downloaded from SyndiNet (accessible via emagging the computer). Set to 1 to enable.
	var/tgui_id								// ID of TGUI interface
	var/ui_style							// ID of custom TGUI style (optional)
	var/ui_x = 575							// Default size of TGUI window, in pixels
	var/ui_y = 700
	var/ui_header = null					// Example: "something.gif" - a header image that will be rendered in computer's UI when this program is running at background. Images are taken from /icons/program_icons. Be careful not to use too large images!

/datum/computer_file/program/New(obj/item/modular_computer/comp = null)
	..()
	if(comp && istype(comp))
		computer = comp

/datum/computer_file/program/Destroy()
	computer = null
	. = ..()

/datum/computer_file/program/clone()
	var/datum/computer_file/program/temp = ..()
	temp.required_access = required_access
	temp.filedesc = filedesc
	temp.program_icon_state = program_icon_state
	temp.requires_ntnet = requires_ntnet
	temp.requires_ntnet_feature = requires_ntnet_feature
	temp.usage_flags = usage_flags
	return temp

// Relays icon update to the computer.
/datum/computer_file/program/proc/update_computer_icon()
	if(computer)
		computer.update_icon()

// Attempts to create a log in global ntnet datum. Returns 1 on success, 0 on fail.
/datum/computer_file/program/proc/generate_network_log(text)
	if(computer)
		return computer.add_log(text)
	return 0

/datum/computer_file/program/proc/is_supported_by_hardware(hardware_flag = 0, loud = 0, mob/user = null)
	if(!(hardware_flag & usage_flags))
		if(loud && computer && user)
			to_chat(user, "<span class='danger'>\The [computer] flashes an \"Hardware Error - Incompatible software\" warning.</span>")
		return 0
	return 1

/datum/computer_file/program/proc/get_signal(specific_action = 0)
	if(computer)
		return computer.get_ntnet_status(specific_action)
	return 0

// Called by Process() on device that runs us, once every tick.
/datum/computer_file/program/proc/process_tick()
	return 1

// Check if the user can run program. Only humans can operate computer. Automatically called in run_program()
// User has to wear their ID for ID Scan to work.
// Can also be called manually, with optional parameter being access_to_check to scan the user's ID
/datum/computer_file/program/proc/can_run(mob/user, loud = FALSE, access_to_check, transfer = FALSE)
	// Defaults to required_access
	if(!access_to_check)
		if(transfer && transfer_access)
			access_to_check = transfer_access
		else
			access_to_check = required_access
	if(!access_to_check) // No required_access, allow it.
		return TRUE

	if(!transfer && computer && (computer.obj_flags & EMAGGED))	//emags can bypass the execution locks but not the download ones.
		return TRUE

	if(IsAdminGhost(user))
		return TRUE

	if(issilicon(user))
		return TRUE

	if(ishuman(user))
		var/obj/item/card/id/D
		var/obj/item/computer_hardware/card_slot/card_slot
		if(computer && card_slot)
			card_slot = computer.all_components[MC_CARD]
			D = card_slot.GetID()
		var/mob/living/carbon/human/h = user
		var/obj/item/card/id/I = h.get_idcard(TRUE)

		if(!I && !D)
			if(loud)
				to_chat(user, "<span class='danger'>\The [computer] flashes an \"RFID Error - Unable to scan ID\" warning.</span>")
			return FALSE

		if(I)
			if(access_to_check in I.GetAccess())
				return TRUE
		else if(D)
			if(access_to_check in D.GetAccess())
				return TRUE
		if(loud)
			to_chat(user, "<span class='danger'>\The [computer] flashes an \"Access Denied\" warning.</span>")
	return 0

// This attempts to retrieve header data for UIs. If implementing completely new device of different type than existing ones
// always include the device here in this proc. This proc basically relays the request to whatever is running the program.
/datum/computer_file/program/proc/get_header_data()
	if(computer)
		return computer.get_header_data()
	return list()

// This is performed on program startup. May be overridden to add extra logic. Remember to include ..() call. Return 1 on success, 0 on failure.
// When implementing new program based device, use this to run the program.
/datum/computer_file/program/proc/run_program(mob/living/user)
	if(can_run(user, 1))
		if(requires_ntnet && network_destination)
			generate_network_log("Connection opened to [network_destination].")
		program_state = PROGRAM_STATE_ACTIVE
		return 1
	return 0

// Use this proc to kill the program. Designed to be implemented by each program if it requires on-quit logic, such as the NTNRC client.
/datum/computer_file/program/proc/kill_program(forced = FALSE)
	program_state = PROGRAM_STATE_KILLED
	if(network_destination)
		generate_network_log("Connection to [network_destination] closed.")
	return 1


/datum/computer_file/program/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui && tgui_id)
		var/datum/asset/assets = get_asset_datum(/datum/asset/simple/headers)
		assets.send(user)

		ui = new(user, src, ui_key, tgui_id, filedesc, ui_x, ui_y, state = state)

		if(ui_style)
			ui.set_style(ui_style)
		ui.set_autoupdate(state = 1)
		ui.open()

// CONVENTIONS, READ THIS WHEN CREATING NEW PROGRAM AND OVERRIDING THIS PROC:
// Topic calls are automagically forwarded from NanoModule this program contains.
// Calls beginning with "PRG_" are reserved for programs handling.
// Calls beginning with "PC_" are reserved for computer handling (by whatever runs the program)
// ALWAYS INCLUDE PARENT CALL ..() OR DIE IN FIRE.
/datum/computer_file/program/ui_act(action,params,datum/tgui/ui)
	if(..())
		return 1
	if(computer)
		switch(action)
			if("PC_exit")
				computer.kill_program()
				ui.close()
				return 1
			if("PC_shutdown")
				computer.shutdown_computer()
				ui.close()
				return 1
			if("PC_minimize")
				var/mob/user = usr
				if(!computer.active_program || !computer.all_components[MC_CPU])
					return

				computer.idle_threads.Add(computer.active_program)
				program_state = PROGRAM_STATE_BACKGROUND // Should close any existing UIs

				computer.active_program = null
				computer.update_icon()
				ui.close()

				if(user && istype(user))
					computer.ui_interact(user) // Re-open the UI on this computer. It should show the main screen now.


/datum/computer_file/program/ui_host()
	if(computer.physical)
		return computer.physical
	else
		return computer

/datum/computer_file/program/ui_status(mob/user)
	if(program_state != PROGRAM_STATE_ACTIVE) // Our program was closed. Close the ui if it exists.
		return UI_CLOSE
	return ..()
