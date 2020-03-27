
#define FLOODLIGHT_OFF 1
#define FLOODLIGHT_LOW 2
#define FLOODLIGHT_MED 3
#define FLOODLIGHT_HIGH 4

/obj/structure/floodlight_frame
	name = "floodlight frame"
	desc = "A bare metal frame looking vaguely like a floodlight. Requires wiring."
	max_integrity = 100
	icon = 'icons/obj/lighting.dmi'
	icon_state = "floodlight_c1"
	density = TRUE
	var/state = FLOODLIGHT_NEEDS_WIRES

/obj/structure/floodlight_frame/attackby(obj/item/O, mob/user, params)
	if(istype(O, /obj/item/stack/cable_coil) && state == FLOODLIGHT_NEEDS_WIRES)
		var/obj/item/stack/S = O
		if(S.use(5))
			to_chat(user, "<span class='notice'>You wire [src].</span>")
			name = "wired [name]"
			desc = "A bare metal frame looking vaguely like a floodlight. Requires securing with a screwdriver."
			icon_state = "floodlight_c2"
			state = FLOODLIGHT_NEEDS_SECURING
			return
		else
			to_chat(user, "You need 5 cables to wire [src].")
			return
	if(O.tool_behaviour == TOOL_SCREWDRIVER && state == FLOODLIGHT_NEEDS_SECURING)
		to_chat(user, "<span class='notice'>You fasten the wiring and electronics in [src].</span>")
		name = "secured [name]"
		desc = "A bare metal frame that looks like a floodlight. Requires a light tube to complete."
		icon_state = "floodlight_c3"
		state = FLOODLIGHT_NEEDS_LIGHTS
		return
	if(istype(O, /obj/item/light/tube))
		var/obj/item/light/tube/L = O
		if(state == FLOODLIGHT_NEEDS_LIGHTS && L.status != 2) //Ready for a light tube, and not broken.
			to_chat(user, "<span class='notice'>You put lights in [src].</span>")
			new /obj/machinery/power/floodlight(loc)
			qdel(src)
			qdel(O)
			return
		else //A minute of silence for all the accidentally broken light tubes.
			return
	if(istype(O, /obj/item/lightreplacer))
		var/obj/item/lightreplacer/L = O
		if(state == FLOODLIGHT_NEEDS_LIGHTS && L.CanUse(user))
			L.Use(user)
			to_chat(user, "<span class='notice'>You put lights in [src].</span>")
			new /obj/machinery/power/floodlight(loc)
			qdel(src)
			return
	..()

/obj/machinery/power/floodlight
	name = "floodlight"
	desc = "A pole with powerful mounted lights on it. Due to its high power draw, it must be powered by a direct connection to a wire node."
	icon = 'icons/obj/lighting.dmi'
	icon_state = "floodlight"
	density = TRUE
	max_integrity = 100
	integrity_failure = 0.8
	idle_power_usage = 100
	active_power_usage = 1000
	anchored = FALSE
	light_power = 1.75
	var/list/light_setting_list = list(0, 5, 10, 15)
	var/light_power_coefficient = 200
	var/setting = FLOODLIGHT_OFF

/obj/machinery/power/floodlight/process()
	var/turf/T = get_turf(src)
	var/obj/structure/cable/C = locate() in T
	if(!C && powernet)
		disconnect_from_network()
	if(setting > FLOODLIGHT_OFF) //If on
		if(avail(active_power_usage))
			add_load(active_power_usage)
		else
			change_setting(FLOODLIGHT_OFF)
	else if(avail(idle_power_usage))
		add_load(idle_power_usage)

/obj/machinery/power/floodlight/proc/change_setting(newval, mob/user)
	if((newval < FLOODLIGHT_OFF) || (newval > light_setting_list.len))
		return
	setting = newval
	active_power_usage = light_setting_list[setting] * light_power_coefficient
	if(!avail(active_power_usage) && setting > FLOODLIGHT_OFF)
		return change_setting(setting - 1)
	set_light(light_setting_list[setting], light_power)
	var/setting_text = ""
	if(setting > FLOODLIGHT_OFF)
		icon_state = "[initial(icon_state)]_on"
	else
		icon_state = initial(icon_state)
	switch(setting)
		if(1)
			setting_text = "OFF"
		if(2)
			setting_text = "low power"
		if(3)
			setting_text = "standard lighting"
		if(4)
			setting_text = "high power"
	if(user)
		to_chat(user, "<span class='notice'>You set [src] to [setting_text].</span>")

/obj/machinery/power/floodlight/attackby(obj/item/O, mob/user, params)
	if(O.tool_behaviour == TOOL_WRENCH)
		default_unfasten_wrench(user, O, time = 20)
		change_setting(1)
		if(anchored)
			connect_to_network()
		else
			disconnect_from_network()
	else
		. = ..()

/obj/machinery/power/floodlight/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	var/current = setting
	if(current == 1)
		current = light_setting_list.len
	else
		current--
	change_setting(current, user)
	..()

/obj/machinery/power/floodlight/obj_break(damage_flag)
	. = ..()
	if(!.)
		return
	playsound(loc, 'sound/effects/glassbr3.ogg', 100, TRUE)
	var/obj/structure/floodlight_frame/F = new(loc)
	F.state = FLOODLIGHT_NEEDS_LIGHTS
	new /obj/item/light/tube/broken(loc)
	qdel(src)

/obj/machinery/power/floodlight/play_attack_sound(damage_amount, damage_type = BRUTE, damage_flag = 0)
	playsound(src, 'sound/effects/glasshit.ogg', 75, TRUE)
