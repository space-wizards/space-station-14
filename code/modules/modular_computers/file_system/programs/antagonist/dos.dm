/datum/computer_file/program/ntnet_dos
	filename = "ntn_dos"
	filedesc = "DoS Traffic Generator"
	program_icon_state = "hostile"
	extended_desc = "This advanced script can perform denial of service attacks against NTNet quantum relays. The system administrator will probably notice this. Multiple devices can run this program together against same relay for increased effect"
	size = 20
	requires_ntnet = 1
	available_on_ntnet = 0
	available_on_syndinet = 1
	tgui_id = "ntos_net_dos"
	ui_style = "syndicate"
	ui_x = 400
	ui_y = 250

	var/obj/machinery/ntnet_relay/target = null
	var/dos_speed = 0
	var/error = ""
	var/executed = 0

/datum/computer_file/program/ntnet_dos/process_tick()
	dos_speed = 0
	switch(ntnet_status)
		if(1)
			dos_speed = NTNETSPEED_LOWSIGNAL * 10
		if(2)
			dos_speed = NTNETSPEED_HIGHSIGNAL * 10
		if(3)
			dos_speed = NTNETSPEED_ETHERNET * 10
	if(target && executed)
		target.dos_overload += dos_speed
		if(!target.is_operational())
			target.dos_sources.Remove(src)
			target = null
			error = "Connection to destination relay lost."

/datum/computer_file/program/ntnet_dos/kill_program(forced = FALSE)
	if(target)
		target.dos_sources.Remove(src)
	target = null
	executed = 0

	..()

/datum/computer_file/program/ntnet_dos/ui_act(action, params)
	if(..())
		return 1
	switch(action)
		if("PRG_target_relay")
			for(var/obj/machinery/ntnet_relay/R in SSnetworks.station_network.relays)
				if("[R.uid]" == params["targid"])
					target = R
			return 1
		if("PRG_reset")
			if(target)
				target.dos_sources.Remove(src)
				target = null
			executed = 0
			error = ""
			return 1
		if("PRG_execute")
			if(target)
				executed = 1
				target.dos_sources.Add(src)
				if(SSnetworks.station_network.intrusion_detection_enabled)
					var/obj/item/computer_hardware/network_card/network_card = computer.all_components[MC_NET]
					SSnetworks.station_network.add_log("IDS WARNING - Excess traffic flood targeting relay [target.uid] detected from device: [network_card.get_network_tag()]")
					SSnetworks.station_network.intrusion_detection_alarm = 1
			return 1

/datum/computer_file/program/ntnet_dos/ui_data(mob/user)
	if(!SSnetworks.station_network)
		return

	var/list/data = list()

	data = get_header_data()

	if(error)
		data["error"] = error
	else if(target && executed)
		data["target"] = 1
		data["speed"] = dos_speed

		// This is mostly visual, generate some strings of 1s and 0s
		// Probability of 1 is equal of completion percentage of DoS attack on this relay.
		// Combined with UI updates this adds quite nice effect to the UI
		var/percentage = target.dos_overload * 100 / target.dos_capacity
		data["dos_strings"] = list()
		for(var/j, j<10, j++)
			var/string = ""
			for(var/i, i<20, i++)
				string = "[string][prob(percentage)]"
			data["dos_strings"] += list(list("nums" = string))
	else
		data["relays"] = list()
		for(var/obj/machinery/ntnet_relay/R in SSnetworks.station_network.relays)
			data["relays"] += list(list("id" = R.uid))
		data["focus"] = target ? target.uid : null

	return data
