/datum/computer_file/program/ntnetmonitor
	filename = "ntmonitor"
	filedesc = "NTNet Diagnostics and Monitoring"
	program_icon_state = "comm_monitor"
	extended_desc = "This program monitors stationwide NTNet network, provides access to logging systems, and allows for configuration changes"
	size = 12
	requires_ntnet = 1
	required_access = ACCESS_NETWORK	//NETWORK CONTROL IS A MORE SECURE PROGRAM.
	available_on_ntnet = 1
	tgui_id = "ntos_net_monitor"

/datum/computer_file/program/ntnetmonitor/ui_act(action, params)
	if(..())
		return 1
	switch(action)
		if("resetIDS")
			. = 1
			if(SSnetworks.station_network)
				SSnetworks.station_network.resetIDS()
			return 1
		if("toggleIDS")
			. = 1
			if(SSnetworks.station_network)
				SSnetworks.station_network.toggleIDS()
			return 1
		if("toggleWireless")
			. = 1
			if(!SSnetworks.station_network)
				return 1

			// NTNet is disabled. Enabling can be done without user prompt
			if(SSnetworks.station_network.setting_disabled)
				SSnetworks.station_network.setting_disabled = 0
				return 1

			// NTNet is enabled and user is about to shut it down. Let's ask them if they really want to do it, as wirelessly connected computers won't connect without NTNet being enabled (which may prevent people from turning it back on)
			var/mob/user = usr
			if(!user)
				return 1
			var/response = alert(user, "Really disable NTNet wireless? If your computer is connected wirelessly you won't be able to turn it back on! This will affect all connected wireless devices.", "NTNet shutdown", "Yes", "No")
			if(response == "Yes")
				SSnetworks.station_network.setting_disabled = 1
			return 1
		if("purgelogs")
			. = 1
			if(SSnetworks.station_network)
				SSnetworks.station_network.purge_logs()
		if("updatemaxlogs")
			. = 1
			var/mob/user = usr
			var/logcount = text2num(input(user,"Enter amount of logs to keep in memory ([MIN_NTNET_LOGS]-[MAX_NTNET_LOGS]):"))
			if(SSnetworks.station_network)
				SSnetworks.station_network.update_max_log_count(logcount)
		if("toggle_function")
			. = 1
			if(!SSnetworks.station_network)
				return 1
			SSnetworks.station_network.toggle_function(text2num(params["id"]))

/datum/computer_file/program/ntnetmonitor/ui_data(mob/user)
	if(!SSnetworks.station_network)
		return
	var/list/data = get_header_data()

	data["ntnetstatus"] = SSnetworks.station_network.check_function()
	data["ntnetrelays"] = SSnetworks.station_network.relays.len
	data["idsstatus"] = SSnetworks.station_network.intrusion_detection_enabled
	data["idsalarm"] = SSnetworks.station_network.intrusion_detection_alarm

	data["config_softwaredownload"] = SSnetworks.station_network.setting_softwaredownload
	data["config_peertopeer"] = SSnetworks.station_network.setting_peertopeer
	data["config_communication"] = SSnetworks.station_network.setting_communication
	data["config_systemcontrol"] = SSnetworks.station_network.setting_systemcontrol

	data["ntnetlogs"] = list()

	for(var/i in SSnetworks.station_network.logs)
		data["ntnetlogs"] += list(list("entry" = i))
	data["ntnetmaxlogs"] = SSnetworks.station_network.setting_maxlogcount

	return data
