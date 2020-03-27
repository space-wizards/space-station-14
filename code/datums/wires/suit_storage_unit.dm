/datum/wires/suit_storage_unit
	holder_type = /obj/machinery/suit_storage_unit
	proper_name = "Suit Storage Unit"

/datum/wires/suit_storage_unit/New(atom/holder)
	wires = list(
		WIRE_HACK, WIRE_SAFETY,
		WIRE_ZAP
	)
	add_duds(2)
	..()

/datum/wires/suit_storage_unit/interactable(mob/user)
	var/obj/machinery/suit_storage_unit/SSU = holder
	if(SSU.panel_open)
		return TRUE

/datum/wires/suit_storage_unit/get_status()
	var/obj/machinery/suit_storage_unit/SSU = holder
	var/list/status = list()
	status += "The UV bulb is [SSU.uv_super ? "glowing" : "dim"]."
	status += "The service light is [SSU.safeties ? "off" : "on"]."
	return status

/datum/wires/suit_storage_unit/on_pulse(wire)
	var/obj/machinery/suit_storage_unit/SSU = holder
	switch(wire)
		if(WIRE_HACK)
			SSU.uv_super = !SSU.uv_super
		if(WIRE_SAFETY)
			SSU.safeties = !SSU.safeties
		if(WIRE_ZAP)
			if(usr)
				SSU.shock(usr)

/datum/wires/suit_storage_unit/on_cut(wire, mend)
	var/obj/machinery/suit_storage_unit/SSU = holder
	switch(wire)
		if(WIRE_HACK)
			SSU.uv_super = !mend
		if(WIRE_SAFETY)
			SSU.safeties = mend
		if(WIRE_ZAP)
			if(usr)
				SSU.shock(usr)
