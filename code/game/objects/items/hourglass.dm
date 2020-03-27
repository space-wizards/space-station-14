#define HOURGLASS_STATES 7 //Remember to update if you change the sprite

/obj/item/hourglass
	name = "hourglass"
	desc = "Nanotrasen patented gravity invariant hourglass. Guaranteed to flow perfectly under any conditions."
	var/obj/effect/countdown/hourglass/countdown
	var/time = 1 MINUTES
	var/finish_time //So countdown doesn't need to fiddle with timers
	var/timing_id	//if present we're timing
	var/hand_activated = TRUE
	icon = 'icons/obj/hourglass.dmi'
	icon_state = "hourglass_idle"

/obj/item/hourglass/Initialize(mapload)
	. = ..()
	countdown = new(src)

/obj/item/hourglass/attack_self(mob/user)
	. = ..()
	if(hand_activated)
		toggle(user)

/obj/item/hourglass/proc/toggle(mob/user)
	if(!timing_id)
		to_chat(user,"<span class='notice'>You flip the [src].</span>")
		start()
		flick("hourglass_flip",src)
	else
		to_chat(user,"<span class='notice'>You stop the [src].</span>") //Sand magically flows back because that's more convinient to use.
		stop()

/obj/item/hourglass/update_icon_state()
	if(timing_id)
		icon_state = "hourglass_active"
	else
		icon_state = "hourglass_idle"

/obj/item/hourglass/proc/start()
	finish_time = world.time + time
	timing_id = addtimer(CALLBACK(src, .proc/finish), time, TIMER_STOPPABLE)
	countdown.start()
	timing_animation()

/obj/item/hourglass/proc/timing_animation()
	var/step_time = time / HOURGLASS_STATES
	animate(src, time = step_time, icon_state = "hourglass_1")
	for(var/i in 2 to HOURGLASS_STATES)
		animate(time = step_time, icon_state = "hourglass_[i]")

/obj/item/hourglass/proc/stop()
	if(timing_id)
		deltimer(timing_id)
		timing_id = null
	countdown.stop()
	finish_time = null
	animate(src)
	update_icon()

/obj/item/hourglass/proc/finish()
	visible_message("<span class='notice'>[src] stops.</span>")
	stop()

/obj/item/hourglass/Destroy()
	QDEL_NULL(countdown)
	. = ..()

//Admin events zone.
/obj/item/hourglass/admin
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF
	anchored = TRUE
	hand_activated = FALSE

/obj/item/hourglass/admin/attack_hand(mob/user)
	. = ..()
	if(user.client && user.client.holder)
		toggle(user)

/obj/item/hourglass/admin/attack_ghost(mob/user)
	if(user.client && user.client.holder)
		toggle(user)

#undef HOURGLASS_STATES
