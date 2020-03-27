/datum/computer_file/program/filemanager
	filename = "filemanager"
	filedesc = "File Manager"
	extended_desc = "This program allows management of files."
	program_icon_state = "generic"
	size = 8
	requires_ntnet = 0
	available_on_ntnet = 0
	undeletable = 1
	tgui_id = "ntos_file_manager"

	var/open_file
	var/error

/datum/computer_file/program/filemanager/ui_act(action, params)
	if(..())
		return 1

	var/obj/item/computer_hardware/hard_drive/HDD = computer.all_components[MC_HDD]
	var/obj/item/computer_hardware/hard_drive/RHDD = computer.all_components[MC_SDD]
	var/obj/item/computer_hardware/printer/printer = computer.all_components[MC_PRINT]

	switch(action)
		if("PRG_openfile")
			. = 1
			open_file = params["name"]
		if("PRG_newtextfile")
			. = 1
			var/newname = stripped_input(usr, "Enter file name or leave blank to cancel:", "File rename", max_length=50)
			if(!newname)
				return 1
			if(!HDD)
				return 1
			var/datum/computer_file/data/F = new/datum/computer_file/data()
			F.filename = newname
			F.filetype = "TXT"
			HDD.store_file(F)
		if("PRG_deletefile")
			. = 1
			if(!HDD)
				return 1
			var/datum/computer_file/file = HDD.find_file_by_name(params["name"])
			if(!file || file.undeletable)
				return 1
			HDD.remove_file(file)
		if("PRG_usbdeletefile")
			. = 1
			if(!RHDD)
				return 1
			var/datum/computer_file/file = RHDD.find_file_by_name(params["name"])
			if(!file || file.undeletable)
				return 1
			RHDD.remove_file(file)
		if("PRG_closefile")
			. = 1
			open_file = null
			error = null
		if("PRG_clone")
			. = 1
			if(!HDD)
				return 1
			var/datum/computer_file/F = HDD.find_file_by_name(params["name"])
			if(!F || !istype(F))
				return 1
			var/datum/computer_file/C = F.clone(1)
			HDD.store_file(C)
		if("PRG_rename")
			. = 1
			if(!HDD)
				return 1
			var/datum/computer_file/file = HDD.find_file_by_name(params["name"])
			if(!file || !istype(file))
				return 1
			var/newname = stripped_input(usr, "Enter new file name:", "File rename", file.filename, max_length=50)
			if(file && newname)
				file.filename = newname
		if("PRG_edit")
			. = 1
			if(!open_file)
				return 1
			if(!HDD)
				return 1
			var/datum/computer_file/data/F = HDD.find_file_by_name(open_file)
			if(!F || !istype(F))
				return 1
			if(F.do_not_edit && (alert("WARNING: This file is not compatible with editor. Editing it may result in permanently corrupted formatting or damaged data consistency. Edit anyway?", "Incompatible File", "No", "Yes") == "No"))
				return 1
			// 16384 is the limit for file length in characters. Currently, papers have value of 2048 so this is 8 times as long, since we can't edit parts of the file independently.
			var/newtext = stripped_multiline_input(usr, "Editing file [open_file]. You may use most tags used in paper formatting:", "Text Editor", html_decode(F.stored_data), 16384, TRUE)
			if(!newtext)
				return
			if(F)
				var/datum/computer_file/data/backup = F.clone()
				HDD.remove_file(F)
				F.stored_data = newtext
				F.calculate_size()
				// We can't store the updated file, it's probably too large. Print an error and restore backed up version.
				// This is mostly intended to prevent people from losing texts they spent lot of time working on due to running out of space.
				// They will be able to copy-paste the text from error screen and store it in notepad or something.
				if(!HDD.store_file(F))
					error = "I/O error: Unable to overwrite file. Hard drive is probably full. You may want to backup your changes before closing this window:<br><br>[F.stored_data]<br><br>"
					HDD.store_file(backup)
		if("PRG_printfile")
			. = 1
			if(!open_file)
				return 1
			if(!HDD)
				return 1
			var/datum/computer_file/data/F = HDD.find_file_by_name(open_file)
			if(!F || !istype(F))
				return 1
			if(!printer)
				error = "Missing Hardware: Your computer does not have required hardware to complete this operation."
				return 1
			if(!printer.print_text("<font face=\"[(computer.obj_flags & EMAGGED) ? CRAYON_FONT : PRINTER_FONT]\">" + prepare_printjob(F.stored_data) + "</font>", open_file))
				error = "Hardware error: Printer was unable to print the file. It may be out of paper."
				return 1
		if("PRG_copytousb")
			. = 1
			if(!HDD || !RHDD)
				return 1
			var/datum/computer_file/F = HDD.find_file_by_name(params["name"])
			if(!F || !istype(F))
				return 1
			var/datum/computer_file/C = F.clone(0)
			RHDD.store_file(C)
		if("PRG_copyfromusb")
			. = 1
			if(!HDD || !RHDD)
				return 1
			var/datum/computer_file/F = RHDD.find_file_by_name(params["name"])
			if(!F || !istype(F))
				return 1
			var/datum/computer_file/C = F.clone(0)
			HDD.store_file(C)

