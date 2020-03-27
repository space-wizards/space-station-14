
/obj/item/pressure_plate
	name = "pressure plate"
	desc = "An electronic device that triggers when stepped on."
	icon = 'icons/obj/device.dmi'
	item_state = "flash"
	icon_state = "pressureplate"
	level = 1
	var/trigger_mob = TRUE
	var/trigger_item = FALSE
	var/trigger_silent = FALSE
	var/sound/trigger_sound = 'sound/effects/pressureplate.ogg'
	var/obj/item/assembly/signaler/sigdev = null
	var/roundstart_signaller = FALSE
	var/roundstart_signaller_freq = FREQ_PRESSURE_PLATE
	var/roundstart_signaller_code = 30
	var/roundstart_hide = FALSE
	var/removable_signaller = TRUE
	var/active = FALSE
	var/image/tile_overlay = null
	var/can_trigger = TRUE
	var/trigger_delay = 10

/obj/item/pressure_plate/Initialize()
	. = ..()
	tile_overlay = image(icon = 'icons/turf/floors.dmi', icon_state = "pp_overlay")
	if(roundstart_signaller)
		sigdev = new
		sigdev.code = roundstart_signaller_code
		sigdev.frequency = roundstart_signaller_freq
		if(isopenturf(loc))
			hide(TRUE)

/obj/item/pressure_plate/Crossed(atom/movable/AM)
	. = ..()
	if(!can_trigger || !active)
		return
	if(trigger_mob && isliving(AM))
		var/mob/living/L = AM
		to_chat(L, "<span class='warning'>You feel something click beneath you!</span>")
	else if(!trigger_item)
		return
	can_trigger = FALSE
	addtimer(CALLBACK(src, .proc/trigger), trigger_delay)

/obj/item/pressure_plate/proc/trigger()
	can_trigger = TRUE
	if(istype(sigdev))
		sigdev.signal()

/obj/item/pressure_plate/attackby(obj/item/I, mob/living/L)
	if(istype(I, /obj/item/assembly/signaler) && !istype(sigdev) && removable_signaller && L.transferItemToLoc(I, src))
		sigdev = I
		to_chat(L, "<span class='notice'>You attach [I] to [src]!</span>")
	return ..()

/obj/item/pressure_plate/attack_self(mob/living/L)
	if(removable_signaller && istype(sigdev))
		to_chat(L, "<span class='notice'>You remove [sigdev] from [src].</span>")
		if(!L.put_in_hands(sigdev))
			sigdev.forceMove(get_turf(src))
		sigdev = null
	return ..()

/obj/item/pressure_plate/hide(yes)
	if(yes)
		invisibility = INVISIBILITY_MAXIMUM
		anchored = TRUE
		icon_state = null
		active = TRUE
		can_trigger = TRUE
		if(tile_overlay)
			loc.add_overlay(tile_overlay)
	else
		invisibility = initial(invisibility)
		anchored = FALSE
		icon_state = initial(icon_state)
		active = FALSE
		if(tile_overlay)
			loc.overlays -= tile_overlay
