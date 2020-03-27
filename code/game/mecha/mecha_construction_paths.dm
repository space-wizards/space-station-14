////////////////////////////////
///// Construction datums //////
////////////////////////////////
/datum/component/construction/mecha
	var/base_icon

/datum/component/construction/mecha/spawn_result()
	if(!result)
		return
	// Remove default mech power cell, as we replace it with a new one.
	var/obj/mecha/M = new result(drop_location())
	QDEL_NULL(M.cell)
	QDEL_NULL(M.scanmod)
	QDEL_NULL(M.capacitor)

	var/obj/item/mecha_parts/chassis/parent_chassis = parent
	M.CheckParts(parent_chassis.contents)

	SSblackbox.record_feedback("tally", "mechas_created", 1, M.name)
	QDEL_NULL(parent)

/datum/component/construction/mecha/update_parent(step_index)
	..()
	// By default, each step in mech construction has a single icon_state:
	// "[base_icon][index - 1]"
	// For example, Ripley's step 1 icon_state is "ripley0".
	var/atom/parent_atom = parent
	if(!steps[index]["icon_state"] && base_icon)
		parent_atom.icon_state = "[base_icon][index - 1]"

/datum/component/construction/unordered/mecha_chassis/custom_action(obj/item/I, mob/living/user, typepath)
	. = user.transferItemToLoc(I, parent)
	if(.)
		var/atom/parent_atom = parent
		user.visible_message("<span class='notice'>[user] has connected [I] to [parent].</span>", "<span class='notice'>You connect [I] to [parent].</span>")
		parent_atom.add_overlay(I.icon_state+"+o")
		qdel(I)

/datum/component/construction/unordered/mecha_chassis/spawn_result()
	var/atom/parent_atom = parent
	parent_atom.icon = 'icons/mecha/mech_construction.dmi'
	parent_atom.density = TRUE
	parent_atom.cut_overlays()
	..()


/datum/component/construction/unordered/mecha_chassis/ripley
	result = /datum/component/construction/mecha/ripley
	steps = list(
		/obj/item/mecha_parts/part/ripley_torso,
		/obj/item/mecha_parts/part/ripley_left_arm,
		/obj/item/mecha_parts/part/ripley_right_arm,
		/obj/item/mecha_parts/part/ripley_left_leg,
		/obj/item/mecha_parts/part/ripley_right_leg
	)

/datum/component/construction/mecha/ripley
	result = /obj/mecha/working/ripley
	base_icon = "ripley"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/ripley/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/ripley/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed."
		),

		//9
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//14
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed."
		),

		//15
		list(
			"key" = /obj/item/stack/sheet/metal,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured."
		),

		//16
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Outer plating is installed."
		),

		//17
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Outer Plating is wrenched."
		),

		//18
		list(
			"key" = /obj/item/stack/rods,
			"amount" = 10,
			"back_key" = TOOL_WELDER,
			"desc" = "Outer Plating is welded."
		),

		//19
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WIRECUTTER,
			"desc" = "Cockpit wire screen is installed."
		),
	)

/datum/component/construction/mecha/ripley/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures [I].</span>", "<span class='notice'>You secure [I].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I].</span>", "<span class='notice'>You install [I].</span>")
			else
				user.visible_message("<span class='notice'>[user] unsecures the capacitor from [parent].</span>", "<span class='notice'>You unsecure the capacitor from [parent].</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the internal armor layer to [parent].</span>", "<span class='notice'>You install the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the internal armor layer.</span>", "<span class='notice'>You secure the internal armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries internal armor layer from [parent].</span>", "<span class='notice'>You pry internal armor layer from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the internal armor layer to [parent].</span>", "<span class='notice'>You weld the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the internal armor layer.</span>", "<span class='notice'>You unfasten the internal armor layer.</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the external reinforced armor layer to [parent].</span>", "<span class='notice'>You install the external reinforced armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts the internal armor layer from [parent].</span>", "<span class='notice'>You cut the internal armor layer from [parent].</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the external armor layer.</span>", "<span class='notice'>You secure the external reinforced armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries external armor layer from [parent].</span>", "<span class='notice'>You pry external armor layer from [parent].</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the external armor layer to [parent].</span>", "<span class='notice'>You weld the external armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the external armor layer.</span>", "<span class='notice'>You unfasten the external armor layer.</span>")
	return TRUE

