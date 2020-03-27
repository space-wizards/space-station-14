/obj/item/implantpad
	name = "implant pad"
	desc = "Used to modify implants."
	icon = 'icons/obj/items_and_weapons.dmi'
	icon_state = "implantpad-0"
	item_state = "electronic"
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	throw_speed = 3
	throw_range = 5
	w_class = WEIGHT_CLASS_SMALL
	var/obj/item/implantcase/case = null

/obj/item/implantpad/update_icon_state()
	icon_state = "implantpad-[!QDELETED(case)]"

/obj/item/implantpad/examine(mob/user)
	. = ..()
	if(Adjacent(user))
		. += "It [case ? "contains \a [case]" : "is currently empty"]."
		if(case)
			. += "<span class='info'>Alt-click to remove [case].</span>"
	else
		if(case)
			. += "<span class='warning'>There seems to be something inside it, but you can't quite tell what from here...</span>"

/obj/item/implantpad/handle_atom_del(atom/A)
	if(A == case)
		case = null
	update_icon()
	updateSelfDialog()
	. = ..()

/obj/item/implantpad/AltClick(mob/user)
	..()
	if(!user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		return
	if(!case)
		to_chat(user, "<span class='warning'>There's no implant to remove from [src].</span>")
		return

	user.put_in_hands(case)

	add_fingerprint(user)
	case.add_fingerprint(user)
	case = null

	updateSelfDialog()
	update_icon()

/obj/item/implantpad/attackby(obj/item/implantcase/C, mob/user, params)
	if(istype(C, /obj/item/implantcase) && !case)
		if(!user.transferItemToLoc(C, src))
			return
		case = C
		updateSelfDialog()
		update_icon()
	else
		return ..()

/obj/item/implantpad/ui_interact(mob/user)
	if(!user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		user.unset_machine(src)
		user << browse(null, "window=implantpad")
		return

	user.set_machine(src)
	var/dat = "<B>Implant Mini-Computer:</B><HR>"
	if(case)
		if(case.imp)
			if(istype(case.imp, /obj/item/implant))
				dat += case.imp.get_data()
		else
			dat += "The implant casing is empty."
	else
		dat += "Please insert an implant casing!"
	user << browse(dat, "window=implantpad")
	onclose(user, "implantpad")
