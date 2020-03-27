/obj/item/computer_hardware/battery
	name = "power cell controller"
	desc = "A charge controller for standard power cells, used in all kinds of modular computers."
	icon_state = "cell_con"
	critical = 1
	malfunction_probability = 1
	var/obj/item/stock_parts/cell/battery = null
	device_type = MC_CELL

/obj/item/computer_hardware/battery/New(loc, battery_type = null)
	if(battery_type)
		battery = new battery_type(src)
	..()

/obj/item/computer_hardware/battery/Destroy()
	. = ..()
	QDEL_NULL(battery)

/obj/item/computer_hardware/battery/handle_atom_del(atom/A)
	if(A == battery)
		try_eject(0, null, TRUE)
	. = ..()

/obj/item/computer_hardware/battery/try_insert(obj/item/I, mob/living/user = null)
	if(!holder)
		return FALSE

	if(!istype(I, /obj/item/stock_parts/cell))
		return FALSE

	if(battery)
		to_chat(user, "<span class='warning'>You try to connect \the [I] to \the [src], but its connectors are occupied.</span>")
		return FALSE

	if(I.w_class > holder.max_hardware_size)
		to_chat(user, "<span class='warning'>This power cell is too large for \the [holder]!</span>")
		return FALSE

	if(user && !user.transferItemToLoc(I, src))
		return FALSE

	battery = I
	to_chat(user, "<span class='notice'>You connect \the [I] to \the [src].</span>")

	return TRUE


/obj/item/computer_hardware/battery/try_eject(slot=0, mob/living/user = null, forced = 0)
	if(!battery)
		to_chat(user, "<span class='warning'>There is no power cell connected to \the [src].</span>")
		return FALSE
	else
		if(user)
			user.put_in_hands(battery)
		else
			battery.forceMove(drop_location())
		to_chat(user, "<span class='notice'>You detach \the [battery] from \the [src].</span>")
		battery = null

		if(holder)
			if(holder.enabled && !holder.use_power())
				holder.shutdown_computer()

		return TRUE







/obj/item/stock_parts/cell/computer
	name = "standard battery"
	desc = "A standard power cell, commonly seen in high-end portable microcomputers or low-end laptops."
	icon = 'icons/obj/module.dmi'
	icon_state = "cell_mini"
	w_class = WEIGHT_CLASS_TINY
	maxcharge = 750


/obj/item/stock_parts/cell/computer/advanced
	name = "advanced battery"
	desc = "An advanced power cell, often used in most laptops. It is too large to be fitted into smaller devices."
	icon_state = "cell"
	w_class = WEIGHT_CLASS_SMALL
	maxcharge = 1500

/obj/item/stock_parts/cell/computer/super
	name = "super battery"
	desc = "An advanced power cell, often used in high-end laptops."
	icon_state = "cell"
	w_class = WEIGHT_CLASS_SMALL
	maxcharge = 2000

/obj/item/stock_parts/cell/computer/micro
	name = "micro battery"
	desc = "A small power cell, commonly seen in most portable microcomputers."
	icon_state = "cell_micro"
	maxcharge = 500

/obj/item/stock_parts/cell/computer/nano
	name = "nano battery"
	desc = "A tiny power cell, commonly seen in low-end portable microcomputers."
	icon_state = "cell_micro"
	maxcharge = 300
