//we cant use defines in tgui, so use a string instead of magic numbers
#define COOLING "Cooling"
#define HEATING "Heating"
#define NEUTRAL "Neutral"

///this the plumbing version of a heater/freezer.
/obj/machinery/plumbing/acclimator
	name = "chemical acclimator"
	desc = "An efficient cooler and heater for the perfect showering temperature or illicit chemical factory."

	icon_state = "acclimator"
	buffer = 200

	///towards wich temperature do we build?
	var/target_temperature = 300
	///I cant find a good name for this. Basically if target is 300, and this is 10, it will still target 300 but will start emptying itself at 290 and 310.
	var/allowed_temperature_difference = 1
	///cool/heat power
	var/heater_coefficient = 0.1
	///Are we turned on or off? this is from the on and off button
	var/enabled = TRUE
	///COOLING, HEATING or NEUTRAL. We track this for change, so we dont needlessly update our icon
	var/acclimate_state
	/**We can't take anything in, at least till we're emptied. Down side of the round robin chem transfer, otherwise while emptying 5u of an unreacted chem gets added,
	and you get nasty leftovers
	*/
	var/emptying = FALSE

	ui_x = 320
	ui_y = 271

/obj/machinery/plumbing/acclimator/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/acclimator, bolt)

/obj/machinery/plumbing/acclimator/process()
	if(stat & NOPOWER || !enabled || !reagents.total_volume || reagents.chem_temp == target_temperature)
		if(acclimate_state != NEUTRAL)
			acclimate_state = NEUTRAL
			update_icon()
		if(!reagents.total_volume)
			emptying = FALSE
		return

	if(reagents.chem_temp < target_temperature && acclimate_state != HEATING) //note that we check if the temperature is the same at the start
		acclimate_state = HEATING
		update_icon()
	else if(reagents.chem_temp > target_temperature && acclimate_state != COOLING)
		acclimate_state = COOLING
		update_icon()
	if(!emptying)
		if(reagents.chem_temp >= target_temperature && target_temperature + allowed_temperature_difference >= reagents.chem_temp) //cooling here
			emptying = TRUE
		if(reagents.chem_temp <= target_temperature && target_temperature - allowed_temperature_difference <= reagents.chem_temp) //heating here
			emptying = TRUE

	reagents.adjust_thermal_energy((target_temperature - reagents.chem_temp) * heater_coefficient * SPECIFIC_HEAT_DEFAULT * reagents.total_volume) //keep constant with chem heater
	reagents.handle_reactions()

/obj/machinery/plumbing/acclimator/update_icon_state()
	icon_state = initial(icon_state)
	switch(acclimate_state)
		if(COOLING)
			icon_state += "_cold"
		if(HEATING)
			icon_state += "_hot"

/obj/machinery/plumbing/acclimator/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "acclimator", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/plumbing/acclimator/ui_data(mob/user)
	var/list/data = list()

	data["enabled"] = enabled
	data["chem_temp"] = reagents.chem_temp
	data["target_temperature"] = target_temperature
	data["allowed_temperature_difference"] = allowed_temperature_difference
	data["acclimate_state"] = acclimate_state
	data["max_volume"] = reagents.maximum_volume
	data["reagent_volume"] = reagents.total_volume
	data["emptying"] = emptying
	return data

/obj/machinery/plumbing/acclimator/ui_act(action, params)
	if(..())
		return
	. = TRUE
	switch(action)
		if("set_target_temperature")
			var/target = text2num(params["temperature"])
			target_temperature = CLAMP(target, 0, 1000)
		if("set_allowed_temperature_difference")
			var/target = text2num(params["temperature"])
			allowed_temperature_difference = CLAMP(target, 0, 1000)
		if("toggle_power")
			enabled = !enabled
		if("change_volume")
			var/target = text2num(params["volume"])
			reagents.maximum_volume = CLAMP(round(target), 1, buffer)

#undef COOLING
#undef HEATING
#undef NEUTRAL
