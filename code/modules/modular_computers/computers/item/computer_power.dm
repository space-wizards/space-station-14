// Tries to draw power from charger or, if no operational charger is present, from power cell.
/obj/item/modular_computer/proc/use_power(amount = 0)
	if(check_power_override())
		return TRUE

	var/obj/item/computer_hardware/recharger/recharger = all_components[MC_CHARGE]

	if(recharger && recharger.check_functionality())
		if(recharger.use_power(amount))
			return TRUE

	var/obj/item/computer_hardware/battery/battery_module = all_components[MC_CELL]

	if(battery_module && battery_module.battery && battery_module.battery.charge)
		var/obj/item/stock_parts/cell/cell = battery_module.battery
		if(cell.use(amount * GLOB.CELLRATE))
			return TRUE
		else // Discharge the cell anyway.
			cell.use(min(amount*GLOB.CELLRATE, cell.charge))
			return FALSE
	return FALSE

/obj/item/modular_computer/proc/give_power(amount)
	var/obj/item/computer_hardware/battery/battery_module = all_components[MC_CELL]
	if(battery_module && battery_module.battery)
		return battery_module.battery.give(amount)
	return 0

/obj/item/modular_computer/get_cell()
	var/obj/item/computer_hardware/battery/battery_module = all_components[MC_CELL]
	if(battery_module && battery_module.battery)
		return battery_module.battery

// Used in following function to reduce copypaste
/obj/item/modular_computer/proc/power_failure()
	if(enabled) // Shut down the computer
		if(active_program)
			active_program.event_powerfailure(0)
		for(var/I in idle_threads)
			var/datum/computer_file/program/PRG = I
			PRG.event_powerfailure(1)
		shutdown_computer(0)

// Handles power-related things, such as battery interaction, recharging, shutdown when it's discharged
/obj/item/modular_computer/proc/handle_power()
	var/obj/item/computer_hardware/recharger/recharger = all_components[MC_CHARGE]
	if(recharger)
		recharger.process()

	var/power_usage = screen_on ? base_active_power_usage : base_idle_power_usage

	for(var/obj/item/computer_hardware/H in all_components)
		if(H.enabled)
			power_usage += H.power_usage

	if(use_power(power_usage))
		last_power_usage = power_usage
		return TRUE
	else
		power_failure()
		return FALSE

// Used by child types if they have other power source than battery or recharger
/obj/item/modular_computer/proc/check_power_override()
	return FALSE
