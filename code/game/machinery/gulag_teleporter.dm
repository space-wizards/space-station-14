/*
The gulag teleporter teleports a prisoner to the gulag outpost.
It automatically strips the prisoner and equips a prisoner ID, prisoner jumpsuit and oranges sneakers.
You can set the amount of points in the console
The console is located at computer/gulag_teleporter.dm
*/

//Gulag teleporter
/obj/machinery/gulag_teleporter
	name = "labor camp teleporter"
	desc = "A bluespace teleporter used for teleporting prisoners to the labor camp."
	icon = 'icons/obj/machines/implantchair.dmi'
	icon_state = "implantchair"
	state_open = FALSE
	density = TRUE
	use_power = IDLE_POWER_USE
	idle_power_usage = 200
	active_power_usage = 5000
	circuit = /obj/item/circuitboard/machine/gulag_teleporter
	var/locked = FALSE
	var/message_cooldown
	var/breakout_time = 600
	var/jumpsuit_type = /obj/item/clothing/under/rank/prisoner
	var/shoes_type = /obj/item/clothing/shoes/sneakers/orange
	var/obj/machinery/gulag_item_reclaimer/linked_reclaimer
	var/static/list/telegulag_required_items = typecacheof(list(
		/obj/item/implant,
		/obj/item/clothing/suit/space/eva/plasmaman,
		/obj/item/clothing/under/plasmaman,
		/obj/item/clothing/head/helmet/space/plasmaman,
		/obj/item/tank/internals,
		/obj/item/clothing/mask/breath,
		/obj/item/clothing/mask/gas))

/obj/machinery/gulag_teleporter/Initialize()
	. = ..()
	locate_reclaimer()

/obj/machinery/gulag_teleporter/Destroy()
	if(linked_reclaimer)
		linked_reclaimer.linked_teleporter = null
	return ..()

/obj/machinery/gulag_teleporter/interact(mob/user)
	. = ..()
	if(locked)
		to_chat(user, "<span class='warning'>[src] is locked!</span>")
		return
	toggle_open()

/obj/machinery/gulag_teleporter/updateUsrDialog()
	return

/obj/machinery/gulag_teleporter/attackby(obj/item/I, mob/user)
	if(!occupant && default_deconstruction_screwdriver(user, "[icon_state]", "[icon_state]",I))
		update_icon()
		return

	if(default_deconstruction_crowbar(I))
		return

	if(default_pry_open(I))
		return

	return ..()

/obj/machinery/gulag_teleporter/update_icon_state()
	icon_state = initial(icon_state) + (state_open ? "_open" : "")
	//no power or maintenance
	if(stat & (NOPOWER|BROKEN))
		icon_state += "_unpowered"
		if((stat & MAINT) || panel_open)
			icon_state += "_maintenance"
		return

	if((stat & MAINT) || panel_open)
		icon_state += "_maintenance"
		return

	//running and someone in there
	if(occupant)
		icon_state += "_occupied"
		return


/obj/machinery/gulag_teleporter/relaymove(mob/user)
	if(user.stat != CONSCIOUS)
		return
	if(locked)
		if(message_cooldown <= world.time)
			message_cooldown = world.time + 50
			to_chat(user, "<span class='warning'>[src]'s door won't budge!</span>")
		return
	open_machine()

/obj/machinery/gulag_teleporter/container_resist(mob/living/user)
	if(!locked)
		open_machine()
		return
	user.changeNext_move(CLICK_CD_BREAKOUT)
	user.last_special = world.time + CLICK_CD_BREAKOUT
	user.visible_message("<span class='notice'>You see [user] kicking against the door of [src]!</span>", \
		"<span class='notice'>You lean on the back of [src] and start pushing the door open... (this will take about [DisplayTimeText(breakout_time)].)</span>", \
		"<span class='hear'>You hear a metallic creaking from [src].</span>")
	if(do_after(user,(breakout_time), target = src))
		if(!user || user.stat != CONSCIOUS || user.loc != src || state_open || !locked)
			return
		locked = FALSE
		user.visible_message("<span class='warning'>[user] successfully broke out of [src]!</span>", \
			"<span class='notice'>You successfully break out of [src]!</span>")
		open_machine()

/obj/machinery/gulag_teleporter/proc/locate_reclaimer()
	linked_reclaimer = locate(/obj/machinery/gulag_item_reclaimer)
	if(linked_reclaimer)
		linked_reclaimer.linked_teleporter = src

/obj/machinery/gulag_teleporter/proc/toggle_open()
	if(panel_open)
		to_chat(usr, "<span class='notice'>Close the maintenance panel first.</span>")
		return

	if(state_open)
		close_machine()
		return
	if(!locked)
		open_machine()

// strips and stores all occupant's items
/obj/machinery/gulag_teleporter/proc/strip_occupant()
	if(linked_reclaimer)
		linked_reclaimer.stored_items[occupant] = list()
	var/mob/living/mob_occupant = occupant
	for(var/obj/item/W in mob_occupant)
		if(!is_type_in_typecache(W, telegulag_required_items))
			if(mob_occupant.temporarilyRemoveItemFromInventory(W))
				if(istype(W, /obj/item/restraints/handcuffs))
					W.forceMove(get_turf(src))
					continue
				if(linked_reclaimer)
					linked_reclaimer.stored_items[mob_occupant] += W
					linked_reclaimer.contents += W
					W.forceMove(linked_reclaimer)
				else
					W.forceMove(src)

/obj/machinery/gulag_teleporter/proc/handle_prisoner(obj/item/id, datum/data/record/R)
	if(!ishuman(occupant))
		return
	strip_occupant()
	var/mob/living/carbon/human/prisoner = occupant
	if(!isplasmaman(prisoner) && jumpsuit_type)
		prisoner.equip_to_appropriate_slot(new jumpsuit_type)
	if(shoes_type)
		prisoner.equip_to_appropriate_slot(new shoes_type)
	if(id)
		prisoner.equip_to_appropriate_slot(id)
	if(R)
		R.fields["criminal"] = "Incarcerated"

/obj/item/circuitboard/machine/gulag_teleporter
	name = "labor camp teleporter (Machine Board)"
	build_path = /obj/machinery/gulag_teleporter
	req_components = list(
							/obj/item/stack/ore/bluespace_crystal = 2,
							/obj/item/stock_parts/scanning_module,
							/obj/item/stock_parts/manipulator)
	def_components = list(/obj/item/stack/ore/bluespace_crystal = /obj/item/stack/ore/bluespace_crystal/artificial)

/*  beacon that receives the teleported prisoner */
/obj/structure/gulag_beacon
	name = "labor camp bluespace beacon"
	desc = "A receiving beacon for bluespace teleportations."
	icon = 'icons/turf/floors.dmi'
	icon_state = "light_on-w"
	resistance_flags = INDESTRUCTIBLE
	anchored = TRUE
