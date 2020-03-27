//GUNCASES//
/obj/structure/guncase
	name = "gun locker"
	desc = "A locker that holds guns."
	icon = 'icons/obj/closet.dmi'
	icon_state = "shotguncase"
	anchored = FALSE
	density = TRUE
	opacity = 0
	var/case_type = ""
	var/gun_category = /obj/item/gun
	var/open = TRUE
	var/capacity = 4

/obj/structure/guncase/Initialize(mapload)
	. = ..()
	if(mapload)
		for(var/obj/item/I in loc.contents)
			if(istype(I, gun_category))
				I.forceMove(src)
			if(contents.len >= capacity)
				break
	update_icon()

/obj/structure/guncase/update_overlays()
	. = ..()
	if(case_type && LAZYLEN(contents))
		var/mutable_appearance/gun_overlay = mutable_appearance(icon, case_type)
		for(var/i in 1 to contents.len)
			gun_overlay.pixel_x = 3 * (i - 1)
			. += new /mutable_appearance(gun_overlay)
	if(open)
		. += "[icon_state]_open"
	else
		. += "[icon_state]_door"

/obj/structure/guncase/attackby(obj/item/I, mob/user, params)
	if(iscyborg(user) || isalien(user))
		return
	if(istype(I, gun_category) && open)
		if(LAZYLEN(contents) < capacity)
			if(!user.transferItemToLoc(I, src))
				return
			to_chat(user, "<span class='notice'>You place [I] in [src].</span>")
			update_icon()
		else
			to_chat(user, "<span class='warning'>[src] is full.</span>")
		return

	else if(user.a_intent != INTENT_HARM)
		open = !open
		update_icon()
	else
		return ..()

/obj/structure/guncase/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(iscyborg(user) || isalien(user))
		return
	if(contents.len && open)
		ShowWindow(user)
	else
		open = !open
		update_icon()

/obj/structure/guncase/proc/ShowWindow(mob/user)
	var/dat = {"<div class='block'>
				<h3>Stored Guns</h3>
				<table align='center'>"}
	if(LAZYLEN(contents))
		for(var/i in 1 to contents.len)
			var/obj/item/I = contents[i]
			dat += "<tr><A href='?src=[REF(src)];retrieve=[REF(I)]'>[I.name]</A><br>"
	dat += "</table></div>"

	var/datum/browser/popup = new(user, "gunlocker", "<div align='center'>[name]</div>", 350, 300)
	popup.set_content(dat)
	popup.open(FALSE)

/obj/structure/guncase/Topic(href, href_list)
	if(href_list["retrieve"])
		var/obj/item/O = locate(href_list["retrieve"]) in contents
		if(!O || !istype(O))
			return
		if(!usr.canUseTopic(src, BE_CLOSE) || !open)
			return
		if(ishuman(usr))
			if(!usr.put_in_hands(O))
				O.forceMove(get_turf(src))
			update_icon()

/obj/structure/guncase/handle_atom_del(atom/A)
	update_icon()

/obj/structure/guncase/contents_explosion(severity, target)
	for(var/atom/A in contents)
		A.ex_act(severity++, target)
		CHECK_TICK

/obj/structure/guncase/shotgun
	name = "shotgun locker"
	desc = "A locker that holds shotguns."
	case_type = "shotgun"
	gun_category = /obj/item/gun/ballistic/shotgun

/obj/structure/guncase/ecase
	name = "energy gun locker"
	desc = "A locker that holds energy guns."
	icon_state = "ecase"
	case_type = "egun"
	gun_category = /obj/item/gun/energy/e_gun