/datum/component/construction/unordered/mecha_chassis/gygax
	result = /datum/component/construction/mecha/gygax
	steps = list(
		/obj/item/mecha_parts/part/gygax_torso,
		/obj/item/mecha_parts/part/gygax_left_arm,
		/obj/item/mecha_parts/part/gygax_right_arm,
		/obj/item/mecha_parts/part/gygax_left_leg,
		/obj/item/mecha_parts/part/gygax_right_leg,
		/obj/item/mecha_parts/part/gygax_head
	)

/datum/component/construction/mecha/gygax
	result = /obj/mecha/combat/gygax
	base_icon = "gygax"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/gygax/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/gygax/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed."
		),

		//9
		list(
			"key" = /obj/item/circuitboard/mecha/gygax/targeting,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Weapon control module is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Weapon control module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//14
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//15
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//16
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed."
		),

		//17
		list(
			"key" = /obj/item/stack/sheet/metal,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured."
		),

		//18
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Internal armor is installed."
		),

		//19
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Internal armor is wrenched."
		),

		//20
		list(
			"key" = /obj/item/mecha_parts/part/gygax_armor,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_WELDER,
			"desc" = "Internal armor is welded."
		),

		//21
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is installed."
		),

		//22
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "External armor is wrenched."
		),

	)

/datum/component/construction/mecha/gygax/action(datum/source, atom/used_atom, mob/user)
	return check_step(used_atom,user)

/datum/component/construction/mecha/gygax/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the weapon control module.</span>", "<span class='notice'>You secure the weapon control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the weapon control module from [parent].</span>", "<span class='notice'>You remove the weapon control module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the weapon control module.</span>", "<span class='notice'>You unfasten the weapon control module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the capacitor.</span>", "<span class='notice'>You secure the capacitor.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the capacitor.</span>", "<span class='notice'>You unfasten the capacitor.</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the internal armor layer to [parent].</span>", "<span class='notice'>You install the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the internal armor layer.</span>", "<span class='notice'>You secure the internal armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries internal armor layer from [parent].</span>", "<span class='notice'>You pry internal armor layer from [parent].</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the internal armor layer to [parent].</span>", "<span class='notice'>You weld the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the internal armor layer.</span>", "<span class='notice'>You unfasten the internal armor layer.</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts the internal armor layer from [parent].</span>", "<span class='notice'>You cut the internal armor layer from [parent].</span>")
		if(21)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures Gygax Armor Plates.</span>", "<span class='notice'>You secure Gygax Armor Plates.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries Gygax Armor Plates from [parent].</span>", "<span class='notice'>You pry Gygax Armor Plates from [parent].</span>")
		if(22)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds Gygax Armor Plates to [parent].</span>", "<span class='notice'>You weld Gygax Armor Plates to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens Gygax Armor Plates.</span>", "<span class='notice'>You unfasten Gygax Armor Plates.</span>")
	return TRUE

/datum/component/construction/unordered/mecha_chassis/firefighter
	result = /datum/component/construction/mecha/firefighter
	steps = list(
		/obj/item/mecha_parts/part/ripley_torso,
		/obj/item/mecha_parts/part/ripley_left_arm,
		/obj/item/mecha_parts/part/ripley_right_arm,
		/obj/item/mecha_parts/part/ripley_left_leg,
		/obj/item/mecha_parts/part/ripley_right_leg,
		/obj/item/clothing/suit/fire
	)

