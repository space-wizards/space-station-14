/obj/item/assembly/prox_sensor
	name = "proximity sensor"
	desc = "Used for scanning and alerting when someone enters a certain proximity."
	icon_state = "prox"
	custom_materials = list(/datum/material/iron=800, /datum/material/glass=200)
	attachable = TRUE

	var/scanning = FALSE
	var/timing = FALSE
	var/time = 10
	var/sensitivity = 1
	var/hearing_range = 3
	drop_sound = 'sound/items/handling/component_drop.ogg'
	pickup_sound =  'sound/items/handling/component_pickup.ogg'


/obj/item/assembly/prox_sensor/Initialize()
	. = ..()
	proximity_monitor = new(src, 0)
	START_PROCESSING(SSobj, src)

/obj/item/assembly/prox_sensor/Destroy()
	STOP_PROCESSING(SSobj, src)
	. = ..()

/obj/item/assembly/prox_sensor/examine(mob/user)
	. = ..()
	. += "<span class='notice'>The proximity sensor is [timing ? "arming" : (scanning ? "armed" : "disarmed")].</span>"

/obj/item/assembly/prox_sensor/activate()
	if(!..())
		return FALSE//Cooldown check
	if(!scanning)
		timing = !timing
	else
		scanning = FALSE
	update_icon()
	return TRUE

/obj/item/assembly/prox_sensor/on_detach()
	. = ..()
	if(!.)
		return
	else
		proximity_monitor.SetHost(src,src)


/obj/item/assembly/prox_sensor/toggle_secure()
	secured = !secured
	if(!secured)
		if(scanning)
			toggle_scan()
			proximity_monitor.SetHost(src,src)
		timing = FALSE
		STOP_PROCESSING(SSobj, src)
	else
		START_PROCESSING(SSobj, src)
		proximity_monitor.SetHost(loc,src)
	update_icon()
	return secured



/obj/item/assembly/prox_sensor/HasProximity(atom/movable/AM as mob|obj)
	if (istype(AM, /obj/effect/beam))
		return
	sense()

/obj/item/assembly/prox_sensor/proc/sense()
	if(!scanning || !secured || next_activate > world.time)
		return FALSE
	pulse(FALSE)
	audible_message("[icon2html(src, hearers(src))] *beep* *beep* *beep*", null, hearing_range)
	for(var/CHM in get_hearers_in_view(hearing_range, src))
		if(ismob(CHM))
			var/mob/LM = CHM
			LM.playsound_local(get_turf(src), 'sound/machines/triple_beep.ogg', ASSEMBLY_BEEP_VOLUME, TRUE)
	next_activate = world.time + 30
	return TRUE


/obj/item/assembly/prox_sensor/process()
	if(!timing)
		return
	time--
	if(time <= 0)
		timing = FALSE
		toggle_scan(TRUE)
		time = initial(time)

/obj/item/assembly/prox_sensor/proc/toggle_scan(scan)
	if(!secured)
		return FALSE
	scanning = scan
	proximity_monitor.SetRange(scanning ? sensitivity : 0)
	update_icon()

/obj/item/assembly/prox_sensor/proc/sensitivity_change(value)
	var/sense = min(max(sensitivity + value, 0), 5)
	sensitivity = sense
	if(scanning && proximity_monitor.SetRange(sense))
		sense()

/obj/item/assembly/prox_sensor/update_icon()
	cut_overlays()
	attached_overlays = list()
	if(timing)
		add_overlay("prox_timing")
		attached_overlays += "prox_timing"
	if(scanning)
		add_overlay("prox_scanning")
		attached_overlays += "prox_scanning"
	if(holder)
		holder.update_icon()
	return

/obj/item/assembly/prox_sensor/ui_interact(mob/user)//TODO: Change this to the wires thingy
	. = ..()
	if(is_secured(user))
		var/second = time % 60
		var/minute = (time - second) / 60
		var/dat = "<TT><B>Proximity Sensor</B></TT>"
		if(!scanning)
			dat += "<BR>[(timing ? "<A href='?src=[REF(src)];time=0'>Arming</A>" : "<A href='?src=[REF(src)];time=1'>Not Arming</A>")] [minute]:[second]"
			dat += "<BR><A href='?src=[REF(src)];tp=-30'>-</A> <A href='?src=[REF(src)];tp=-1'>-</A> <A href='?src=[REF(src)];tp=1'>+</A> <A href='?src=[REF(src)];tp=30'>+</A>"
		dat += "<BR><A href='?src=[REF(src)];scanning=[scanning?"0'>Armed":"1'>Unarmed (Movement sensor active when armed!)"]</A>"
		dat += "<BR>Detection range: <A href='?src=[REF(src)];sense=down'>-</A> [sensitivity] <A href='?src=[REF(src)];sense=up'>+</A>"
		dat += "<BR><BR><A href='?src=[REF(src)];refresh=1'>Refresh</A>"
		dat += "<BR><BR><A href='?src=[REF(src)];close=1'>Close</A>"
		user << browse(dat, "window=prox")
		onclose(user, "prox")
		return


/obj/item/assembly/prox_sensor/Topic(href, href_list)
	..()
	if(!usr.canUseTopic(src, BE_CLOSE))
		usr << browse(null, "window=prox")
		onclose(usr, "prox")
		return

	if(href_list["sense"])
		sensitivity_change(((href_list["sense"] == "up") ? 1 : -1))

	if(href_list["scanning"])
		toggle_scan(text2num(href_list["scanning"]))

	if(href_list["time"])
		timing = text2num(href_list["time"])
		update_icon()

	if(href_list["tp"])
		var/tp = text2num(href_list["tp"])
		time += tp
		time = min(max(round(time), 0), 600)

	if(href_list["close"])
		usr << browse(null, "window=prox")
		return

	if(usr)
		attack_self(usr)
