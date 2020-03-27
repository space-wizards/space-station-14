/obj/item/beacon
	name = "\improper tracking beacon"
	desc = "A beacon used by a teleporter."
	icon = 'icons/obj/device.dmi'
	icon_state = "beacon"
	item_state = "beacon"
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	var/enabled = TRUE
	var/renamed = FALSE

/obj/item/beacon/Initialize()
	. = ..()
	if (enabled)
		GLOB.teleportbeacons += src
	else 
		icon_state = "beacon-off"

/obj/item/beacon/Destroy()
	GLOB.teleportbeacons.Remove(src)
	return ..()

/obj/item/beacon/attack_self(mob/user)
	enabled = !enabled
	if (enabled)
		icon_state = "beacon"
		GLOB.teleportbeacons += src
	else 
		icon_state = "beacon-off"
		GLOB.teleportbeacons.Remove(src)
	to_chat(user, "<span class='notice'>You [enabled ? "enable" : "disable"] the beacon.</span>")
	return

/obj/item/beacon/attackby(obj/item/W, mob/user)
	if(istype(W, /obj/item/pen)) // needed for things that use custom names like the locator
		var/new_name = stripped_input(user, "What would you like the name to be?")
		if(!user.canUseTopic(src, BE_CLOSE))
			return
		if(new_name)
			name = new_name
			renamed = TRUE
		return
	else	
		return ..()