/datum/component/construction/mecha/firefighter
	result = /obj/mecha/working/ripley/firefighter
	base_icon = "fireripley"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/ripley/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/ripley/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed."
		),
		//9
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//14
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed."
		),

		//15
		list(
			"key" = /obj/item/stack/sheet/plasteel,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured."
		),

		//16
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Internal armor is installed."
		),

		//13
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Internal armor is wrenched."
		),

		//17
		list(
			"key" = /obj/item/stack/sheet/plasteel,
			"amount" = 5,
			"back_key" = TOOL_WELDER,
			"desc" = "Internal armor is welded."
		),

		//18
		list(
			"key" = /obj/item/stack/sheet/plasteel,
			"amount" = 5,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is being installed."
		),

		//19
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is installed."
		),

		//20
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "External armor is wrenched."
		),
	)

/datum/component/construction/mecha/firefighter/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	//TODO: better messages.
	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I]into [parent].</span>", "<span class='notice'>You install [I]into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the capacitor.</span>", "<span class='notice'>You secure the capacitor.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the capacitor.</span>", "<span class='notice'>You unfasten the capacitor.</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the internal armor layer to [parent].</span>", "<span class='notice'>You install the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the internal armor layer.</span>", "<span class='notice'>You secure the internal armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries internal armor layer from [parent].</span>", "<span class='notice'>You pry internal armor layer from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the internal armor layer to [parent].</span>", "<span class='notice'>You weld the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the internal armor layer.</span>", "<span class='notice'>You unfasten the internal armor layer.</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] starts to install the external armor layer to [parent].</span>", "<span class='notice'>You install the external armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts the internal armor layer from [parent].</span>", "<span class='notice'>You cut the internal armor layer from [parent].</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the external reinforced armor layer to [parent].</span>", "<span class='notice'>You install the external reinforced armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the external armor from [parent].</span>", "<span class='notice'>You remove the external armor from [parent].</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the external armor layer.</span>", "<span class='notice'>You secure the external reinforced armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries external armor layer from [parent].</span>", "<span class='notice'>You pry external armor layer from [parent].</span>")
		if(21)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the external armor layer to [parent].</span>", "<span class='notice'>You weld the external armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the external armor layer.</span>", "<span class='notice'>You unfasten the external armor layer.</span>")
	return TRUE

/datum/component/construction/unordered/mecha_chassis/honker
	result = /datum/component/construction/mecha/honker
	steps = list(
		/obj/item/mecha_parts/part/honker_torso,
		/obj/item/mecha_parts/part/honker_left_arm,
		/obj/item/mecha_parts/part/honker_right_arm,
		/obj/item/mecha_parts/part/honker_left_leg,
		/obj/item/mecha_parts/part/honker_right_leg,
		/obj/item/mecha_parts/part/honker_head
	)

/datum/component/construction/mecha/honker
	result = /obj/mecha/combat/honker
	steps = list(
		//1
		list(
			"key" = /obj/item/bikehorn
		),

		//2
		list(
			"key" = /obj/item/circuitboard/mecha/honker/main,
			"action" = ITEM_DELETE
		),

		//3
		list(
			"key" = /obj/item/bikehorn
		),

		//4
		list(
			"key" = /obj/item/circuitboard/mecha/honker/peripherals,
			"action" = ITEM_DELETE
		),

		//5
		list(
			"key" = /obj/item/bikehorn
		),

		//6
		list(
			"key" = /obj/item/circuitboard/mecha/honker/targeting,
			"action" = ITEM_DELETE
		),

		//7
		list(
			"key" = /obj/item/bikehorn
		),

		//6
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE
		),

		//8
		list(
			"key" = /obj/item/bikehorn
		),

		//9
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE
		),

		//10
		list(
			"key" = /obj/item/bikehorn
		),

		//11
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE
		),

		//12
		list(
			"key" = /obj/item/bikehorn
		),

		//13
		list(
			"key" = /obj/item/clothing/mask/gas/clown_hat,
			"action" = ITEM_DELETE
		),

		//14
		list(
			"key" = /obj/item/bikehorn
		),

		//15
		list(
			"key" = /obj/item/clothing/shoes/clown_shoes,
			"action" = ITEM_DELETE
		),

		//16
		list(
			"key" = /obj/item/bikehorn
		),
	)

