/obj/item/computer_hardware
	name = "hardware"
	desc = "Unknown Hardware."
	icon = 'icons/obj/module.dmi'
	icon_state = "std_mod"

	w_class = WEIGHT_CLASS_TINY	// w_class limits which devices can contain this component.
	// 1: PDAs/Tablets, 2: Laptops, 3-4: Consoles only
	var/obj/item/modular_computer/holder = null
	// Computer that holds this hardware, if any.

	var/power_usage = 0 			// If the hardware uses extra power, change this.
	var/enabled = 1					// If the hardware is turned off set this to 0.
	var/critical = 0				// Prevent disabling for important component, like the CPU.
	var/can_install = 1				// Prevents direct installation of removable media.
	var/damage = 0					// Current damage level
	var/max_damage = 100			// Maximal damage level.
	var/damage_malfunction = 20		// "Malfunction" threshold. When damage exceeds this value the hardware piece will semi-randomly fail and do !!FUN!! things
	var/damage_failure = 50			// "Failure" threshold. When damage exceeds this value the hardware piece will not work at all.
	var/malfunction_probability = 10// Chance of malfunction when the component is damaged
	var/device_type

/obj/item/computer_hardware/New(var/obj/L)
	..()
	pixel_x = rand(-8, 8)
	pixel_y = rand(-8, 8)

/obj/item/computer_hardware/Destroy()
	if(holder)
		holder.uninstall_component(src)
	return ..()


/obj/item/computer_hardware/attackby(obj/item/I, mob/living/user)
	// Cable coil. Works as repair method, but will probably require multiple applications and more cable.
	if(istype(I, /obj/item/stack/cable_coil))
		var/obj/item/stack/S = I
		if(obj_integrity == max_integrity)
			to_chat(user, "<span class='warning'>\The [src] doesn't seem to require repairs.</span>")
			return 1
		if(S.use(1))
			to_chat(user, "<span class='notice'>You patch up \the [src] with a bit of \the [I].</span>")
			obj_integrity = min(obj_integrity + 10, max_integrity)
		return 1

	if(try_insert(I, user))
		return TRUE

	return ..()

/obj/item/computer_hardware/multitool_act(mob/living/user, obj/item/I)
	..()
	to_chat(user, "***** DIAGNOSTICS REPORT *****")
	diagnostics(user)
	to_chat(user, "******************************")
	return TRUE

// Called on multitool click, prints diagnostic information to the user.
/obj/item/computer_hardware/proc/diagnostics(var/mob/user)
	to_chat(user, "Hardware Integrity Test... (Corruption: [damage]/[max_damage]) [damage > damage_failure ? "FAIL" : damage > damage_malfunction ? "WARN" : "PASS"]")

// Handles damage checks
/obj/item/computer_hardware/proc/check_functionality()
	if(!enabled) // Disabled.
		return FALSE

	if(damage > damage_failure) // Too damaged to work at all.
		return FALSE

	if(damage > damage_malfunction) // Still working. Well, sometimes...
		if(prob(malfunction_probability))
			return FALSE

	return TRUE // Good to go.

/obj/item/computer_hardware/examine(var/mob/user)
	. = ..()
	if(damage > damage_failure)
		. += "<span class='danger'>It seems to be severely damaged!</span>"
	else if(damage > damage_malfunction)
		. += "<span class='warning'>It seems to be damaged!</span>"
	else if(damage)
		. += "<span class='notice'>It seems to be slightly damaged.</span>"

// Component-side compatibility check.
/obj/item/computer_hardware/proc/can_install(obj/item/modular_computer/M, mob/living/user = null)
	return can_install

// Called when component is installed into PC.
/obj/item/computer_hardware/proc/on_install(obj/item/modular_computer/M, mob/living/user = null)
	return

// Called when component is removed from PC.
/obj/item/computer_hardware/proc/on_remove(obj/item/modular_computer/M, mob/living/user = null)
	try_eject(forced = 1)

// Called when someone tries to insert something in it - paper in printer, card in card reader, etc.
/obj/item/computer_hardware/proc/try_insert(obj/item/I, mob/living/user = null)
	return FALSE

// Called when someone tries to eject something from it - card from card reader, etc.
/obj/item/computer_hardware/proc/try_eject(slot=0, mob/living/user = null, forced = 0)
	return FALSE
