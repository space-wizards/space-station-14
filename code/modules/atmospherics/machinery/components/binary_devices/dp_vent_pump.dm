//Acts like a normal vent, but has an input AND output.

#define EXT_BOUND	1
#define INPUT_MIN	2
#define OUTPUT_MAX	4

/obj/machinery/atmospherics/components/binary/dp_vent_pump
	icon = 'icons/obj/atmospherics/components/unary_devices.dmi' //We reuse the normal vent icons!
	icon_state = "dpvent_map-2"

	//node2 is output port
	//node1 is input port

	name = "dual-port air vent"
	desc = "Has a valve and pump attached to it. There are two ports."

	level = 1
	var/frequency = 0
	var/id = null
	var/datum/radio_frequency/radio_connection

	var/pump_direction = 1 //0 = siphoning, 1 = releasing

	var/external_pressure_bound = ONE_ATMOSPHERE
	var/input_pressure_min = 0
	var/output_pressure_max = 0

	var/pressure_checks = EXT_BOUND

	//EXT_BOUND: Do not pass external_pressure_bound
	//INPUT_MIN: Do not pass input_pressure_min
	//OUTPUT_MAX: Do not pass output_pressure_max

/obj/machinery/atmospherics/components/binary/dp_vent_pump/Destroy()
	SSradio.remove_object(src, frequency)
	return ..()

/obj/machinery/atmospherics/components/binary/dp_vent_pump/update_icon_nopipes()
	cut_overlays()
	if(showpipe)
		var/image/cap = getpipeimage(icon, "dpvent_cap", dir, piping_layer = piping_layer)
		add_overlay(cap)

	if(!on || !is_operational())
		icon_state = "vent_off"
	else
		icon_state = pump_direction ? "vent_out" : "vent_in"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/process_atmos()
	..()

	if(!on)
		return
	var/datum/gas_mixture/air1 = airs[1]
	var/datum/gas_mixture/air2 = airs[2]

	var/datum/gas_mixture/environment = loc.return_air()
	var/environment_pressure = environment.return_pressure()

	if(pump_direction) //input -> external
		var/pressure_delta = 10000

		if(pressure_checks&EXT_BOUND)
			pressure_delta = min(pressure_delta, (external_pressure_bound - environment_pressure))
		if(pressure_checks&INPUT_MIN)
			pressure_delta = min(pressure_delta, (air1.return_pressure() - input_pressure_min))

		if(pressure_delta > 0)
			if(air1.temperature > 0)
				var/transfer_moles = pressure_delta*environment.volume/(air1.temperature * R_IDEAL_GAS_EQUATION)

				var/datum/gas_mixture/removed = air1.remove(transfer_moles)
				//Removed can be null if there is no atmosphere in air1
				if(!removed)
					return

				loc.assume_air(removed)
				air_update_turf()

				var/datum/pipeline/parent1 = parents[1]
				parent1.update = 1

	else //external -> output
		var/pressure_delta = 10000

		if(pressure_checks&EXT_BOUND)
			pressure_delta = min(pressure_delta, (environment_pressure - external_pressure_bound))
		if(pressure_checks&INPUT_MIN)
			pressure_delta = min(pressure_delta, (output_pressure_max - air2.return_pressure()))

		if(pressure_delta > 0)
			if(environment.temperature > 0)
				var/transfer_moles = pressure_delta*air2.volume/(environment.temperature * R_IDEAL_GAS_EQUATION)

				var/datum/gas_mixture/removed = loc.remove_air(transfer_moles)
				//removed can be null if there is no air in the location
				if(!removed)
					return

				air2.merge(removed)
				air_update_turf()

				var/datum/pipeline/parent2 = parents[2]
				parent2.update = 1

	//Radio remote control

/obj/machinery/atmospherics/components/binary/dp_vent_pump/proc/set_frequency(new_frequency)
	SSradio.remove_object(src, frequency)
	frequency = new_frequency
	if(frequency)
		radio_connection = SSradio.add_object(src, frequency, filter = RADIO_ATMOSIA)

