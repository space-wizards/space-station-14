/obj/item/desynchronizer
	name = "desynchronizer"
	desc = "An experimental device that can temporarily desynchronize the user from spacetime, effectively making them disappear while it's active."
	icon = 'icons/obj/device.dmi'
	icon_state = "desynchronizer"
	item_state = "electronic"
	w_class = WEIGHT_CLASS_SMALL
	item_flags = NOBLUDGEON
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	custom_materials = list(/datum/material/iron=250, /datum/material/glass=500)
	var/max_duration = 3000
	var/duration = 300
	var/last_use = 0
	var/next_use = 0
	var/obj/effect/abstract/sync_holder/sync_holder
	var/resync_timer

/obj/item/desynchronizer/attack_self(mob/living/user)
	if(world.time < next_use)
		to_chat(user, "<span class='warning'>[src] is still recharging.</span>")
		return
	if(!sync_holder)
		desync(user)
	else
		resync()

/obj/item/desynchronizer/examine(mob/user)
	. = ..()
	if(world.time < next_use)
		. += "<span class='warning'>Time left to recharge: [DisplayTimeText(next_use - world.time)]</span>"
	. += "<span class='notice'>Alt-click to customize the duration. Current duration: [DisplayTimeText(duration)].</span>"
	. += "<span class='notice'>Can be used again to interrupt the effect early. The recharge time is the same as the time spent in desync.</span>"

/obj/item/desynchronizer/AltClick(mob/living/user)
	if(!istype(user) || !user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		return
	var/new_duration = input(user, "Set the duration (5-300):", "Desynchronizer", duration / 10) as null|num
	if(new_duration)
		new_duration = new_duration SECONDS
		new_duration = CLAMP(new_duration, 50, max_duration)
		duration = new_duration
		to_chat(user, "<span class='notice'>You set the duration to [DisplayTimeText(duration)].</span>")

/obj/item/desynchronizer/proc/desync(mob/living/user)
	if(sync_holder)
		return
	sync_holder = new(drop_location())
	new /obj/effect/temp_visual/desynchronizer(drop_location())
	to_chat(user, "<span class='notice'>You activate [src], desynchronizing yourself from the present. You can still see your surroundings, but you feel eerily dissociated from reality.</span>")
	user.forceMove(sync_holder)
	SEND_SIGNAL(user, COMSIG_MOVABLE_SECLUDED_LOCATION)
	for(var/thing in user)
		var/atom/movable/AM = thing
		SEND_SIGNAL(AM, COMSIG_MOVABLE_SECLUDED_LOCATION)
	last_use = world.time
	icon_state = "desynchronizer-on"
	resync_timer = addtimer(CALLBACK(src, .proc/resync), duration , TIMER_STOPPABLE)

/obj/item/desynchronizer/proc/resync()
	new /obj/effect/temp_visual/desynchronizer(sync_holder.drop_location())
	QDEL_NULL(sync_holder)
	if(resync_timer)
		deltimer(resync_timer)
		resync_timer = null
	icon_state = initial(icon_state)
	next_use = world.time + (world.time - last_use) // Could be 2*world.time-last_use but that would just be confusing

/obj/item/desynchronizer/Destroy()
	if(sync_holder)
		resync()
	return ..()

/obj/effect/abstract/sync_holder
	name = "desyncronized pocket"
	desc = "A pocket in spacetime, keeping the user a fraction of a second in the future."
	icon = null
	icon_state = null
	alpha = 0
	invisibility = INVISIBILITY_ABSTRACT
	mouse_opacity = MOUSE_OPACITY_TRANSPARENT
	anchored = TRUE
	resistance_flags = INDESTRUCTIBLE

/obj/effect/abstract/sync_holder/Destroy()
	for(var/I in contents)
		var/atom/movable/AM = I
		AM.forceMove(drop_location())
	return ..()

/obj/effect/abstract/sync_holder/AllowDrop()
	return TRUE //no dropping spaghetti out of your spacetime pocket
