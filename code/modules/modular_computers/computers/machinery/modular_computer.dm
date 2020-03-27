// Modular Computer - device that runs various programs and operates with hardware
// DO NOT SPAWN THIS TYPE. Use /laptop/ or /console/ instead.
/obj/machinery/modular_computer
	name = "modular computer"
	desc = "An advanced computer."

	use_power = IDLE_POWER_USE
	idle_power_usage = 5
	var/hardware_flag = 0								// A flag that describes this device type
	var/last_power_usage = 0							// Power usage during last tick

	// Modular computers can run on various devices. Each DEVICE (Laptop, Console, Tablet,..)
	// must have it's own DMI file. Icon states must be called exactly the same in all files, but may look differently
	// If you create a program which is limited to Laptops and Consoles you don't have to add it's icon_state overlay for Tablets too, for example.

	icon = null
	icon_state = null
	var/icon_state_unpowered = null						// Icon state when the computer is turned off.
	var/icon_state_powered = null						// Icon state when the computer is turned on.
	var/screen_icon_state_menu = "menu"					// Icon state overlay when the computer is turned on, but no program is loaded that would override the screen.
	var/screen_icon_screensaver = "standby"				// Icon state overlay when the computer is powered, but not 'switched on'.
	var/max_hardware_size = 0							// Maximal hardware size. Currently, tablets have 1, laptops 2 and consoles 3. Limits what hardware types can be installed.
	var/steel_sheet_cost = 10							// Amount of steel sheets refunded when disassembling an empty frame of this computer.
	var/light_strength = 0								// Light luminosity when turned on
	var/base_active_power_usage = 100					// Power usage when the computer is open (screen is active) and can be interacted with. Remember hardware can use power too.
	var/base_idle_power_usage = 10						// Power usage when the computer is idle and screen is off (currently only applies to laptops)

	var/obj/item/modular_computer/processor/cpu = null				// CPU that handles most logic while this type only handles power and other specific things.

/obj/machinery/modular_computer/Initialize()
	. = ..()
	cpu = new(src)
	cpu.physical = src

/obj/machinery/modular_computer/Destroy()
	QDEL_NULL(cpu)
	return ..()

/obj/machinery/modular_computer/examine(mob/user)
	. = ..()
	. += get_modular_computer_parts_examine(user)

/obj/machinery/modular_computer/attack_ghost(mob/dead/observer/user)
	. = ..()
	if(.)
		return
	if(cpu)
		cpu.attack_ghost(user)

/obj/machinery/modular_computer/emag_act(mob/user)
	return cpu ? cpu.emag_act(user) : 1

/obj/machinery/modular_computer/update_icon()
	cut_overlays()
	icon_state = icon_state_powered

	if(!cpu || !cpu.enabled)
		if (!(stat & NOPOWER) && (cpu && cpu.use_power()))
			add_overlay(screen_icon_screensaver)
		else
			icon_state = icon_state_unpowered
		set_light(0)
	else
		set_light(light_strength)
		if(cpu.active_program)
			add_overlay(cpu.active_program.program_icon_state ? cpu.active_program.program_icon_state : screen_icon_state_menu)
		else
			add_overlay(screen_icon_state_menu)

	if(cpu && cpu.obj_integrity <= cpu.integrity_failure * cpu.max_integrity)
		add_overlay("bsod")
		add_overlay("broken")

// Eject ID card from computer, if it has ID slot with card inside.
/obj/machinery/modular_computer/proc/eject_id()
	set name = "Eject ID"
	set category = "Object"

	if(cpu)
		cpu.eject_id()

// Eject ID card from computer, if it has ID slot with card inside.
/obj/machinery/modular_computer/proc/eject_disk()
	set name = "Eject Data Disk"
	set category = "Object"

	if(cpu)
		cpu.eject_disk()

/obj/machinery/modular_computer/proc/eject_card()
	set name = "Eject Intellicard"
	set category = "Object"
	set src in view(1)

	if(cpu)
		cpu.eject_card()

/obj/machinery/modular_computer/AltClick(mob/user)
	if(cpu)
		cpu.AltClick(user)

//ATTACK HAND IGNORING PARENT RETURN VALUE
// On-click handling. Turns on the computer if it's off and opens the GUI.
/obj/machinery/modular_computer/interact(mob/user)
	if(cpu)
		return cpu.interact(user) // CPU is an item, that's why we route attack_hand to attack_self
	else
		return ..()

// Process currently calls handle_power(), may be expanded in future if more things are added.
/obj/machinery/modular_computer/process()
	if(cpu)
		// Keep names in sync.
		cpu.name = name
		cpu.process()

// Used in following function to reduce copypaste
/obj/machinery/modular_computer/proc/power_failure(malfunction = 0)
	var/obj/item/computer_hardware/battery/battery_module = cpu.all_components[MC_CELL]
	if(cpu && cpu.enabled) // Shut down the computer
		visible_message("<span class='danger'>\The [src]'s screen flickers [battery_module ? "\"BATTERY [malfunction ? "MALFUNCTION" : "CRITICAL"]\"" : "\"EXTERNAL POWER LOSS\""] warning as it shuts down unexpectedly.</span>")
		if(cpu)
			cpu.shutdown_computer(0)
	stat |= NOPOWER
	update_icon()

// Modular computers can have battery in them, we handle power in previous proc, so prevent this from messing it up for us.
/obj/machinery/modular_computer/power_change()
	if(cpu && cpu.use_power()) // If MC_CPU still has a power source, PC wouldn't go offline.
		stat &= ~NOPOWER
		update_icon()
		return
	. = ..()

/obj/machinery/modular_computer/attackby(var/obj/item/W as obj, mob/user)
	if(cpu && !(flags_1 & NODECONSTRUCT_1))
		return cpu.attackby(W, user)
	return ..()


// Stronger explosions cause serious damage to internal components
// Minor explosions are mostly mitigitated by casing.
/obj/machinery/modular_computer/ex_act(severity)
	if(cpu)
		cpu.ex_act(severity)
	..()

// EMPs are similar to explosions, but don't cause physical damage to the casing. Instead they screw up the components
/obj/machinery/modular_computer/emp_act(severity)
	. = ..()
	if(. & EMP_PROTECT_CONTENTS)
		return
	if(cpu)
		cpu.emp_act(severity)

// "Stun" weapons can cause minor damage to components (short-circuits?)
// "Burn" damage is equally strong against internal components and exterior casing
// "Brute" damage mostly damages the casing.
/obj/machinery/modular_computer/bullet_act(obj/projectile/Proj)
	if(cpu)
		cpu.bullet_act(Proj)
