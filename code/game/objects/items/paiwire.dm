/obj/item/pai_cable
	desc = "A flexible coated cable with a universal jack on one end."
	name = "data cable"
	icon = 'icons/obj/power.dmi'
	icon_state = "wire1"
	item_flags = NOBLUDGEON
	var/obj/machinery/machine

/obj/item/pai_cable/proc/plugin(obj/machinery/M, mob/living/user)
	if(!user.transferItemToLoc(src, M))
		return
	user.visible_message("<span class='notice'>[user] inserts [src] into a data port on [M].</span>", "<span class='notice'>You insert [src] into a data port on [M].</span>", "<span class='hear'>You hear the satisfying click of a wire jack fastening into place.</span>")
	machine = M
