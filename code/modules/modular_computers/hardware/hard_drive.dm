/obj/item/computer_hardware/hard_drive
	name = "hard disk drive"
	desc = "A small HDD, for use in basic computers where power efficiency is desired."
	power_usage = 25
	icon_state = "harddisk_mini"
	critical = 1
	w_class = WEIGHT_CLASS_TINY
	device_type = MC_HDD
	var/max_capacity = 128
	var/used_capacity = 0
	var/list/stored_files = list()		// List of stored files on this drive. DO NOT MODIFY DIRECTLY!

/obj/item/computer_hardware/hard_drive/on_remove(obj/item/modular_computer/MC, mob/user)
	MC.shutdown_computer()

/obj/item/computer_hardware/hard_drive/proc/install_default_programs()
	store_file(new/datum/computer_file/program/computerconfig(src)) 	// Computer configuration utility, allows hardware control and displays more info than status bar
	store_file(new/datum/computer_file/program/ntnetdownload(src))		// NTNet Downloader Utility, allows users to download more software from NTNet repository
	store_file(new/datum/computer_file/program/filemanager(src))		// File manager, allows text editor functions and basic file manipulation.

/obj/item/computer_hardware/hard_drive/examine(user)
	. = ..()
	. += "<span class='notice'>It has [max_capacity] GQ of storage capacity.</span>"

/obj/item/computer_hardware/hard_drive/diagnostics(var/mob/user)
	..()
	// 999 is a byond limit that is in place. It's unlikely someone will reach that many files anyway, since you would sooner run out of space.
	to_chat(user, "NT-NFS File Table Status: [stored_files.len]/999")
	to_chat(user, "Storage capacity: [used_capacity]/[max_capacity]GQ")

// Use this proc to add file to the drive. Returns 1 on success and 0 on failure. Contains necessary sanity checks.
/obj/item/computer_hardware/hard_drive/proc/store_file(var/datum/computer_file/F)
	if(!F || !istype(F))
		return 0

	if(!can_store_file(F))
		return 0

	if(!check_functionality())
		return 0

	if(!stored_files)
		return 0

	// This file is already stored. Don't store it again.
	if(F in stored_files)
		return 0

	F.holder = src
	stored_files.Add(F)
	recalculate_size()
	return 1

// Use this proc to remove file from the drive. Returns 1 on success and 0 on failure. Contains necessary sanity checks.
/obj/item/computer_hardware/hard_drive/proc/remove_file(var/datum/computer_file/F)
	if(!F || !istype(F))
		return 0

	if(!stored_files)
		return 0

	if(!check_functionality())
		return 0

	if(F in stored_files)
		stored_files -= F
		recalculate_size()
		return 1
	else
		return 0

// Loops through all stored files and recalculates used_capacity of this drive
/obj/item/computer_hardware/hard_drive/proc/recalculate_size()
	var/total_size = 0
	for(var/datum/computer_file/F in stored_files)
		total_size += F.size

	used_capacity = total_size

// Checks whether file can be stored on the hard drive. We can only store unique files, so this checks whether we wouldn't get a duplicity by adding a file.
/obj/item/computer_hardware/hard_drive/proc/can_store_file(var/datum/computer_file/F)
	if(!F || !istype(F))
		return 0

	if(F in stored_files)
		return 0

	var/name = F.filename + "." + F.filetype
	for(var/datum/computer_file/file in stored_files)
		if((file.filename + "." + file.filetype) == name)
			return 0

	// In the unlikely event someone manages to create that many files.
	// BYOND is acting weird with numbers above 999 in loops (infinite loop prevention)
	if(stored_files.len >= 999)
		return 0
	if((used_capacity + F.size) > max_capacity)
		return 0
	else
		return 1


// Tries to find the file by filename. Returns null on failure
/obj/item/computer_hardware/hard_drive/proc/find_file_by_name(var/filename)
	if(!check_functionality())
		return null

	if(!filename)
		return null

	if(!stored_files)
		return null

	for(var/datum/computer_file/F in stored_files)
		if(F.filename == filename)
			return F
	return null

/obj/item/computer_hardware/hard_drive/Destroy()
	stored_files = null
	return ..()

/obj/item/computer_hardware/hard_drive/Initialize()
	. = ..()
	install_default_programs()


/obj/item/computer_hardware/hard_drive/advanced
	name = "advanced hard disk drive"
	desc = "A hybrid HDD, for use in higher grade computers where balance between power efficiency and capacity is desired."
	max_capacity = 256
	power_usage = 50 					// Hybrid, medium capacity and medium power storage
	icon_state = "harddisk_mini"
	w_class = WEIGHT_CLASS_SMALL

/obj/item/computer_hardware/hard_drive/super
	name = "super hard disk drive"
	desc = "A high capacity HDD, for use in cluster storage solutions where capacity is more important than power efficiency."
	max_capacity = 512
	power_usage = 100					// High-capacity but uses lots of power, shortening battery life. Best used with APC link.
	icon_state = "harddisk_mini"
	w_class = WEIGHT_CLASS_SMALL

/obj/item/computer_hardware/hard_drive/cluster
	name = "cluster hard disk drive"
	desc = "A large storage cluster consisting of multiple HDDs for usage in dedicated storage systems."
	power_usage = 500
	max_capacity = 2048
	icon_state = "harddisk"
	w_class = WEIGHT_CLASS_NORMAL

// For tablets, etc. - highly power efficient.
/obj/item/computer_hardware/hard_drive/small
	name = "solid state drive"
	desc = "An efficient SSD for portable devices."
	power_usage = 10
	max_capacity = 64
	icon_state = "ssd_mini"
	w_class = WEIGHT_CLASS_TINY
	custom_price = 150

// Syndicate variant - very slight better
/obj/item/computer_hardware/hard_drive/small/syndicate
	desc = "An efficient SSD for portable devices developed by a rival organisation."
	power_usage = 8
	max_capacity = 70
	var/datum/antagonist/traitor/traitor_data // Syndicate hard drive has the user's data baked directly into it on creation

/obj/item/computer_hardware/hard_drive/micro
	name = "micro solid state drive"
	desc = "A highly efficient SSD chip for portable devices."
	power_usage = 2
	max_capacity = 32
	icon_state = "ssd_micro"
	w_class = WEIGHT_CLASS_TINY
