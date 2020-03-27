/*
CONTAINS:
SAFES
FLOOR SAFES
*/

//SAFES
/obj/structure/safe
	name = "safe"
	desc = "A huge chunk of metal with a dial embedded in it. Fine print on the dial reads \"Scarborough Arms - 2 tumbler safe, guaranteed thermite resistant, explosion resistant, and assistant resistant.\""
	icon = 'icons/obj/structures.dmi'
	icon_state = "safe"
	anchored = TRUE
	density = TRUE
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | ACID_PROOF
	interaction_flags_atom = INTERACT_ATOM_ATTACK_HAND | INTERACT_ATOM_UI_INTERACT 
	var/open = FALSE		//is the safe open?
	var/tumbler_1_pos	//the tumbler position- from 0 to 72
	var/tumbler_1_open	//the tumbler position to open at- 0 to 72
	var/tumbler_2_pos
	var/tumbler_2_open
	var/dial = 0		//where is the dial pointing?
	var/space = 0		//the combined w_class of everything in the safe
	var/maxspace = 24	//the maximum combined w_class of stuff in the safe
	var/explosion_count = 0	//Tough, but breakable

/obj/structure/safe/Initialize()
	. = ..()
	tumbler_1_pos = rand(0, 71)
	tumbler_1_open = rand(0, 71)

	tumbler_2_pos = rand(0, 71)
	tumbler_2_open = rand(0, 71)


/obj/structure/safe/Initialize(mapload)
	. = ..()

	if(!mapload)
		return

	for(var/obj/item/I in loc)
		if(space >= maxspace)
			return
		if(I.w_class + space <= maxspace)
			space += I.w_class
			I.forceMove(src)


/obj/structure/safe/proc/check_unlocked(mob/user, canhear)
	if(explosion_count > 2)
		return 1
	if(user && canhear)
		if(tumbler_1_pos == tumbler_1_open)
			to_chat(user, "<span class='hear'>You hear a [pick("tonk", "krunk", "plunk")] from [src].</span>")
		if(tumbler_2_pos == tumbler_2_open)
			to_chat(user, "<span class='hear'>You hear a [pick("tink", "krink", "plink")] from [src].</span>")
	if(tumbler_1_pos == tumbler_1_open && tumbler_2_pos == tumbler_2_open)
		if(user)
			visible_message("<i><b>[pick("Spring", "Sprang", "Sproing", "Clunk", "Krunk")]!</b></i>")
		return TRUE
	return FALSE

/obj/structure/safe/proc/decrement(num)
	num -= 1
	if(num < 0)
		num = 71
	return num

/obj/structure/safe/proc/increment(num)
	num += 1
	if(num > 71)
		num = 0
	return num

/obj/structure/safe/update_icon_state()
	if(open)
		icon_state = "[initial(icon_state)]-open"
	else
		icon_state = initial(icon_state)

/obj/structure/safe/ui_interact(mob/user)
	user.set_machine(src)
	var/dat = "<center>"
	dat += "<a href='?src=[REF(src)];open=1'>[open ? "Close" : "Open"] [src]</a> | <a href='?src=[REF(src)];decrement=1'>-</a> [dial] <a href='?src=[REF(src)];increment=1'>+</a>"
	if(open)
		dat += "<table>"
		for(var/i = contents.len, i>=1, i--)
			var/obj/item/P = contents[i]
			dat += "<tr><td><a href='?src=[REF(src)];retrieve=[REF(P)]'>[P.name]</a></td></tr>"
		dat += "</table></center>"
	user << browse("<html><head><title>[name]</title></head><body>[dat]</body></html>", "window=safe;size=350x300")