/datum/computer_file/program/filemanager/proc/parse_tags(t)
	t = replacetext(t, "\[center\]", "<center>")
	t = replacetext(t, "\[/center\]", "</center>")
	t = replacetext(t, "\[br\]", "<BR>")
	t = replacetext(t, "\n", "<BR>")
	t = replacetext(t, "\[b\]", "<B>")
	t = replacetext(t, "\[/b\]", "</B>")
	t = replacetext(t, "\[i\]", "<I>")
	t = replacetext(t, "\[/i\]", "</I>")
	t = replacetext(t, "\[u\]", "<U>")
	t = replacetext(t, "\[/u\]", "</U>")
	t = replacetext(t, "\[time\]", "[station_time_timestamp()]")
	t = replacetext(t, "\[date\]", "[time2text(world.realtime, "MMM DD")] [GLOB.year_integer+540]")
	t = replacetext(t, "\[large\]", "<font size=\"4\">")
	t = replacetext(t, "\[/large\]", "</font>")
	t = replacetext(t, "\[h1\]", "<H1>")
	t = replacetext(t, "\[/h1\]", "</H1>")
	t = replacetext(t, "\[h2\]", "<H2>")
	t = replacetext(t, "\[/h2\]", "</H2>")
	t = replacetext(t, "\[h3\]", "<H3>")
	t = replacetext(t, "\[/h3\]", "</H3>")
	t = replacetext(t, "\[*\]", "<li>")
	t = replacetext(t, "\[hr\]", "<HR>")
	t = replacetext(t, "\[small\]", "<font size = \"1\">")
	t = replacetext(t, "\[/small\]", "</font>")
	t = replacetext(t, "\[list\]", "<ul>")
	t = replacetext(t, "\[/list\]", "</ul>")
	t = replacetext(t, "\[table\]", "<table border=1 cellspacing=0 cellpadding=3 style='border: 1px solid black;'>")
	t = replacetext(t, "\[/table\]", "</td></tr></table>")
	t = replacetext(t, "\[grid\]", "<table>")
	t = replacetext(t, "\[/grid\]", "</td></tr></table>")
	t = replacetext(t, "\[row\]", "</td><tr>")
	t = replacetext(t, "\[tr\]", "</td><tr>")
	t = replacetext(t, "\[td\]", "<td>")
	t = replacetext(t, "\[cell\]", "<td>")
	t = replacetext(t, "\[tab\]", "&nbsp;&nbsp;&nbsp;&nbsp;")

	t = parsemarkdown_basic(t)

	return t

/datum/computer_file/program/filemanager/proc/prepare_printjob(t) // Additional stuff to parse if we want to print it and make a happy Head of Personnel. Forms FTW.
	t = replacetext(t, "\[field\]", "<span class=\"paper_field\"></span>")
	t = replacetext(t, "\[sign\]", "<span class=\"paper_field\"></span>")

	t = parse_tags(t)

	t = replacetext(t, regex("(?:%s(?:ign)|%f(?:ield))(?=\\s|$)", "ig"), "<span class=\"paper_field\"></span>")

	return t

/datum/computer_file/program/filemanager/ui_data(mob/user)
	var/list/data = get_header_data()

	var/obj/item/computer_hardware/hard_drive/HDD = computer.all_components[MC_HDD]
	var/obj/item/computer_hardware/hard_drive/portable/RHDD = computer.all_components[MC_SDD]
	if(error)
		data["error"] = error
	if(open_file)
		var/datum/computer_file/data/file

		if(!computer || !HDD)
			data["error"] = "I/O ERROR: Unable to access hard drive."
		else
			file = HDD.find_file_by_name(open_file)
			if(!istype(file))
				data["error"] = "I/O ERROR: Unable to open file."
			else
				data["filedata"] = parse_tags(file.stored_data)
				data["filename"] = "[file.filename].[file.filetype]"
	else
		if(!computer || !HDD)
			data["error"] = "I/O ERROR: Unable to access hard drive."
		else
			var/list/files[0]
			for(var/datum/computer_file/F in HDD.stored_files)
				files.Add(list(list(
					"name" = F.filename,
					"type" = F.filetype,
					"size" = F.size,
					"undeletable" = F.undeletable
				)))
			data["files"] = files
			if(RHDD)
				data["usbconnected"] = 1
				var/list/usbfiles[0]
				for(var/datum/computer_file/F in RHDD.stored_files)
					usbfiles.Add(list(list(
						"name" = F.filename,
						"type" = F.filetype,
						"size" = F.size,
						"undeletable" = F.undeletable
					)))
				data["usbfiles"] = usbfiles

	return data
