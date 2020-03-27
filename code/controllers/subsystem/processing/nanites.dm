PROCESSING_SUBSYSTEM_DEF(nanites)
	name = "Nanites"
	flags = SS_BACKGROUND|SS_POST_FIRE_TIMING|SS_NO_INIT
	wait = 10

	var/list/datum/nanite_cloud_backup/cloud_backups = list()
	var/list/mob/living/nanite_monitored_mobs = list()
	var/list/datum/nanite_program/relay/nanite_relays = list()
	var/neural_network_count = 0

/datum/controller/subsystem/processing/nanites/proc/check_hardware(datum/nanite_cloud_backup/backup)
	if(QDELETED(backup.storage) || (backup.storage.stat & (NOPOWER|BROKEN)))
		return FALSE
	return TRUE

/datum/controller/subsystem/processing/nanites/proc/get_cloud_backup(cloud_id, force = FALSE)
	for(var/I in cloud_backups)
		var/datum/nanite_cloud_backup/backup = I
		if(!force && !check_hardware(backup))
			return
		if(backup.cloud_id == cloud_id)
			return backup
