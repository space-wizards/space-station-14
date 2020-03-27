
/*

Contents:
- Assorted ninjadrain_act() procs
- What is Object Oriented Programming

They *could* go in their appropriate files, but this is supposed to be modular

*/


//Needs to return the amount drained from the atom, if no drain on a power object, return FALSE, otherwise, return a define.
/atom/proc/ninjadrain_act()
	return INVALID_DRAIN




//APC//
/obj/machinery/power/apc/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	var/maxcapacity = 0 //Safety check for batteries
	var/drain = 0 //Drain amount from batteries

	. = 0

	if(cell && cell.charge)
		var/datum/effect_system/spark_spread/spark_system = new /datum/effect_system/spark_spread()
		spark_system.set_up(5, 0, loc)

		while(G.candrain && cell.charge> 0 && !maxcapacity)
			drain = rand(G.mindrain, G.maxdrain)

			if(cell.charge < drain)
				drain = cell.charge

			if(S.cell.charge + drain > S.cell.maxcharge)
				drain = S.cell.maxcharge - S.cell.charge
				maxcapacity = 1//Reached maximum battery capacity.

			if (do_after(H,10, target = src))
				spark_system.start()
				playsound(loc, "sparks", 50, TRUE)
				cell.use(drain)
				S.cell.give(drain)
				. += drain
			else
				break

		if(!(obj_flags & EMAGGED))
			flick("apc-spark", G)
			playsound(loc, "sparks", 50, TRUE)
			obj_flags |= EMAGGED
			locked = FALSE
			update_icon()





//SMES//
/obj/machinery/power/smes/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	var/maxcapacity = 0 //Safety check for batteries
	var/drain = 0 //Drain amount from batteries

	. = 0

	if(charge)
		var/datum/effect_system/spark_spread/spark_system = new /datum/effect_system/spark_spread()
		spark_system.set_up(5, 0, loc)

		while(G.candrain && charge > 0 && !maxcapacity)
			drain = rand(G.mindrain, G.maxdrain)

			if(charge < drain)
				drain = charge

			if(S.cell.charge + drain > S.cell.maxcharge)
				drain = S.cell.maxcharge - S.cell.charge
				maxcapacity = 1

			if (do_after(H,10, target = src))
				spark_system.start()
				playsound(loc, "sparks", 50, TRUE)
				charge -= drain
				S.cell.give(drain)
				. += drain

			else
				break


//CELL//
/obj/item/stock_parts/cell/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	. = 0

	if(charge)
		if(G.candrain && do_after(H,30, target = src))
			. = charge
			if(S.cell.charge + charge > S.cell.maxcharge)
				S.cell.charge = S.cell.maxcharge
			else
				S.cell.give(charge)
			charge = 0
			corrupt()
			update_icon()

/obj/machinery/proc/AI_notify_hack()
	var/turf/location = get_turf(src)
	var/alertstr = "<span class='userdanger'>Network Alert: Hacking attempt detected[location?" in [location]":". Unable to pinpoint location"]</span>."
	for(var/mob/living/silicon/ai/AI in GLOB.player_list)
		to_chat(AI, alertstr)

//RDCONSOLE//
/obj/machinery/computer/rdconsole/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	. = DRAIN_RD_HACK_FAILED

	to_chat(H, "<span class='notice'>Hacking \the [src]...</span>")
	AI_notify_hack()

	if(stored_research)
		to_chat(H, "<span class='notice'>Copying files...</span>")
		if(do_after(H, S.s_delay, target = src) && G.candrain && src)
			stored_research.copy_research_to(S.stored_research)
	to_chat(H, "<span class='notice'>Data analyzed. Process finished.</span>")

//RD SERVER//
//Shamelessly copypasted from above, since these two used to be the same proc, but with MANY colon operators
/obj/machinery/rnd/server/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	. = DRAIN_RD_HACK_FAILED

	to_chat(H, "<span class='notice'>Hacking \the [src]...</span>")
	AI_notify_hack()

	if(stored_research)
		to_chat(H, "<span class='notice'>Copying files...</span>")
		if(do_after(H, S.s_delay, target = src) && G.candrain && src)
			stored_research.copy_research_to(S.stored_research)
	to_chat(H, "<span class='notice'>Data analyzed. Process finished.</span>")


