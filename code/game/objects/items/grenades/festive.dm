//~*~*~*~*SPARKLER*~*~*~*~*~*~

/obj/item/sparkler
	name = "sparkler"
	desc = "A little stick coated with metal power and barium nitrate, burns with a pleasing sparkle."
	icon = 'icons/obj/holiday_misc.dmi'
	icon_state = "sparkler"
	w_class = WEIGHT_CLASS_TINY
	heat = 1000
	var/burntime = 60
	var/lit = FALSE

/obj/item/sparkler/fire_act(exposed_temperature, exposed_volume)
	light()

/obj/item/sparkler/attackby(obj/item/W, mob/user, params)
	var/ignition_msg = W.ignition_effect(src, user)
	if(ignition_msg)
		light(user, ignition_msg)
	else
		return ..()

/obj/item/sparkler/proc/light(mob/user, message)
	if(lit)
		return
	if(user && message)
		user.visible_message(message)
	lit = TRUE
	icon_state = "sparkler_on"
	force = 6
	hitsound = 'sound/items/welder.ogg'
	name = "lit [initial(name)]"
	attack_verb = list("burnt")
	set_light(l_range = 2, l_power = 2)
	damtype = "fire"
	START_PROCESSING(SSobj, src)
	playsound(src, 'sound/effects/fuse.ogg', 20, TRUE)
	update_icon()

/obj/item/sparkler/process()
	burntime--
	if(burntime < 1)
		new /obj/item/stack/rods(drop_location())
		qdel(src)
	else
		open_flame(heat)

/obj/item/sparkler/Destroy()
	STOP_PROCESSING(SSobj, src)
	..()

/obj/item/sparkler/ignition_effect(atom/A, mob/user)
	. = "<span class='notice'>[user] gracefully lights [A] with [src].</span>"

/obj/item/sparkler/get_temperature()
	return lit * heat

//~*~*~*~*FIRECRACKER*~*~*~*~*~*~

/obj/item/grenade/firecracker
	name = "large firecracker"
	desc = "Outlawed in most of the sector. Doubles as an excellent finger remover."
	icon = 'icons/obj/holiday_misc.dmi'
	icon_state = "firecracker"
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	w_class = WEIGHT_CLASS_SMALL
	item_state = "flare"
	throw_speed = 3
	throw_range = 7
	det_time = 30

/obj/item/grenade/firecracker/attack_self(mob/user) // You need to light it manually.
	return

/obj/item/grenade/firecracker/attackby(obj/item/W, mob/user, params)
	var/ignition_msg = W.ignition_effect(src, user)
	if(ignition_msg && !active)
		visible_message(ignition_msg)
		preprime(user)
	else
		return ..()

/obj/item/grenade/firecracker/fire_act(exposed_temperature, exposed_volume)
	prime()

obj/item/grenade/firecracker/wirecutter_act(mob/living/user, obj/item/I)
	if(active)
		return
	if(det_time)
		det_time -= 10
		to_chat(user, "<span class='notice'>You shorten the fuse of [src] with [I].")
		playsound(src, 'sound/items/wirecutter.ogg', 20, TRUE)
		icon_state = initial(icon_state) + "_[det_time]"
		update_icon()
	else
		to_chat(user, "<span class='danger'>You've already removed all of the fuse!")

obj/item/grenade/firecracker/preprime(mob/user, delayoverride, msg = TRUE, volume = 80)
	var/turf/T = get_turf(src)
	log_grenade(user, T)
	if(user)
		add_fingerprint(user)
		if(msg)
			to_chat(user, "<span class='warning'>You prime [src]! [capitalize(DisplayTimeText(det_time))]!</span>")
	playsound(src, 'sound/effects/fuse.ogg', volume, TRUE)
	active = TRUE
	icon_state = initial(icon_state) + "_active"
	addtimer(CALLBACK(src, .proc/prime), isnull(delayoverride)? det_time : delayoverride)

/obj/item/grenade/firecracker/prime()
	update_mob()
	var/explosion_loc = get_turf(src)
	qdel(src)
	explosion(explosion_loc,-1,-1,2)


