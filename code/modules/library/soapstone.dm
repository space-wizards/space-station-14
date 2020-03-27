/obj/item/soapstone
	name = "soapstone"
	desc = "Leave informative messages for the crew, including the crew of future shifts!\nEven if out of uses, it can still be used to remove messages.\n(Not suitable for engraving on shuttles, off station or on cats. Side effects may include prompt beatings, psychotic clown incursions, and/or orbital bombardment.)"
	icon = 'icons/obj/items_and_weapons.dmi'
	icon_state = "soapstone"
	throw_speed = 3
	throw_range = 5
	w_class = WEIGHT_CLASS_TINY
	var/tool_speed = 50
	var/remaining_uses = 3

/obj/item/soapstone/Initialize(mapload)
	. = ..()
	check_name()

/obj/item/soapstone/examine(mob/user)
	. = ..()
	if(remaining_uses != -1)
		. += "It has [remaining_uses] uses left."

/obj/item/soapstone/afterattack(atom/target, mob/user, proximity)
	. = ..()
	var/turf/T = get_turf(target)
	if(!proximity)
		return

	var/obj/structure/chisel_message/existing_message = locate() in T

	if(!remaining_uses && !existing_message)
		to_chat(user, "<span class='warning'>[src] is too worn out to use.</span>")
		return

	if(!good_chisel_message_location(T))
		to_chat(user, "<span class='warning'>It's not appropriate to engrave on [T].</span>")
		return

	if(existing_message)
		user.visible_message("<span class='notice'>[user] starts erasing [existing_message].</span>", "<span class='notice'>You start erasing [existing_message].</span>", "<span class='hear'>You hear a chipping sound.</span>")
		playsound(loc, 'sound/items/gavel.ogg', 50, TRUE, -1)
		if(do_after(user, tool_speed, target = existing_message))
			user.visible_message("<span class='notice'>[user] erases [existing_message].</span>", "<span class='notice'>You erase [existing_message][existing_message.creator_key == user.ckey ? ", refunding a use" : ""].</span>")
			existing_message.persists = FALSE
			qdel(existing_message)
			playsound(loc, 'sound/items/gavel.ogg', 50, TRUE, -1)
			if(existing_message.creator_key == user.ckey)
				refund_use()
		return

	var/message = stripped_input(user, "What would you like to engrave?", "Leave a message")
	if(!message)
		to_chat(user, "<span class='notice'>You decide not to engrave anything.</span>")
		return

	if(!target.Adjacent(user) && locate(/obj/structure/chisel_message) in T)
		to_chat(user, "<span class='warning'>Someone wrote here before you chose! Find another spot.</span>")
		return
	playsound(loc, 'sound/items/gavel.ogg', 50, TRUE, -1)
	user.visible_message("<span class='notice'>[user] starts engraving a message into [T]...</span>", "<span class='notice'>You start engraving a message into [T]...</span>", "<span class='hear'>You hear a chipping sound.</span>")
	if(can_use() && do_after(user, tool_speed, target = T) && can_use()) //This looks messy but it's actually really clever!
		if(!locate(/obj/structure/chisel_message) in T)
			user.visible_message("<span class='notice'>[user] leaves a message for future spacemen!</span>", "<span class='notice'>You engrave a message into [T]!</span>", "<span class='hear'>You hear a chipping sound.</span>")
			playsound(loc, 'sound/items/gavel.ogg', 50, TRUE, -1)
			var/obj/structure/chisel_message/M = new(T)
			M.register(user, message)
			remove_use()

/obj/item/soapstone/proc/can_use()
	return remaining_uses == -1 || remaining_uses >= 0

/obj/item/soapstone/proc/remove_use()
	if(remaining_uses <= 0)
		return
	remaining_uses--
	check_name()

/obj/item/soapstone/proc/refund_use()
	if(remaining_uses == -1)
		return
	remaining_uses++
	check_name()

/obj/item/soapstone/proc/check_name()
	if(remaining_uses)
		// This will mess up RPG loot names, but w/e
		name = initial(name)
	else
		name = "dull [initial(name)]"

/* Persistent engraved messages, etched onto the station turfs to serve
   as instructions and/or memes for the next generation of spessmen.

   Limited in location to station_z only. Can be smashed out or exploded,
   but only permamently removed with the curator's soapstone.
*/

/obj/item/soapstone/infinite
	remaining_uses = -1

/obj/item/soapstone/empty
	remaining_uses = 0

/proc/good_chisel_message_location(turf/T)
	if(!T)
		. = FALSE
	else if(!(isfloorturf(T) || iswallturf(T)))
		. = FALSE
	else
		. = TRUE

