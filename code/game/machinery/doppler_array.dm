/obj/machinery/doppler_array
	name = "tachyon-doppler array"
	desc = "A highly precise directional sensor array which measures the release of quants from decaying tachyons. The doppler shifting of the mirror-image formed by these quants can reveal the size, location and temporal affects of energetic disturbances within a large radius ahead of the array.\n"
	icon = 'icons/obj/machines/research.dmi'
	icon_state = "tdoppler"
	density = TRUE
	var/cooldown = 10
	var/next_announce = 0
	var/max_dist = 150
	verb_say = "states coldly"
	var/list/message_log = list()

/obj/machinery/doppler_array/Initialize()
	. = ..()
	RegisterSignal(SSdcs, COMSIG_GLOB_EXPLOSION, .proc/sense_explosion)

/obj/machinery/doppler_array/ComponentInitialize()
	. = ..()
	AddComponent(/datum/component/simple_rotation,ROTATION_ALTCLICK | ROTATION_CLOCKWISE,null,null,CALLBACK(src,.proc/rot_message))

/obj/machinery/doppler_array/ui_interact(mob/user)
	. = ..()
	if(stat)
		return FALSE

	var/list/dat = list()
	for(var/i in 1 to message_log.len)
		dat += "Log recording #[i]: [message_log[i]]<br/><br>"
	dat += "<A href='?src=[REF(src)];delete_log=1'>Delete logs</A><br>"
	dat += "<hr>"
	dat += "<A href='?src=[REF(src)];refresh=1'>(Refresh)</A><br>"
	dat += "</body></html>"
	var/datum/browser/popup = new(user, "computer", name, 400, 500)
	popup.set_content(dat.Join(" "))
	popup.open()
	return

/obj/machinery/doppler_array/Topic(href, href_list)
	if(..())
		return
	if(href_list["delete_log"])
		message_log.Cut()
	if(href_list["refresh"])
		updateUsrDialog()

	updateUsrDialog()
	return

/obj/machinery/doppler_array/attackby(obj/item/I, mob/user, params)
	if(I.tool_behaviour == TOOL_WRENCH)
		if(!anchored && !isinspace())
			anchored = TRUE
			power_change()
			to_chat(user, "<span class='notice'>You fasten [src].</span>")
		else if(anchored)
			anchored = FALSE
			power_change()
			to_chat(user, "<span class='notice'>You unfasten [src].</span>")
		I.play_tool_sound(src)
		return
	return ..()

/obj/machinery/doppler_array/proc/rot_message(mob/user)
	to_chat(user, "<span class='notice'>You adjust [src]'s dish to face to the [dir2text(dir)].</span>")
	playsound(src, 'sound/items/screwdriver2.ogg', 50, TRUE)

/obj/machinery/doppler_array/proc/sense_explosion(datum/source, turf/epicenter, devastation_range, heavy_impact_range, light_impact_range,
			took, orig_dev_range, orig_heavy_range, orig_light_range)
	if(stat & NOPOWER)
		return FALSE
	var/turf/zone = get_turf(src)
	if(zone.z != epicenter.z)
		return FALSE

	if(next_announce > world.time)
		return FALSE
	next_announce = world.time + cooldown

	var/distance = get_dist(epicenter, zone)
	var/direct = get_dir(zone, epicenter)

	if(distance > max_dist)
		return FALSE
	if(!(direct & dir))
		return FALSE

	var/list/messages = list("Explosive disturbance detected.",
							 "Epicenter at: grid ([epicenter.x],[epicenter.y]). Temporal displacement of tachyons: [took] seconds.",
							 "Factual: Epicenter radius: [devastation_range]. Outer radius: [heavy_impact_range]. Shockwave radius: [light_impact_range].")

	// If the bomb was capped, say its theoretical size.
	if(devastation_range < orig_dev_range || heavy_impact_range < orig_heavy_range || light_impact_range < orig_light_range)
		messages += "Theoretical: Epicenter radius: [orig_dev_range]. Outer radius: [orig_heavy_range]. Shockwave radius: [orig_light_range]."

	for(var/message in messages)
		say(message)
	LAZYADD(message_log, messages.Join(" "))
	return TRUE

/obj/machinery/doppler_array/powered()
	if(!anchored)
		return FALSE
	return ..()

/obj/machinery/doppler_array/update_icon_state()
	if(stat & BROKEN)
		icon_state = "[initial(icon_state)]-broken"
	else if(powered())
		icon_state = initial(icon_state)
	else
		icon_state = "[initial(icon_state)]-off"

/obj/machinery/doppler_array/research
	name = "tachyon-doppler research array"
	desc = "A specialized tachyon-doppler bomb detection array that uses the results of the highest yield of explosions for research."
	var/datum/techweb/linked_techweb

/obj/machinery/doppler_array/research/sense_explosion(datum/source, turf/epicenter, devastation_range, heavy_impact_range, light_impact_range,
		took, orig_dev_range, orig_heavy_range, orig_light_range) //probably needs a way to ignore admin explosives later on
	. = ..()
	if(!.)
		return
	if(!istype(linked_techweb))
		say("Warning: No linked research system!")
		return

	var/point_gain = 0

	/*****The Point Calculator*****/

	if(orig_light_range < 10)
		say("Explosion not large enough for research calculations.")
		return
	else if(orig_light_range < 4500)
		point_gain = (83300 * orig_light_range) / (orig_light_range + 3000)
	else
		point_gain = TECHWEB_BOMB_POINTCAP

	/*****The Point Capper*****/
	if(point_gain > linked_techweb.largest_bomb_value)
		if(point_gain <= TECHWEB_BOMB_POINTCAP || linked_techweb.largest_bomb_value < TECHWEB_BOMB_POINTCAP)
			var/old_tech_largest_bomb_value = linked_techweb.largest_bomb_value //held so we can pull old before we do math
			linked_techweb.largest_bomb_value = point_gain
			point_gain -= old_tech_largest_bomb_value
			point_gain = min(point_gain,TECHWEB_BOMB_POINTCAP)
		else
			linked_techweb.largest_bomb_value = TECHWEB_BOMB_POINTCAP
			point_gain = 1000
		var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_SCI)
		if(D)
			D.adjust_money(point_gain)
			linked_techweb.add_point_type(TECHWEB_POINT_TYPE_DEFAULT, point_gain)
			say("Explosion details and mixture analyzed and sold to the highest bidder for [point_gain] cr, with a reward of [point_gain] points.")

	else //you've made smaller bombs
		say("Data already captured. Aborting.")
		return


/obj/machinery/doppler_array/research/science/Initialize()
	. = ..()
	linked_techweb = SSresearch.science_tech