// HONK doesn't have any construction step icons, so we just set an icon once.
/datum/component/construction/mecha/honker/update_parent(step_index)
	if(step_index == 1)
		var/atom/parent_atom = parent
		parent_atom.icon = 'icons/mecha/mech_construct.dmi'
		parent_atom.icon_state = "honker_chassis"
	..()

/datum/component/construction/mecha/honker/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	if(istype(I, /obj/item/bikehorn))
		playsound(parent, 'sound/items/bikehorn.ogg', 50, TRUE)
		user.visible_message("<span class='danger'>HONK!</span>")

	//TODO: better messages.
	switch(index)
		if(2)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(4)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(6)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(8)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(10)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(12)
			user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
		if(14)
			user.visible_message("<span class='notice'>[user] puts [I] on [parent].</span>", "<span class='notice'>You put [I] on [parent].</span>")
		if(16)
			user.visible_message("<span class='notice'>[user] puts [I] on [parent].</span>", "<span class='notice'>You put [I] on [parent].</span>")
	return TRUE

/datum/component/construction/unordered/mecha_chassis/durand
	result = /datum/component/construction/mecha/durand
	steps = list(
		/obj/item/mecha_parts/part/durand_torso,
		/obj/item/mecha_parts/part/durand_left_arm,
		/obj/item/mecha_parts/part/durand_right_arm,
		/obj/item/mecha_parts/part/durand_left_leg,
		/obj/item/mecha_parts/part/durand_right_leg,
		/obj/item/mecha_parts/part/durand_head
	)

/datum/component/construction/mecha/durand
	result = /obj/mecha/combat/durand
	base_icon = "durand"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/durand/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/durand/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed."
		),

		//9
		list(
			"key" = /obj/item/circuitboard/mecha/durand/targeting,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Weapon control module is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Weapon control module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//14
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//15
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//16
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed."
		),

		//17
		list(
			"key" = /obj/item/stack/sheet/metal,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured."
		),

		//18
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Internal armor is installed."
		),

		//19
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Internal armor is wrenched."
		),

		//20
		list(
			"key" = /obj/item/mecha_parts/part/durand_armor,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_WELDER,
			"desc" = "Internal armor is welded."
		),

		//21
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is installed."
		),

		//22
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "External armor is wrenched."
		),
	)


/datum/component/construction/mecha/durand/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	//TODO: better messages.
	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the weapon control module.</span>", "<span class='notice'>You secure the weapon control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the weapon control module from [parent].</span>", "<span class='notice'>You remove the weapon control module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the weapon control module.</span>", "<span class='notice'>You unfasten the weapon control module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the capacitor.</span>", "<span class='notice'>You secure the capacitor.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the capacitor.</span>", "<span class='notice'>You unfasten the capacitor.</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the internal armor layer to [parent].</span>", "<span class='notice'>You install the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the internal armor layer.</span>", "<span class='notice'>You secure the internal armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries internal armor layer from [parent].</span>", "<span class='notice'>You pry internal armor layer from [parent].</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the internal armor layer to [parent].</span>", "<span class='notice'>You weld the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the internal armor layer.</span>", "<span class='notice'>You unfasten the internal armor layer.</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts the internal armor layer from [parent].</span>", "<span class='notice'>You cut the internal armor layer from [parent].</span>")
		if(21)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures Durand Armor Plates.</span>", "<span class='notice'>You secure Durand Armor Plates.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries Durand Armor Plates from [parent].</span>", "<span class='notice'>You pry Durand Armor Plates from [parent].</span>")
		if(22)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds Durand Armor Plates to [parent].</span>", "<span class='notice'>You weld Durand Armor Plates to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens Durand Armor Plates.</span>", "<span class='notice'>You unfasten Durand Armor Plates.</span>")
	return TRUE

