///We pump liquids from activated(plungerated) geysers to a plumbing outlet. We need to be wired.
/obj/machinery/power/liquid_pump
	name = "liquid pump"
	desc = "Pump up those sweet liquids from under the surface."
	icon = 'icons/obj/plumbing/plumbers.dmi'
	icon_state = "pump"
	anchored = FALSE
	density = TRUE
	circuit = /obj/item/circuitboard/machine/pump
	idle_power_usage = 10
	active_power_usage = 1000
	///Are we powered?
	var/powered = FALSE
	///units we pump per process (2 seconds)
	var/pump_power = 2
	///set to true if the loop couldnt find a geyser in process, so it remembers and stops checking every loop until moved. more accurate name would be absolutely_no_geyser_under_me_so_dont_try
	var/geyserless = FALSE
	///The geyser object
	var/obj/structure/geyser/geyser
	///volume of our internal buffer
	var/volume = 200

/obj/machinery/power/liquid_pump/Initialize()
	. = ..()
	create_reagents(volume)
	AddComponent(/datum/component/plumbing/simple_supply, TRUE)

/obj/machinery/power/liquid_pump/attackby(obj/item/W, mob/user, params)
	if(!powered)
		if(!anchored)
			if(default_deconstruction_screwdriver(user, "[initial(icon_state)]_open", "[initial(icon_state)]",W))
				return
		if(default_deconstruction_crowbar(W))
			return
	return ..()

/obj/machinery/power/liquid_pump/wrench_act(mob/living/user, obj/item/I)
	..()
	default_unfasten_wrench(user, I)
	return TRUE
///please note that the component has a hook in the parent call, wich handles activating and deactivating
/obj/machinery/power/liquid_pump/default_unfasten_wrench(mob/user, obj/item/I, time = 20)
	. = ..()
	if(. == SUCCESSFUL_UNFASTEN)
		geyser = null
		update_icon()
		powered = FALSE
		geyserless = FALSE //we switched state, so lets just set this back aswell

/obj/machinery/power/liquid_pump/process()
	if(!anchored || panel_open)
		return
	if(!geyser && !geyserless)
		for(var/obj/structure/geyser/G in loc.contents)
			geyser = G
		if(!geyser) //we didnt find one, abort
			anchored = FALSE
			geyserless = TRUE
			visible_message("<span class='warning'>The [name] makes a sad beep!</span>")
			playsound(src, 'sound/machines/buzz-sigh.ogg', 50)
			return

	if(avail(active_power_usage))
		if(!powered) //we werent powered before this tick so update our sprite
			powered = TRUE
			update_icon()
		add_load(active_power_usage)
		pump()
	else if(powered) //we were powered, but now we arent
		powered = FALSE
		update_icon()
///pump up that sweet geyser nectar
/obj/machinery/power/liquid_pump/proc/pump()
	if(!geyser || !geyser.reagents)
		return
	geyser.reagents.trans_to(src, pump_power)

/obj/machinery/power/liquid_pump/update_icon_state()
	if(powered)
		icon_state = initial(icon_state) + "-on"
	else if(panel_open)
		icon_state = initial(icon_state) + "-open"
	else
		icon_state = initial(icon_state)