/obj/structure/safe/Topic(href, href_list)
	if(!ishuman(usr))
		return
	var/mob/living/carbon/human/user = usr

	if(!user.canUseTopic(src, BE_CLOSE))
		return

	var/canhear = FALSE
	if(user.is_holding_item_of_type(/obj/item/clothing/neck/stethoscope))
		canhear = TRUE

	if(href_list["open"])
		if(check_unlocked())
			to_chat(user, "<span class='notice'>You [open ? "close" : "open"] [src].</span>")
			open = !open
			update_icon()
			updateUsrDialog()
			return
		else
			to_chat(user, "<span class='warning'>You can't [open ? "close" : "open"] [src], the lock is engaged!</span>")
			return

	if(href_list["decrement"])
		dial = decrement(dial)
		if(dial == tumbler_1_pos + 1 || dial == tumbler_1_pos - 71)
			tumbler_1_pos = decrement(tumbler_1_pos)
			if(canhear)
				to_chat(user, "<span class='hear'>You hear a [pick("clack", "scrape", "clank")] from [src].</span>")
			if(tumbler_1_pos == tumbler_2_pos + 37 || tumbler_1_pos == tumbler_2_pos - 35)
				tumbler_2_pos = decrement(tumbler_2_pos)
				if(canhear)
					to_chat(user, "<span class='hear'>You hear a [pick("click", "chink", "clink")] from [src].</span>")
			check_unlocked(user, canhear)
		updateUsrDialog()
		return

	if(href_list["increment"])
		dial = increment(dial)
		if(dial == tumbler_1_pos - 1 || dial == tumbler_1_pos + 71)
			tumbler_1_pos = increment(tumbler_1_pos)
			if(canhear)
				to_chat(user, "<span class='hear'>You hear a [pick("clack", "scrape", "clank")] from [src].</span>")
			if(tumbler_1_pos == tumbler_2_pos - 37 || tumbler_1_pos == tumbler_2_pos + 35)
				tumbler_2_pos = increment(tumbler_2_pos)
				if(canhear)
					to_chat(user, "<span class='hear'>You hear a [pick("click", "chink", "clink")] from [src].</span>")
			check_unlocked(user, canhear)
		updateUsrDialog()
		return

	if(href_list["retrieve"])
		user << browse("", "window=safe") // Close the menu

		var/obj/item/P = locate(href_list["retrieve"]) in src
		if(open)
			if(P && in_range(src, user))
				user.put_in_hands(P)
				space -= P.w_class
				updateUsrDialog()


/obj/structure/safe/attackby(obj/item/I, mob/user, params)
	if(open)
		. = 1 //no afterattack
		if(I.w_class + space <= maxspace)
			space += I.w_class
			if(!user.transferItemToLoc(I, src))
				to_chat(user, "<span class='warning'>\The [I] is stuck to your hand, you cannot put it in the safe!</span>")
				return
			to_chat(user, "<span class='notice'>You put [I] in [src].</span>")
			updateUsrDialog()
			return
		else
			to_chat(user, "<span class='notice'>[I] won't fit in [src].</span>")
			return
	else if(istype(I, /obj/item/clothing/neck/stethoscope))
		to_chat(user, "<span class='warning'>Hold [I] in one of your hands while you manipulate the dial!</span>")
	else
		return ..()


/obj/structure/safe/handle_atom_del(atom/A)
	updateUsrDialog()

/obj/structure/safe/blob_act(obj/structure/blob/B)
	return

/obj/structure/safe/ex_act(severity, target)
	if(((severity == 2 && target == src) || severity == 1) && explosion_count < 3)
		explosion_count++
		switch(explosion_count)
			if(1)
				desc = initial(desc) + "\nIt looks a little banged up."
			if(2)
				desc = initial(desc) + "\nIt's pretty heavily damaged."
			if(3)
				desc = initial(desc) + "\nThe lock seems to be broken."


//FLOOR SAFES
/obj/structure/safe/floor
	name = "floor safe"
	icon_state = "floorsafe"
	density = FALSE
	level = 1	//underfloor
	layer = LOW_OBJ_LAYER


/obj/structure/safe/floor/Initialize(mapload)
	. = ..()
	if(mapload)
		var/turf/T = loc
		hide(T.intact)


/obj/structure/safe/floor/hide(var/intact)
	invisibility = intact ? INVISIBILITY_MAXIMUM : 0
