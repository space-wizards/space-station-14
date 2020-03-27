/obj/item/circuitboard/machine/spaceship_navigation_beacon
	name = "Bluespace Navigation Gigabeacon (Machine Board)"
	build_path = /obj/machinery/spaceship_navigation_beacon
	req_components = list()


/obj/machinery/spaceship_navigation_beacon
	name = "Bluespace Navigation Gigabeacon"
	desc = "A device that creates a bluespace anchor that allow ships jump near to it."
	icon = 'icons/obj/abductor.dmi'
	icon_state = "core"
	use_power = IDLE_POWER_USE
	idle_power_usage = 0
	density = TRUE
	circuit = /obj/item/circuitboard/machine/spaceship_navigation_beacon

	var/locked = FALSE //Locked beacons don't allow to jump to it.


/obj/machinery/spaceship_navigation_beacon/Initialize()
	. = ..()
	SSshuttle.beacons |= src

obj/machinery/spaceship_navigation_beacon/emp_act()
	locked = TRUE

/obj/machinery/spaceship_navigation_beacon/Destroy()
	SSshuttle.beacons -= src
	return ..()

// update the icon_state
/obj/machinery/spaceship_navigation_beacon/update_icon_state()
	if(powered())
		icon_state = "core"
	else
		icon_state = "core-open"

/obj/machinery/spaceship_navigation_beacon/multitool_act(mob/living/user, obj/item/multitool/I)
	..()
	if(panel_open)
		var/new_name = "Beacon_[input("Enter the custom name for this beacon", "It be Beacon ..your input..") as text|null]"
		if(new_name && Adjacent(user))
			name = new_name
			to_chat(user, "<span class='notice'>You change beacon name to [name].</span>")
	else
		locked =!locked
		to_chat(user, "<span class='notice'>You [locked ? "" : "un"]lock [src].</span>")
	return TRUE

/obj/machinery/spaceship_navigation_beacon/examine()
	.=..()
	. += "<span class='[locked ? "warning" : "nicegreen"]'>Status: [locked ? "LOCKED" : "Stable"] </span>"

/obj/machinery/spaceship_navigation_beacon/attackby(obj/item/W, mob/user, params)
	if(default_deconstruction_screwdriver(user, "core-open", "core", W))
		return
	if(default_deconstruction_crowbar(W))
		return

	return ..()