//PHAZON

/datum/component/construction/unordered/mecha_chassis/phazon
	result = /datum/component/construction/mecha/phazon
	steps = list(
		/obj/item/mecha_parts/part/phazon_torso,
		/obj/item/mecha_parts/part/phazon_left_arm,
		/obj/item/mecha_parts/part/phazon_right_arm,
		/obj/item/mecha_parts/part/phazon_left_leg,
		/obj/item/mecha_parts/part/phazon_right_leg,
		/obj/item/mecha_parts/part/phazon_head
	)

/datum/component/construction/mecha/phazon
	result = /obj/mecha/combat/phazon
	base_icon = "phazon"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/phazon/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/phazon/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed"
		),

		//9
		list(
			"key" = /obj/item/circuitboard/mecha/phazon/targeting,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Weapon control is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Weapon control module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//14
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//15
		list(
			"key" = /obj/item/stack/ore/bluespace_crystal,
			"amount" = 1,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//16
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The bluespace crystal is installed."
		),

		//17
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WIRECUTTER,
			"desc" = "The bluespace crystal is connected."
		),

		//18
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The bluespace crystal is engaged."
		),

		//19
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed.",
			"icon_state" = "phazon17"
			// This is the point where a step icon is skipped, so "icon_state" had to be set manually starting from here.
		),

		//20
		list(
			"key" = /obj/item/stack/sheet/plasteel,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured.",
			"icon_state" = "phazon18"
		),

		//21
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Phase armor is installed.",
			"icon_state" = "phazon19"
		),

		//22
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Phase armor is wrenched.",
			"icon_state" = "phazon20"
		),

		//23
		list(
			"key" = /obj/item/mecha_parts/part/phazon_armor,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_WELDER,
			"desc" = "Phase armor is welded.",
			"icon_state" = "phazon21"
		),

		//24
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is installed.",
			"icon_state" = "phazon22"
		),

		//25
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "External armor is wrenched.",
			"icon_state" = "phazon23"
		),

		//26
		list(
			"key" = /obj/item/assembly/signaler/anomaly,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_WELDER,
			"desc" = "Anomaly core socket is open.",
			"icon_state" = "phazon24"
		),
	)


/datum/component/construction/mecha/phazon/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	//TODO: better messages.
	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the weapon control module.</span>", "<span class='notice'>You secure the weapon control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the weapon control module from [parent].</span>", "<span class='notice'>You remove the weapon control module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the weapon control module.</span>", "<span class='notice'>You unfasten the weapon control module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the capacitor.</span>", "<span class='notice'>You secure the capacitor.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I].</span>", "<span class='notice'>You install [I].</span>")
			else
				user.visible_message("<span class='notice'>[user] unsecures the capacitor from [parent].</span>", "<span class='notice'>You unsecure the capacitor from [parent].</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] connects the bluespace crystal.</span>", "<span class='notice'>You connect the bluespace crystal.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the bluespace crystal from [parent].</span>", "<span class='notice'>You remove the bluespace crystal from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] engages the bluespace crystal.</span>", "<span class='notice'>You engage the bluespace crystal.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the bluespace crystal from [parent].</span>", "<span class='notice'>You disconnect the bluespace crystal from [parent].</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disengages the bluespace crystal.</span>", "<span class='notice'>You disengage the bluespace crystal.</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the phase armor layer to [parent].</span>", "<span class='notice'>You install the phase armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(21)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the phase armor layer.</span>", "<span class='notice'>You secure the phase armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the phase armor layer from [parent].</span>", "<span class='notice'>You pry the phase armor layer from [parent].</span>")
		if(22)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the phase armor layer to [parent].</span>", "<span class='notice'>You weld the phase armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the phase armor layer.</span>", "<span class='notice'>You unfasten the phase armor layer.</span>")
		if(23)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts phase armor layer from [parent].</span>", "<span class='notice'>You cut the phase armor layer from [parent].</span>")
		if(24)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures Phazon Armor Plates.</span>", "<span class='notice'>You secure Phazon Armor Plates.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries Phazon Armor Plates from [parent].</span>", "<span class='notice'>You pry Phazon Armor Plates from [parent].</span>")
		if(25)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds Phazon Armor Plates to [parent].</span>", "<span class='notice'>You weld Phazon Armor Plates to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens Phazon Armor Plates.</span>", "<span class='notice'>You unfasten Phazon Armor Plates.</span>")
		if(26)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] carefully inserts the anomaly core into [parent] and secures it.</span>",
					"<span class='notice'>You slowly place the anomaly core into its socket and close its chamber.</span>")
	return TRUE

