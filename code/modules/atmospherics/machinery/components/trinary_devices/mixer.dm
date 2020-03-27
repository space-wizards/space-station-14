/obj/machinery/atmospherics/components/trinary/mixer
	icon_state = "mixer_off"
	density = FALSE

	name = "gas mixer"
	desc = "Very useful for mixing gasses."

	can_unwrench = TRUE

	var/target_pressure = ONE_ATMOSPHERE
	var/node1_concentration = 0.5
	var/node2_concentration = 0.5

	construction_type = /obj/item/pipe/trinary/flippable
	pipe_state = "mixer"

	ui_x = 370
	ui_y = 165

	//node 3 is the outlet, nodes 1 & 2 are intakes

/obj/machinery/atmospherics/components/trinary/mixer/CtrlClick(mob/user)
	if(user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		on = !on
		update_icon()
	return ..()

/obj/machinery/atmospherics/components/trinary/mixer/AltClick(mob/user)
	if(user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		target_pressure = MAX_OUTPUT_PRESSURE
		update_icon()
	return ..()

/obj/machinery/atmospherics/components/trinary/mixer/update_icon()
	cut_overlays()
	for(var/direction in GLOB.cardinals)
		if(!(direction & initialize_directions))
			continue
		var/obj/machinery/atmospherics/node = findConnecting(direction)

		var/image/cap
		if(node)
			cap = getpipeimage(icon, "cap", direction, node.pipe_color, piping_layer = piping_layer)
		else
			cap = getpipeimage(icon, "cap", direction, piping_layer = piping_layer)

		add_overlay(cap)

	return ..()

/obj/machinery/atmospherics/components/trinary/mixer/update_icon_nopipes()
	var/on_state = on && nodes[1] && nodes[2] && nodes[3] && is_operational()
	icon_state = "mixer_[on_state ? "on" : "off"][flipped ? "_f" : ""]"

/obj/machinery/atmospherics/components/trinary/mixer/New()
	..()
	var/datum/gas_mixture/air3 = airs[3]
	air3.volume = 300
	airs[3] = air3

/obj/machinery/atmospherics/components/trinary/mixer/process_atmos()
	..()
	if(!on || !(nodes[1] && nodes[2] && nodes[3]) && !is_operational())
		return

	//Get those gases, mah boiiii
	var/datum/gas_mixture/air1 = airs[1]
	var/datum/gas_mixture/air2 = airs[2]

	if(!air1 || !air2)
		return

	var/datum/gas_mixture/air3 = airs[3]

	var/output_starting_pressure = air3.return_pressure()

	if(output_starting_pressure >= target_pressure)
		//No need to mix if target is already full!
		return

	//Calculate necessary moles to transfer using PV=nRT
	var/general_transfer = (target_pressure - output_starting_pressure) * air3.volume / R_IDEAL_GAS_EQUATION

	var/transfer_moles1 = air1.temperature ? node1_concentration * general_transfer / air1.temperature : 0
	var/transfer_moles2 = air2.temperature ? node2_concentration * general_transfer / air2.temperature : 0

	var/air1_moles = air1.total_moles()
	var/air2_moles = air2.total_moles()

	if(!node2_concentration)
		if(air1.temperature <= 0)
			return
		transfer_moles1 = min(transfer_moles1, air1_moles)
		transfer_moles2 = 0
	else if(!node1_concentration)
		if(air2.temperature <= 0)
			return
		transfer_moles2 = min(transfer_moles2, air2_moles)
		transfer_moles1 = 0
	else
		if(air1.temperature <= 0 || air2.temperature <= 0)
			return
		if((transfer_moles2 <= 0) || (transfer_moles1 <= 0))
			return
		if((air1_moles < transfer_moles1) || (air2_moles < transfer_moles2))
			var/ratio = 0
			ratio = min(air1_moles / transfer_moles1, air2_moles / transfer_moles2)
			transfer_moles1 *= ratio
			transfer_moles2 *= ratio

	//Actually transfer the gas

	if(transfer_moles1)
		var/datum/gas_mixture/removed1 = air1.remove(transfer_moles1)
		air3.merge(removed1)
		var/datum/pipeline/parent1 = parents[1]
		parent1.update = TRUE

	if(transfer_moles2)
		var/datum/gas_mixture/removed2 = air2.remove(transfer_moles2)
		air3.merge(removed2)
		var/datum/pipeline/parent2 = parents[2]
		parent2.update = TRUE

	var/datum/pipeline/parent3 = parents[3]
	parent3.update = TRUE

/obj/machinery/atmospherics/components/trinary/mixer/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
																	datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "atmos_mixer", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/atmospherics/components/trinary/mixer/ui_data()
	var/data = list()
	data["on"] = on
	data["set_pressure"] = round(target_pressure)
	data["max_pressure"] = round(MAX_OUTPUT_PRESSURE)
	data["node1_concentration"] = round(node1_concentration*100, 1)
	data["node2_concentration"] = round(node2_concentration*100, 1)
	return data

/obj/machinery/atmospherics/components/trinary/mixer/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("power")
			on = !on
			investigate_log("was turned [on ? "on" : "off"] by [key_name(usr)]", INVESTIGATE_ATMOS)
			. = TRUE
		if("pressure")
			var/pressure = params["pressure"]
			if(pressure == "max")
				pressure = MAX_OUTPUT_PRESSURE
				. = TRUE
			else if(pressure == "input")
				pressure = input("New output pressure (0-[MAX_OUTPUT_PRESSURE] kPa):", name, target_pressure) as num|null
				if(!isnull(pressure) && !..())
					. = TRUE
			else if(text2num(pressure) != null)
				pressure = text2num(pressure)
				. = TRUE
			if(.)
				target_pressure = CLAMP(pressure, 0, MAX_OUTPUT_PRESSURE)
				investigate_log("was set to [target_pressure] kPa by [key_name(usr)]", INVESTIGATE_ATMOS)
		if("node1")
			var/value = text2num(params["concentration"])
			adjust_node1_value(value)
			investigate_log("was set to [node1_concentration] % on node 1 by [key_name(usr)]", INVESTIGATE_ATMOS)
			. = TRUE
		if("node2")
			var/value = text2num(params["concentration"])
			adjust_node1_value(100 - value)
			investigate_log("was set to [node2_concentration] % on node 2 by [key_name(usr)]", INVESTIGATE_ATMOS)
			. = TRUE
	update_icon()

/obj/machinery/atmospherics/components/trinary/mixer/proc/adjust_node1_value(newValue)
	node1_concentration = newValue / 100
	node2_concentration = 1 - node1_concentration

/obj/machinery/atmospherics/components/trinary/mixer/can_unwrench(mob/user)
	. = ..()
	if(. && on && is_operational())
		to_chat(user, "<span class='warning'>You cannot unwrench [src], turn it off first!</span>")
		return FALSE

// mapping

/obj/machinery/atmospherics/components/trinary/mixer/layer1
	piping_layer = 1
	icon_state = "mixer_off_map-1"
/obj/machinery/atmospherics/components/trinary/mixer/layer3
	piping_layer = 3
	icon_state = "mixer_off_map-3"

/obj/machinery/atmospherics/components/trinary/mixer/on
	on = TRUE
	icon_state = "mixer_on"

/obj/machinery/atmospherics/components/trinary/mixer/on/layer1
	piping_layer = 1
	icon_state = "mixer_on_map-1"
/obj/machinery/atmospherics/components/trinary/mixer/on/layer3
	piping_layer = 3
	icon_state = "mixer_on_map-3"

/obj/machinery/atmospherics/components/trinary/mixer/flipped
	icon_state = "mixer_off_f"
	flipped = TRUE

/obj/machinery/atmospherics/components/trinary/mixer/flipped/layer1
	piping_layer = 1
	icon_state = "mixer_off_f_map-1"
/obj/machinery/atmospherics/components/trinary/mixer/flipped/layer3
	piping_layer = 3
	icon_state = "mixer_off_f_map-3"

/obj/machinery/atmospherics/components/trinary/mixer/flipped/on
	on = TRUE
	icon_state = "mixer_on_f"

/obj/machinery/atmospherics/components/trinary/mixer/flipped/on/layer1
	piping_layer = 1
	icon_state = "mixer_on_f_map-1"
/obj/machinery/atmospherics/components/trinary/mixer/flipped/on/layer3
	piping_layer = 3
	icon_state = "mixer_on_f_map-3"

/obj/machinery/atmospherics/components/trinary/mixer/airmix //For standard airmix to distro
	name = "air mixer"
	icon_state = "mixer_on"
	node1_concentration = N2STANDARD
	node2_concentration = O2STANDARD
	target_pressure = MAX_OUTPUT_PRESSURE
	on = TRUE

/obj/machinery/atmospherics/components/trinary/mixer/airmix/inverse
	node1_concentration = O2STANDARD
	node2_concentration = N2STANDARD

/obj/machinery/atmospherics/components/trinary/mixer/airmix/flipped
	icon_state = "mixer_on_f"
	flipped = TRUE

/obj/machinery/atmospherics/components/trinary/mixer/airmix/flipped/inverse
	node1_concentration = O2STANDARD
	node2_concentration = N2STANDARD
