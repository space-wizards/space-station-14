#define PTURRET_UNSECURED  0
#define PTURRET_BOLTED  1
#define PTURRET_START_INTERNAL_ARMOUR  2
#define PTURRET_INTERNAL_ARMOUR_ON  3
#define PTURRET_GUN_EQUIPPED  4
#define PTURRET_SENSORS_ON  5
#define PTURRET_CLOSED  6
#define PTURRET_START_EXTERNAL_ARMOUR  7
#define PTURRET_EXTERNAL_ARMOUR_ON  8

/obj/machinery/porta_turret_construct
	name = "turret frame"
	icon = 'icons/obj/turrets.dmi'
	icon_state = "turret_frame"
	desc = "An unfinished covered turret frame."
	anchored = FALSE
	density = TRUE
	var/build_step = PTURRET_UNSECURED //the current step in the building process
	var/finish_name = "turret"	//the name applied to the product turret
	var/obj/item/gun/installed_gun = null

/obj/machinery/porta_turret_construct/attackby(obj/item/I, mob/user, params)
	//this is a bit unwieldy but self-explanatory
	switch(build_step)
		if(PTURRET_UNSECURED)	//first step
			if(I.tool_behaviour == TOOL_WRENCH && !anchored)
				I.play_tool_sound(src, 100)
				to_chat(user, "<span class='notice'>You secure the external bolts.</span>")
				setAnchored(TRUE)
				build_step = PTURRET_BOLTED
				return

			else if(I.tool_behaviour == TOOL_CROWBAR && !anchored)
				I.play_tool_sound(src, 75)
				to_chat(user, "<span class='notice'>You dismantle the turret construction.</span>")
				new /obj/item/stack/sheet/metal( loc, 5)
				qdel(src)
				return

		if(PTURRET_BOLTED)
			if(istype(I, /obj/item/stack/sheet/metal))
				var/obj/item/stack/sheet/metal/M = I
				if(M.use(2))
					to_chat(user, "<span class='notice'>You add some metal armor to the interior frame.</span>")
					build_step = PTURRET_START_INTERNAL_ARMOUR
					icon_state = "turret_frame2"
				else
					to_chat(user, "<span class='warning'>You need two sheets of metal to continue construction!</span>")
				return

			else if(I.tool_behaviour == TOOL_WRENCH)
				I.play_tool_sound(src, 75)
				to_chat(user, "<span class='notice'>You unfasten the external bolts.</span>")
				setAnchored(FALSE)
				build_step = PTURRET_UNSECURED
				return


		if(PTURRET_START_INTERNAL_ARMOUR)
			if(I.tool_behaviour == TOOL_WRENCH)
				I.play_tool_sound(src, 100)
				to_chat(user, "<span class='notice'>You bolt the metal armor into place.</span>")
				build_step = PTURRET_INTERNAL_ARMOUR_ON
				return

			else if(I.tool_behaviour == TOOL_WELDER)
				if(!I.tool_start_check(user, amount=5)) //uses up 5 fuel
					return

				to_chat(user, "<span class='notice'>You start to remove the turret's interior metal armor...</span>")

				if(I.use_tool(src, user, 20, volume=50, amount=5)) //uses up 5 fuel
					build_step = PTURRET_BOLTED
					to_chat(user, "<span class='notice'>You remove the turret's interior metal armor.</span>")
					new /obj/item/stack/sheet/metal(drop_location(), 2)
					return


		if(PTURRET_INTERNAL_ARMOUR_ON)
			if(istype(I, /obj/item/gun/energy)) //the gun installation part
				var/obj/item/gun/energy/E = I
				if(!user.transferItemToLoc(E, src))
					return
				installed_gun = E
				to_chat(user, "<span class='notice'>You add [I] to the turret.</span>")
				build_step = PTURRET_GUN_EQUIPPED
				return

			else if(I.tool_behaviour == TOOL_WRENCH)
				I.play_tool_sound(src, 100)
				to_chat(user, "<span class='notice'>You remove the turret's metal armor bolts.</span>")
				build_step = PTURRET_START_INTERNAL_ARMOUR
				return

		if(PTURRET_GUN_EQUIPPED)
			if(isprox(I))
				build_step = PTURRET_SENSORS_ON
				if(!user.temporarilyRemoveItemFromInventory(I))
					return
				to_chat(user, "<span class='notice'>You add the proximity sensor to the turret.</span>")
				qdel(I)
				return


		if(PTURRET_SENSORS_ON)
			if(I.tool_behaviour == TOOL_SCREWDRIVER)
				I.play_tool_sound(src, 100)
				build_step = PTURRET_CLOSED
				to_chat(user, "<span class='notice'>You close the internal access hatch.</span>")
				return


		if(PTURRET_CLOSED)
			if(istype(I, /obj/item/stack/sheet/metal))
				var/obj/item/stack/sheet/metal/M = I
				if(M.use(2))
					to_chat(user, "<span class='notice'>You add some metal armor to the exterior frame.</span>")
					build_step = PTURRET_START_EXTERNAL_ARMOUR
				else
					to_chat(user, "<span class='warning'>You need two sheets of metal to continue construction!</span>")
				return

			else if(I.tool_behaviour == TOOL_SCREWDRIVER)
				I.play_tool_sound(src, 100)
				build_step = PTURRET_SENSORS_ON
				to_chat(user, "<span class='notice'>You open the internal access hatch.</span>")
				return

		if(PTURRET_START_EXTERNAL_ARMOUR)
			if(I.tool_behaviour == TOOL_WELDER)
				if(!I.tool_start_check(user, amount=5))
					return

				to_chat(user, "<span class='notice'>You begin to weld the turret's armor down...</span>")
				if(I.use_tool(src, user, 30, volume=50, amount=5))
					build_step = PTURRET_EXTERNAL_ARMOUR_ON
					to_chat(user, "<span class='notice'>You weld the turret's armor down.</span>")

					//The final step: create a full turret

					var/obj/machinery/porta_turret/turret
					//fuck lasertag turrets
					if(istype(installed_gun, /obj/item/gun/energy/laser/bluetag) || istype(installed_gun, /obj/item/gun/energy/laser/redtag))
						turret = new/obj/machinery/porta_turret/lasertag(loc)
					else
						turret = new/obj/machinery/porta_turret(loc)
					turret.name = finish_name
					turret.installation = installed_gun.type
					turret.setup(installed_gun)
					qdel(src)

			else if(I.tool_behaviour == TOOL_CROWBAR)
				I.play_tool_sound(src, 75)
				to_chat(user, "<span class='notice'>You pry off the turret's exterior armor.</span>")
				new /obj/item/stack/sheet/metal(loc, 2)
				build_step = PTURRET_CLOSED
				return

	if(istype(I, /obj/item/pen))	//you can rename turrets like bots!
		var/t = stripped_input(user, "Enter new turret name", name, finish_name)
		if(!t)
			return
		if(!Adjacent(user))
			return

		finish_name = t
		return
	return ..()


/obj/machinery/porta_turret_construct/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	switch(build_step)
		if(PTURRET_GUN_EQUIPPED)
			build_step = PTURRET_INTERNAL_ARMOUR_ON

			installed_gun.forceMove(loc)
			to_chat(user, "<span class='notice'>You remove [installed_gun] from the turret frame.</span>")
			installed_gun = null

		if(PTURRET_SENSORS_ON)
			to_chat(user, "<span class='notice'>You remove the prox sensor from the turret frame.</span>")
			new /obj/item/assembly/prox_sensor(loc)
			build_step = PTURRET_GUN_EQUIPPED

/obj/machinery/porta_turret_construct/attack_ai()
	return

/obj/machinery/porta_turret_construct/Destroy()
	if(installed_gun)
		qdel(installed_gun)
		installed_gun = null
	. = ..()