/obj/structure/chisel_message
	name = "engraved message"
	desc = "A message from a past traveler."
	icon = 'icons/obj/stationobjs.dmi'
	icon_state = "soapstone_message"
	layer = LATTICE_LAYER
	density = FALSE
	anchored = TRUE
	max_integrity = 30

	var/hidden_message
	var/creator_key
	var/creator_name
	var/realdate
	var/map
	var/persists = TRUE
	var/list/like_keys = list()
	var/list/dislike_keys = list()

	var/turf/original_turf

/obj/structure/chisel_message/Initialize(mapload)
	. = ..()
	SSpersistence.chisel_messages += src
	var/turf/T = get_turf(src)
	original_turf = T

	if(!good_chisel_message_location(T))
		persists = FALSE
		return INITIALIZE_HINT_QDEL

/obj/structure/chisel_message/proc/register(mob/user, newmessage)
	hidden_message = newmessage
	creator_name = user.real_name
	creator_key = user.ckey
	realdate = world.realtime
	map = SSmapping.config.map_name
	update_icon()

/obj/structure/chisel_message/update_icon()
	..()
	var/hash = md5(hidden_message)
	var/newcolor = copytext_char(hash, 1, 7)
	add_atom_colour("#[newcolor]", FIXED_COLOUR_PRIORITY)
	light_color = "#[newcolor]"
	set_light(1)

/obj/structure/chisel_message/proc/pack()
	var/list/data = list()
	data["hidden_message"] = hidden_message
	data["creator_name"] = creator_name
	data["creator_key"] = creator_key
	data["realdate"] = realdate
	data["map"] = SSmapping.config.map_name
	data["x"] = original_turf.x
	data["y"] = original_turf.y
	data["z"] = original_turf.z
	data["like_keys"] = like_keys
	data["dislike_keys"] = dislike_keys
	return data

/obj/structure/chisel_message/proc/unpack(list/data)
	if(!islist(data))
		return

	hidden_message = data["hidden_message"]
	creator_name = data["creator_name"]
	creator_key = data["creator_key"]
	realdate = data["realdate"]
	like_keys = data["like_keys"]
	if(!like_keys)
		like_keys = list()
	dislike_keys = data["dislike_keys"]
	if(!dislike_keys)
		dislike_keys = list()

	var/x = data["x"]
	var/y = data["y"]
	var/z = data["z"]
	var/turf/newloc = locate(x, y, z)
	if(isturf(newloc))
		forceMove(newloc)
	update_icon()

/obj/structure/chisel_message/examine(mob/user)
	. = ..()
	ui_interact(user)

/obj/structure/chisel_message/Destroy()
	if(persists)
		SSpersistence.SaveChiselMessage(src)
	SSpersistence.chisel_messages -= src
	. = ..()

/obj/structure/chisel_message/interact()
	return

/obj/structure/chisel_message/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.always_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "engraved_message", name, 600, 300, master_ui, state)
		ui.open()

/obj/structure/chisel_message/ui_data(mob/user)
	var/list/data = list()

	data["hidden_message"] = hidden_message
	data["realdate"] = SQLtime(realdate)
	data["num_likes"] = like_keys.len
	data["num_dislikes"] = dislike_keys.len
	data["is_creator"] = user.ckey == creator_key
	data["has_liked"] = (user.ckey in like_keys)
	data["has_disliked"] = (user.ckey in dislike_keys)

	if(check_rights_for(user.client, R_ADMIN))
		data["admin_mode"] = TRUE
		data["creator_key"] = creator_key
		data["creator_name"] = creator_name

	return data

/obj/structure/chisel_message/ui_act(action, params, datum/tgui/ui)
	var/mob/user = usr
	var/is_admin = check_rights_for(user.client, R_ADMIN)
	var/is_creator = user.ckey == creator_key
	var/has_liked = (user.ckey in like_keys)
	var/has_disliked = (user.ckey in dislike_keys)

	switch(action)
		if("like")
			if(is_creator)
				return
			if(has_disliked)
				dislike_keys -= user.ckey
			like_keys |= user.ckey
			. = TRUE
		if("dislike")
			if(is_creator)
				return
			if(has_liked)
				like_keys -= user.ckey
			dislike_keys |= user.ckey
			. = TRUE
		if("neutral")
			if(is_creator)
				return
			dislike_keys -= user.ckey
			like_keys -= user.ckey
			. = TRUE
		if("delete")
			if(!is_admin)
				return
			var/confirm = alert(user, "Confirm deletion of engraved message?", "Confirm Deletion", "Yes", "No")
			if(confirm == "Yes")
				persists = FALSE
				qdel(src)