//ODYSSEUS

/datum/component/construction/unordered/mecha_chassis/odysseus
	result = /datum/component/construction/mecha/odysseus
	steps = list(
		/obj/item/mecha_parts/part/odysseus_torso,
		/obj/item/mecha_parts/part/odysseus_head,
		/obj/item/mecha_parts/part/odysseus_left_arm,
		/obj/item/mecha_parts/part/odysseus_right_arm,
		/obj/item/mecha_parts/part/odysseus_left_leg,
		/obj/item/mecha_parts/part/odysseus_right_leg
	)

/datum/component/construction/mecha/odysseus
	result = /obj/mecha/medical/odysseus
	base_icon = "odysseus"
	steps = list(
		//1
		list(
			"key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are disconnected."
		),

		//2
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_WRENCH,
			"desc" = "The hydraulic systems are connected."
		),

		//3
		list(
			"key" = /obj/item/stack/cable_coil,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The hydraulic systems are active."
		),

		//4
		list(
			"key" = TOOL_WIRECUTTER,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is added."
		),

		//5
		list(
			"key" = /obj/item/circuitboard/mecha/odysseus/main,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The wiring is adjusted."
		),

		//6
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Central control module is installed."
		),

		//7
		list(
			"key" = /obj/item/circuitboard/mecha/odysseus/peripherals,
			"action" = ITEM_DELETE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Central control module is secured."
		),

		//8
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Peripherals control module is installed."
		),
		//9
		list(
			"key" = /obj/item/stock_parts/scanning_module,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Peripherals control module is secured."
		),

		//10
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Scanner module is installed."
		),

		//11
		list(
			"key" = /obj/item/stock_parts/capacitor,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Scanner module is secured."
		),

		//12
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Capacitor is installed."
		),

		//13
		list(
			"key" = /obj/item/stock_parts/cell,
			"action" = ITEM_MOVE_INSIDE,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "Capacitor is secured."
		),

		//11
		list(
			"key" = TOOL_SCREWDRIVER,
			"back_key" = TOOL_CROWBAR,
			"desc" = "The power cell is installed."
		),

		//12
		list(
			"key" = /obj/item/stack/sheet/metal,
			"amount" = 5,
			"back_key" = TOOL_SCREWDRIVER,
			"desc" = "The power cell is secured."
		),

		//13
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "Internal armor is installed."
		),

		//14
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "Internal armor is wrenched."
		),

		//15
		list(
			"key" = /obj/item/stack/sheet/plasteel,
			"amount" = 5,
			"back_key" = TOOL_WELDER,
			"desc" = "Internal armor is welded."
		),

		//16
		list(
			"key" = TOOL_WRENCH,
			"back_key" = TOOL_CROWBAR,
			"desc" = "External armor is installed."
		),

		//17
		list(
			"key" = TOOL_WELDER,
			"back_key" = TOOL_WRENCH,
			"desc" = "External armor is wrenched."
		),
	)

