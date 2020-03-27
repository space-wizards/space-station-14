GLOBAL_LIST_EMPTY(monkey_recyclers)

/obj/machinery/monkey_recycler
	name = "monkey recycler"
	desc = "A machine used for recycling dead monkeys into monkey cubes."
	icon = 'icons/obj/kitchen.dmi'
	icon_state = "grinder"
	layer = BELOW_OBJ_LAYER
	density = TRUE
	use_power = IDLE_POWER_USE
	idle_power_usage = 5
	active_power_usage = 50
	circuit = /obj/item/circuitboard/machine/monkey_recycler
	var/stored_matter = 0
	var/cube_production = 0.2
	var/list/connected = list()			//Keeps track of connected xenobio consoles, for deletion in /Destroy()

/obj/machinery/monkey_recycler/Initialize(mapload)
	. = ..()
	if (mapload)
		GLOB.monkey_recyclers += src

/obj/machinery/monkey_recycler/Destroy()
	GLOB.monkey_recyclers -= src
	for(var/thing in connected)
		var/obj/machinery/computer/camera_advanced/xenobio/console = thing
		console.connected_recycler = null
	connected.Cut()
	return ..()

/obj/machinery/monkey_recycler/RefreshParts()	//Ranges from 0.2 to 0.8 per monkey recycled
	cube_production = 0
	for(var/obj/item/stock_parts/manipulator/B in component_parts)
		cube_production += B.rating * 0.1
	for(var/obj/item/stock_parts/matter_bin/M in component_parts)
		cube_production += M.rating * 0.1

/obj/machinery/monkey_recycler/examine(mob/user)
	. = ..()
	if(in_range(user, src) || isobserver(user))
		. += "<span class='notice'>The status display reads: Producing <b>[cube_production]</b> cubes for every monkey inserted.</span>"

/obj/machinery/monkey_recycler/attackby(obj/item/O, mob/user, params)
	if(default_deconstruction_screwdriver(user, "grinder_open", "grinder", O))
		return

	if(default_pry_open(O))
		return

	if(default_unfasten_wrench(user, O))
		power_change()
		return

	if(default_deconstruction_crowbar(O))
		return

	if(stat) //NOPOWER etc
		return
	else
		return ..()

/obj/machinery/monkey_recycler/MouseDrop_T(mob/living/target, mob/living/user)
	if(!istype(target))
		return
	if(ismonkey(target))
		stuff_monkey_in(target, user)

/obj/machinery/monkey_recycler/proc/stuff_monkey_in(mob/living/carbon/monkey/target, mob/living/user)
	if(!istype(target))
		return
	if(target.stat == CONSCIOUS)
		to_chat(user, "<span class='warning'>The monkey is struggling far too much to put it in the recycler.</span>")
		return
	if(target.buckled || target.has_buckled_mobs())
		to_chat(user, "<span class='warning'>The monkey is attached to something.</span>")
		return
	qdel(target)
	to_chat(user, "<span class='notice'>You stuff the monkey into the machine.</span>")
	playsound(src.loc, 'sound/machines/juicer.ogg', 50, TRUE)
	var/offset = prob(50) ? -2 : 2
	animate(src, pixel_x = pixel_x + offset, time = 0.2, loop = 200) //start shaking
	use_power(500)
	stored_matter += cube_production
	addtimer(VARSET_CALLBACK(src, pixel_x, initial(pixel_x)))
	addtimer(CALLBACK(GLOBAL_PROC, /proc/to_chat, user, "<span class='notice'>The machine now has [stored_matter] monkey\s worth of material stored.</span>"))

/obj/machinery/monkey_recycler/interact(mob/user)
	if(stored_matter >= 1)
		to_chat(user, "<span class='notice'>The machine hisses loudly as it condenses the ground monkey meat. After a moment, it dispenses a brand new monkey cube.</span>")
		playsound(src.loc, 'sound/machines/hiss.ogg', 50, TRUE)
		for(var/i in 1 to FLOOR(stored_matter, 1))
			new /obj/item/reagent_containers/food/snacks/monkeycube(src.loc)
			stored_matter--
		to_chat(user, "<span class='notice'>The machine's display flashes that it has [stored_matter] monkeys worth of material left.</span>")
	else
		to_chat(user, "<span class='danger'>The machine needs at least 1 monkey worth of material to produce a monkey cube. It currently has [stored_matter].</span>")

/obj/machinery/monkey_recycler/multitool_act(mob/living/user, obj/item/multitool/I)
	. = ..()
	if(istype(I))
		to_chat(user, "<span class='notice'>You log [src] in the multitool's buffer.</span>")
		I.buffer = src
		return TRUE
