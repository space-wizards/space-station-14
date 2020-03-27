/obj/machinery/atmospherics/components/unary/passive_vent
	icon_state = "passive_vent_map-2"

	name = "passive vent"
	desc = "It is an open vent."
	can_unwrench = TRUE

	level = 1
	layer = GAS_SCRUBBER_LAYER

	pipe_state = "pvent"

/obj/machinery/atmospherics/components/unary/passive_vent/update_icon_nopipes()
	cut_overlays()
	if(showpipe)
		var/image/cap = getpipeimage(icon, "vent_cap", initialize_directions, piping_layer = piping_layer)
		add_overlay(cap)
	icon_state = "passive_vent"

/obj/machinery/atmospherics/components/unary/passive_vent/process_atmos()
	..()

	var/active = FALSE

	var/datum/gas_mixture/external = loc.return_air()
	var/datum/gas_mixture/internal = airs[1]
	var/external_pressure = external.return_pressure()
	var/internal_pressure = internal.return_pressure()
	var/pressure_delta = abs(external_pressure - internal_pressure)

	if(pressure_delta > 0.5)
		if(external_pressure < internal_pressure)
			var/air_temperature = (external.temperature > 0) ? external.temperature : internal.temperature
			var/transfer_moles = (pressure_delta * external.volume) / (air_temperature * R_IDEAL_GAS_EQUATION)
			var/datum/gas_mixture/removed = internal.remove(transfer_moles)
			external.merge(removed)
		else
			var/air_temperature = (internal.temperature > 0) ? internal.temperature : external.temperature
			var/transfer_moles = (pressure_delta * internal.volume) / (air_temperature * R_IDEAL_GAS_EQUATION)
			transfer_moles = min(transfer_moles, external.total_moles() * internal.volume / external.volume)
			var/datum/gas_mixture/removed = external.remove(transfer_moles)
			if(isnull(removed))
				return
			internal.merge(removed)

		active = TRUE

	active = internal.temperature_share(external, OPEN_HEAT_TRANSFER_COEFFICIENT) ? TRUE : active

	if(active)
		air_update_turf()
		update_parents()

/obj/machinery/atmospherics/components/unary/passive_vent/can_crawl_through()
	return TRUE

/obj/machinery/atmospherics/components/unary/passive_vent/layer1
	piping_layer = 1
	icon_state = "passive_vent_map-1"

/obj/machinery/atmospherics/components/unary/passive_vent/layer3
	piping_layer = 3
	icon_state = "passive_vent_map-3"