/datum/component/construction/mecha/odysseus/custom_action(obj/item/I, mob/living/user, diff)
	if(!..())
		return FALSE

	//TODO: better messages.
	switch(index)
		if(1)
			user.visible_message("<span class='notice'>[user] connects [parent] hydraulic systems.</span>", "<span class='notice'>You connect [parent] hydraulic systems.</span>")
		if(2)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] activates [parent] hydraulic systems.</span>", "<span class='notice'>You activate [parent] hydraulic systems.</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects [parent] hydraulic systems.</span>", "<span class='notice'>You disconnect [parent] hydraulic systems.</span>")
		if(3)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adds the wiring to [parent].</span>", "<span class='notice'>You add the wiring to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] deactivates [parent] hydraulic systems.</span>", "<span class='notice'>You deactivate [parent] hydraulic systems.</span>")
		if(4)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] adjusts the wiring of [parent].</span>", "<span class='notice'>You adjust the wiring of [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the wiring from [parent].</span>", "<span class='notice'>You remove the wiring from [parent].</span>")
		if(5)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] disconnects the wiring of [parent].</span>", "<span class='notice'>You disconnect the wiring of [parent].</span>")
		if(6)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the mainboard.</span>", "<span class='notice'>You secure the mainboard.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the central control module from [parent].</span>", "<span class='notice'>You remove the central computer mainboard from [parent].</span>")
		if(7)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the mainboard.</span>", "<span class='notice'>You unfasten the mainboard.</span>")
		if(8)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the peripherals control module.</span>", "<span class='notice'>You secure the peripherals control module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the peripherals control module from [parent].</span>", "<span class='notice'>You remove the peripherals control module from [parent].</span>")
		if(9)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the peripherals control module.</span>", "<span class='notice'>You unfasten the peripherals control module.</span>")
		if(10)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the scanner module.</span>", "<span class='notice'>You secure the scanner module.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the scanner module from [parent].</span>", "<span class='notice'>You remove the scanner module from [parent].</span>")
		if(11)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] to [parent].</span>", "<span class='notice'>You install [I] to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the scanner module.</span>", "<span class='notice'>You unfasten the scanner module.</span>")
		if(12)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the capacitor.</span>", "<span class='notice'>You secure the capacitor.</span>")
			else
				user.visible_message("<span class='notice'>[user] removes the capacitor from [parent].</span>", "<span class='notice'>You remove the capacitor from [parent].</span>")
		if(13)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs [I] into [parent].</span>", "<span class='notice'>You install [I] into [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the capacitor.</span>", "<span class='notice'>You unfasten the capacitor.</span>")
		if(14)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the power cell.</span>", "<span class='notice'>You secure the power cell.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the power cell from [parent].</span>", "<span class='notice'>You pry the power cell from [parent].</span>")
		if(15)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the internal armor layer to [parent].</span>", "<span class='notice'>You install the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the power cell.</span>", "<span class='notice'>You unfasten the power cell.</span>")
		if(16)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the internal armor layer.</span>", "<span class='notice'>You secure the internal armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries internal armor layer from [parent].</span>", "<span class='notice'>You pry internal armor layer from [parent].</span>")
		if(17)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the internal armor layer to [parent].</span>", "<span class='notice'>You weld the internal armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the internal armor layer.</span>", "<span class='notice'>You unfasten the internal armor layer.</span>")
		if(18)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] installs the external armor layer to [parent].</span>", "<span class='notice'>You install the external reinforced armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] cuts the internal armor layer from [parent].</span>", "<span class='notice'>You cut the internal armor layer from [parent].</span>")
		if(19)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] secures the external armor layer.</span>", "<span class='notice'>You secure the external reinforced armor layer.</span>")
			else
				user.visible_message("<span class='notice'>[user] pries the external armor layer from [parent].</span>", "<span class='notice'>You pry the external armor layer from [parent].</span>")
		if(20)
			if(diff==FORWARD)
				user.visible_message("<span class='notice'>[user] welds the external armor layer to [parent].</span>", "<span class='notice'>You weld the external armor layer to [parent].</span>")
			else
				user.visible_message("<span class='notice'>[user] unfastens the external armor layer.</span>", "<span class='notice'>You unfasten the external armor layer.</span>")
	return TRUE
