/obj/machinery/atmospherics/components/unary/portables_connector
	icon_state = "connector_map-2"

	name = "connector port"
	desc = "For connecting portables devices related to atmospherics control."

	can_unwrench = TRUE

	use_power = NO_POWER_USE
	level = 0
	layer = GAS_FILTER_LAYER

	pipe_flags = PIPING_ONE_PER_TURF
	pipe_state = "connector"

	var/obj/machinery/portable_atmospherics/connected_device

/obj/machinery/atmospherics/components/unary/portables_connector/New()
	..()
	var/datum/gas_mixture/air_contents = airs[1]
	air_contents.volume = 0

/obj/machinery/atmospherics/components/unary/portables_connector/Destroy()
	if(connected_device)
		connected_device.disconnect()
	return ..()

/obj/machinery/atmospherics/components/unary/portables_connector/update_icon_nopipes()
	icon_state = "connector"
	if(showpipe)
		var/image/cap = getpipeimage(icon, "connector_cap", initialize_directions, piping_layer = piping_layer)
		add_overlay(cap)

/obj/machinery/atmospherics/components/unary/portables_connector/process_atmos()
	if(!connected_device)
		return
	update_parents()

/obj/machinery/atmospherics/components/unary/portables_connector/can_unwrench(mob/user)
	. = ..()
	if(. && connected_device)
		to_chat(user, "<span class='warning'>You cannot unwrench [src], detach [connected_device] first!</span>")
		return FALSE

/obj/machinery/atmospherics/components/unary/portables_connector/layer1
	piping_layer = 1
	icon_state = "connector_map-1"

/obj/machinery/atmospherics/components/unary/portables_connector/layer3
	piping_layer = 3
	icon_state = "connector_map-3"

/obj/machinery/atmospherics/components/unary/portables_connector/visible
	level = 2

/obj/machinery/atmospherics/components/unary/portables_connector/visible/layer1
	piping_layer = 1
	icon_state = "connector_map-1"

/obj/machinery/atmospherics/components/unary/portables_connector/visible/layer3
	piping_layer = 3
	icon_state = "connector_map-3"
