/obj/machinery/atmospherics/pipe/heat_exchanging
	level = 2
	var/minimum_temperature_difference = 20
	var/thermal_conductivity = WINDOW_HEAT_TRANSFER_COEFFICIENT
	color = "#404040"
	buckle_lying = -1
	var/icon_temperature = T20C //stop small changes in temperature causing icon refresh
	resistance_flags = LAVA_PROOF | FIRE_PROOF

/obj/machinery/atmospherics/pipe/heat_exchanging/Initialize()
	. = ..()
	add_atom_colour("#404040", FIXED_COLOUR_PRIORITY)

/obj/machinery/atmospherics/pipe/heat_exchanging/isConnectable(obj/machinery/atmospherics/pipe/heat_exchanging/target, given_layer, HE_type_check = TRUE)
	if(istype(target, /obj/machinery/atmospherics/pipe/heat_exchanging) != HE_type_check)
		return FALSE
	. = ..()

/obj/machinery/atmospherics/pipe/heat_exchanging/hide()
	return

/obj/machinery/atmospherics/pipe/heat_exchanging/process_atmos()
	var/environment_temperature = 0
	var/datum/gas_mixture/pipe_air = return_air()

	var/turf/T = loc
	if(istype(T))
		if(islava(T))
			environment_temperature = 5000
		else if(T.blocks_air)
			environment_temperature = T.temperature
		else
			var/turf/open/OT = T
			environment_temperature = OT.GetTemperature()
	else
		environment_temperature = T.temperature

	if(abs(environment_temperature-pipe_air.temperature) > minimum_temperature_difference)
		parent.temperature_interact(T, volume, thermal_conductivity)


	//heatup/cooldown any mobs buckled to ourselves based on our temperature
	if(has_buckled_mobs())
		var/hc = pipe_air.heat_capacity()
		var/mob/living/heat_source = buckled_mobs[1]
		//Best guess-estimate of the total bodytemperature of all the mobs, since they share the same environment it's ~ok~ to guess like this
		var/avg_temp = (pipe_air.temperature * hc + (heat_source.bodytemperature * buckled_mobs.len) * 3500) / (hc + (buckled_mobs ? buckled_mobs.len * 3500 : 0))
		for(var/m in buckled_mobs)
			var/mob/living/L = m
			L.bodytemperature = avg_temp
		pipe_air.temperature = avg_temp

/obj/machinery/atmospherics/pipe/heat_exchanging/process()
	if(!parent)
		return //machines subsystem fires before atmos is initialized so this prevents race condition runtimes

	var/datum/gas_mixture/pipe_air = return_air()

	//Heat causes pipe to glow
	if(pipe_air.temperature && (icon_temperature > 500 || pipe_air.temperature > 500)) //glow starts at 500K
		if(abs(pipe_air.temperature - icon_temperature) > 10)
			icon_temperature = pipe_air.temperature

			var/h_r = heat2colour_r(icon_temperature)
			var/h_g = heat2colour_g(icon_temperature)
			var/h_b = heat2colour_b(icon_temperature)

			if(icon_temperature < 2000)//scale glow until 2000K
				var/scale = (icon_temperature - 500) / 1500
				h_r = 64 + (h_r - 64) * scale
				h_g = 64 + (h_g - 64) * scale
				h_b = 64 + (h_b - 64) * scale

			animate(src, color = rgb(h_r, h_g, h_b), time = 20, easing = SINE_EASING)

	//burn any mobs buckled based on temperature
	if(has_buckled_mobs())
		var/heat_limit = 1000
		if(pipe_air.temperature > heat_limit + 1)
			for(var/m in buckled_mobs)
				var/mob/living/buckled_mob = m
				buckled_mob.apply_damage(4 * log(pipe_air.temperature - heat_limit), BURN, BODY_ZONE_CHEST)
