/obj/item/assembly/timer
	name = "timer"
	desc = "Used to time things. Works well with contraptions which has to count down. Tick tock."
	icon_state = "timer"
	custom_materials = list(/datum/material/iron=500, /datum/material/glass=50)
	attachable = TRUE

	var/timing = FALSE
	var/time = 5
	var/saved_time = 5
	var/loop = FALSE
	var/hearing_range = 3
	drop_sound = 'sound/items/handling/component_drop.ogg'
	pickup_sound =  'sound/items/handling/component_pickup.ogg'

/obj/item/assembly/timer/suicide_act(mob/living/user)
	user.visible_message("<span class='suicide'>[user] looks at the timer and decides [user.p_their()] fate! It looks like [user.p_theyre()] going to commit suicide!</span>")
	activate()//doesnt rely on timer_end to prevent weird metas where one person can control the timer and therefore someone's life. (maybe that should be how it works...)
	addtimer(CALLBACK(src, .proc/manual_suicide, user), time*10)//kill yourself once the time runs out
	return MANUAL_SUICIDE

/obj/item/assembly/timer/proc/manual_suicide(mob/living/user)
	user.visible_message("<span class='suicide'>[user]'s time is up!</span>")
	user.adjustOxyLoss(200)
	user.death(0)

/obj/item/assembly/timer/Initialize()
	. = ..()
	START_PROCESSING(SSobj, src)

/obj/item/assembly/timer/Destroy()
	STOP_PROCESSING(SSobj, src)
	. = ..()

/obj/item/assembly/timer/examine(mob/user)
	. = ..()
	. += "<span class='notice'>The timer is [timing ? "counting down from [time]":"set for [time] seconds"].</span>"

/obj/item/assembly/timer/activate()
	if(!..())
		return FALSE//Cooldown check
	timing = !timing
	update_icon()
	return TRUE


/obj/item/assembly/timer/toggle_secure()
	secured = !secured
	if(secured)
		START_PROCESSING(SSobj, src)
	else
		timing = FALSE
		STOP_PROCESSING(SSobj, src)
	update_icon()
	return secured


/obj/item/assembly/timer/proc/timer_end()
	if(!secured || next_activate > world.time)
		return FALSE
	pulse(FALSE)
	audible_message("[icon2html(src, hearers(src))] *beep* *beep* *beep*", null, hearing_range)
	for(var/CHM in get_hearers_in_view(hearing_range, src))
		if(ismob(CHM))
			var/mob/LM = CHM
			LM.playsound_local(get_turf(src), 'sound/machines/triple_beep.ogg', ASSEMBLY_BEEP_VOLUME, TRUE)
	if(loop)
		timing = TRUE
	update_icon()


/obj/item/assembly/timer/process()
	if(!timing)
		return
	time--
	if(time <= 0)
		timing = FALSE
		timer_end()
		time = saved_time


/obj/item/assembly/timer/update_icon()
	cut_overlays()
	attached_overlays = list()
	if(timing)
		add_overlay("timer_timing")
		attached_overlays += "timer_timing"
	if(holder)
		holder.update_icon()


/obj/item/assembly/timer/ui_interact(mob/user)//TODO: Have this use the wires
	. = ..()
	if(is_secured(user))
		var/second = time % 60
		var/minute = (time - second) / 60
		var/dat = "<TT><B>Timing Unit</B></TT>"
		dat += "<BR>[(timing ? "<A href='?src=[REF(src)];time=0'>Timing</A>" : "<A href='?src=[REF(src)];time=1'>Not Timing</A>")] [minute]:[second]"
		dat += "<BR><A href='?src=[REF(src)];tp=-30'>-</A> <A href='?src=[REF(src)];tp=-1'>-</A> <A href='?src=[REF(src)];tp=1'>+</A> <A href='?src=[REF(src)];tp=30'>+</A>"
		dat += "<BR><BR><A href='?src=[REF(src)];repeat=[(loop ? "0'>Stop repeating" : "1'>Set to repeat")]</A>"
		dat += "<BR><BR><A href='?src=[REF(src)];refresh=1'>Refresh</A>"
		dat += "<BR><BR><A href='?src=[REF(src)];close=1'>Close</A>"
		var/datum/browser/popup = new(user, "timer", name)
		popup.set_content(dat)
		popup.open()


/obj/item/assembly/timer/Topic(href, href_list)
	..()
	if(!usr.canUseTopic(src, BE_CLOSE))
		usr << browse(null, "window=timer")
		onclose(usr, "timer")
		return

	if(href_list["time"])
		timing = text2num(href_list["time"])
		if(timing && istype(holder, /obj/item/transfer_valve))
			log_bomber(usr, "activated a", src, "attachment on [holder]")

		update_icon()
	if(href_list["repeat"])
		loop = text2num(href_list["repeat"])

	if(href_list["tp"])
		var/tp = text2num(href_list["tp"])
		time += tp
		time = min(max(round(time), 1), 600)
		saved_time = time

	if(href_list["close"])
		usr << browse(null, "window=timer")
		return

	if(usr)
		attack_self(usr)
