/obj/machinery/modular_computer/console/preset
	// Can be changed to give devices specific hardware
	var/_has_id_slot = FALSE
	var/_has_printer = FALSE
	var/_has_battery = FALSE
	var/_has_ai = FALSE

/obj/machinery/modular_computer/console/preset/Initialize()
	. = ..()
	if(!cpu)
		return
	cpu.install_component(new /obj/item/computer_hardware/processor_unit)

	if(_has_id_slot)
		cpu.install_component(new /obj/item/computer_hardware/card_slot)
	if(_has_printer)
		cpu.install_component(new /obj/item/computer_hardware/printer)
	if(_has_battery)
		cpu.install_component(new /obj/item/computer_hardware/battery(cpu, /obj/item/stock_parts/cell/computer/super))
	if(_has_ai)
		cpu.install_component(new /obj/item/computer_hardware/ai_slot)
	install_programs()

// Override in child types to install preset-specific programs.
/obj/machinery/modular_computer/console/preset/proc/install_programs()
	return



// ===== ENGINEERING CONSOLE =====
/obj/machinery/modular_computer/console/preset/engineering
	console_department = "Engineering"
	name = "engineering console"
	desc = "A stationary computer. This one comes preloaded with engineering programs."

/obj/machinery/modular_computer/console/preset/engineering/install_programs()
	var/obj/item/computer_hardware/hard_drive/hard_drive = cpu.all_components[MC_HDD]
	hard_drive.store_file(new/datum/computer_file/program/power_monitor())
	hard_drive.store_file(new/datum/computer_file/program/alarm_monitor())
	hard_drive.store_file(new/datum/computer_file/program/supermatter_monitor())

// ===== RESEARCH CONSOLE =====
/obj/machinery/modular_computer/console/preset/research
	console_department = "Research"
	name = "research director's console"
	desc = "A stationary computer. This one comes preloaded with research programs."
	_has_ai = TRUE

/obj/machinery/modular_computer/console/preset/research/install_programs()
	var/obj/item/computer_hardware/hard_drive/hard_drive = cpu.all_components[MC_HDD]
	hard_drive.store_file(new/datum/computer_file/program/ntnetmonitor())
	hard_drive.store_file(new/datum/computer_file/program/nttransfer())
	hard_drive.store_file(new/datum/computer_file/program/chatclient())
	hard_drive.store_file(new/datum/computer_file/program/aidiag())


// ===== COMMAND CONSOLE =====
/obj/machinery/modular_computer/console/preset/command
	console_department = "Command"
	name = "command console"
	desc = "A stationary computer. This one comes preloaded with command programs."
	_has_id_slot = TRUE
	_has_printer = TRUE

/obj/machinery/modular_computer/console/preset/command/install_programs()
	var/obj/item/computer_hardware/hard_drive/hard_drive = cpu.all_components[MC_HDD]
	hard_drive.store_file(new/datum/computer_file/program/chatclient())
	hard_drive.store_file(new/datum/computer_file/program/card_mod())

// ===== CIVILIAN CONSOLE =====
/obj/machinery/modular_computer/console/preset/civilian
	console_department = "Civilian"
	name = "civilian console"
	desc = "A stationary computer. This one comes preloaded with generic programs."

/obj/machinery/modular_computer/console/preset/civilian/install_programs()
	var/obj/item/computer_hardware/hard_drive/hard_drive = cpu.all_components[MC_HDD]
	hard_drive.store_file(new/datum/computer_file/program/chatclient())
	hard_drive.store_file(new/datum/computer_file/program/nttransfer())
	hard_drive.store_file(new/datum/computer_file/program/arcade())
