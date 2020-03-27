/datum/wires/vending
	holder_type = /obj/machinery/vending
	proper_name = "Vending Unit"

/datum/wires/vending/New(atom/holder)
	wires = list(
		WIRE_THROW, WIRE_SHOCK, WIRE_SPEAKER,
		WIRE_CONTRABAND, WIRE_IDSCAN
	)
	add_duds(1)
	..()

/datum/wires/vending/interactable(mob/user)
	var/obj/machinery/vending/V = holder
	if(!issilicon(user) && V.seconds_electrified && V.shock(user, 100))
		return FALSE
	if(V.panel_open)
		return TRUE

/datum/wires/vending/get_status()
	var/obj/machinery/vending/V = holder
	var/list/status = list()
	status += "The orange light is [V.seconds_electrified ? "on" : "off"]."
	status += "The red light is [V.shoot_inventory ? "off" : "blinking"]."
	status += "The green light is [V.extended_inventory ? "on" : "off"]."
	status += "A [V.scan_id ? "purple" : "yellow"] light is on."
	status += "The speaker light is [V.shut_up ? "off" : "on"]."
	return status

/datum/wires/vending/on_pulse(wire)
	var/obj/machinery/vending/V = holder
	switch(wire)
		if(WIRE_THROW)
			V.shoot_inventory = !V.shoot_inventory
		if(WIRE_CONTRABAND)
			V.extended_inventory = !V.extended_inventory
		if(WIRE_SHOCK)
			V.seconds_electrified = MACHINE_DEFAULT_ELECTRIFY_TIME
		if(WIRE_IDSCAN)
			V.scan_id = !V.scan_id
		if(WIRE_SPEAKER)
			V.shut_up = !V.shut_up

/datum/wires/vending/on_cut(wire, mend)
	var/obj/machinery/vending/V = holder
	switch(wire)
		if(WIRE_THROW)
			V.shoot_inventory = !mend
		if(WIRE_CONTRABAND)
			V.extended_inventory = FALSE
		if(WIRE_SHOCK)
			if(mend)
				V.seconds_electrified = MACHINE_NOT_ELECTRIFIED
			else
				V.seconds_electrified = MACHINE_ELECTRIFIED_PERMANENT
		if(WIRE_IDSCAN)
			V.scan_id = mend
		if(WIRE_SPEAKER)
			V.shut_up = mend