/obj/machinery/atmospherics/components/binary/dp_vent_pump/proc/broadcast_status()
	if(!radio_connection)
		return

	var/datum/signal/signal = new(list(
		"tag" = id,
		"device" = "ADVP",
		"power" = on,
		"direction" = pump_direction?("release"):("siphon"),
		"checks" = pressure_checks,
		"input" = input_pressure_min,
		"output" = output_pressure_max,
		"external" = external_pressure_bound,
		"sigtype" = "status"
	))
	radio_connection.post_signal(src, signal, filter = RADIO_ATMOSIA)

/obj/machinery/atmospherics/components/binary/dp_vent_pump/atmosinit()
	..()
	if(frequency)
		set_frequency(frequency)
	broadcast_status()

/obj/machinery/atmospherics/components/binary/dp_vent_pump/receive_signal(datum/signal/signal)
	if(!signal.data["tag"] || (signal.data["tag"] != id) || (signal.data["sigtype"]!="command"))
		return

	if("power" in signal.data)
		on = text2num(signal.data["power"])

	if("power_toggle" in signal.data)
		on = !on

	if("set_direction" in signal.data)
		pump_direction = text2num(signal.data["set_direction"])

	if("checks" in signal.data)
		pressure_checks = text2num(signal.data["checks"])

	if("purge" in signal.data)
		pressure_checks &= ~1
		pump_direction = 0

	if("stabilize" in signal.data)
		pressure_checks |= 1
		pump_direction = 1

	if("set_input_pressure" in signal.data)
		input_pressure_min = CLAMP(text2num(signal.data["set_input_pressure"]),0,ONE_ATMOSPHERE*50)

	if("set_output_pressure" in signal.data)
		output_pressure_max = CLAMP(text2num(signal.data["set_output_pressure"]),0,ONE_ATMOSPHERE*50)

	if("set_external_pressure" in signal.data)
		external_pressure_bound = CLAMP(text2num(signal.data["set_external_pressure"]),0,ONE_ATMOSPHERE*50)

	addtimer(CALLBACK(src, .proc/broadcast_status), 2)

	if(!("status" in signal.data)) //do not update_icon
		update_icon()

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume
	name = "large dual-port air vent"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/New()
	..()
	var/datum/gas_mixture/air1 = airs[1]
	var/datum/gas_mixture/air2 = airs[2]
	air1.volume = 1000
	air2.volume = 1000

// Mapping

/obj/machinery/atmospherics/components/binary/dp_vent_pump/layer1
	piping_layer = 1
	icon_state = "dpvent_map-1"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/layer3
	piping_layer = 3
	icon_state = "dpvent_map-3"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/on
	on = TRUE
	icon_state = "dpvent_map_on-2"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/on/layer1
	piping_layer = 1
	icon_state = "dpvent_map_on-1"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/on/layer3
	piping_layer = 3
	icon_state = "dpvent_map_on-3"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/incinerator_toxmix
	id = INCINERATOR_TOXMIX_DP_VENTPUMP
	frequency = FREQ_AIRLOCK_CONTROL

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/incinerator_atmos
	id = INCINERATOR_ATMOS_DP_VENTPUMP
	frequency = FREQ_AIRLOCK_CONTROL

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/incinerator_syndicatelava
	id = INCINERATOR_SYNDICATELAVA_DP_VENTPUMP
	frequency = FREQ_AIRLOCK_CONTROL

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/layer1
	piping_layer = 1
	icon_state = "dpvent_map-1"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/layer3
	piping_layer = 3
	icon_state = "dpvent_map-3"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/on
	on = TRUE
	icon_state = "dpvent_map_on-2"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/on/layer1
	piping_layer = 1
	icon_state = "dpvent_map_on-1"

/obj/machinery/atmospherics/components/binary/dp_vent_pump/high_volume/on/layer3
	piping_layer = 3
	icon_state = "dpvent_map_on-3"

#undef EXT_BOUND
#undef INPUT_MIN
#undef OUTPUT_MAX