//WIRE//
/obj/structure/cable/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	var/maxcapacity = 0 //Safety check
	var/drain = 0 //Drain amount

	. = 0

	var/datum/powernet/PN = powernet
	while(G.candrain && !maxcapacity && src)
		drain = (round((rand(G.mindrain, G.maxdrain))/2))
		var/drained = 0
		if(PN && do_after(H,10, target = src))
			drained = min(drain, delayed_surplus())
			add_delayedload(drained)
			if(drained < drain)//if no power on net, drain apcs
				for(var/obj/machinery/power/terminal/T in PN.nodes)
					if(istype(T.master, /obj/machinery/power/apc))
						var/obj/machinery/power/apc/AP = T.master
						if(AP.operating && AP.cell && AP.cell.charge > 0)
							AP.cell.charge = max(0, AP.cell.charge - 5)
							drained += 5
		else
			break

		S.cell.give(drain)
		if(S.cell.charge > S.cell.maxcharge)
			. += (drained-(S.cell.charge - S.cell.maxcharge))
			S.cell.charge = S.cell.maxcharge
			maxcapacity = 1
		else
			. += drained
		S.spark_system.start()

//MECH//
/obj/mecha/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	var/maxcapacity = 0 //Safety check
	var/drain = 0 //Drain amount
	. = 0

	occupant_message("<span class='danger'>Warning: Unauthorized access through sub-route 4, block H, detected.</span>")
	if(get_charge())
		while(G.candrain && cell.charge > 0 && !maxcapacity)
			drain = rand(G.mindrain,G.maxdrain)
			if(cell.charge < drain)
				drain = cell.charge
			if(S.cell.charge + drain > S.cell.maxcharge)
				drain = S.cell.maxcharge - S.cell.charge
				maxcapacity = 1
			if (do_after(H,10, target = src))
				spark_system.start()
				playsound(loc, "sparks", 50, TRUE)
				cell.use(drain)
				S.cell.give(drain)
				. += drain
			else
				break

//BORG//
/mob/living/silicon/robot/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	var/maxcapacity = 0 //Safety check
	var/drain = 0 //Drain amount
	. = 0

	to_chat(src, "<span class='danger'>Warning: Unauthorized access through sub-route 12, block C, detected.</span>")

	if(cell && cell.charge)
		while(G.candrain && cell.charge > 0 && !maxcapacity)
			drain = rand(G.mindrain,G.maxdrain)
			if(cell.charge < drain)
				drain = cell.charge
			if(S.cell.charge+drain > S.cell.maxcharge)
				drain = S.cell.maxcharge - S.cell.charge
				maxcapacity = 1
			if (do_after(H,10))
				spark_system.start()
				playsound(loc, "sparks", 50, TRUE)
				cell.use(drain)
				S.cell.give(drain)
				. += drain
			else
				break


//CARBON MOBS//
/mob/living/carbon/ninjadrain_act(obj/item/clothing/suit/space/space_ninja/S, mob/living/carbon/human/H, obj/item/clothing/gloves/space_ninja/G)
	if(!S || !H || !G)
		return INVALID_DRAIN

	. = DRAIN_MOB_SHOCK_FAILED

	//Default cell = 10,000 charge, 10,000/1000 = 10 uses without charging/upgrading
	if(S.cell && S.cell.charge && S.cell.use(1000))
		. = DRAIN_MOB_SHOCK
		//Got that electric touch
		var/datum/effect_system/spark_spread/spark_system = new /datum/effect_system/spark_spread()
		spark_system.set_up(5, 0, loc)
		playsound(src, "sparks", 50, TRUE)
		visible_message("<span class='danger'>[H] electrocutes [src] with [H.p_their()] touch!</span>", "<span class='userdanger'>[H] electrocutes you with [H.p_their()] touch!</span>")
		electrocute_act(25, H)
