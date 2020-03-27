/obj/machinery/computer/robotics
	name = "robotics control console"
	desc = "Used to remotely lockdown or detonate linked Cyborgs and Drones."
	icon_screen = "robot"
	icon_keyboard = "rd_key"
	req_access = list(ACCESS_ROBOTICS)
	circuit = /obj/item/circuitboard/computer/robotics
	light_color = LIGHT_COLOR_PINK
	var/temp = null


/obj/machinery/computer/robotics/proc/can_control(mob/user, mob/living/silicon/robot/R)
	. = FALSE
	if(!istype(R))
		return
	if(isAI(user))
		if (R.connected_ai != user)
			return
	if(iscyborg(user))
		if (R != user)
			return
	if(R.scrambledcodes)
		return
	return TRUE

/obj/machinery/computer/robotics/ui_interact(mob/user)
	. = ..()
	user.set_machine(src)
	var/dat
	var/list/robo_list = list()
	var/robot_count
	for(var/mob/living/silicon/robot/R in GLOB.silicon_mobs)
		if(!can_control(user, R))
			continue
		if(z != (get_turf(R)).z)
			continue
		robot_count++
		var/unit_sync = "Independent"
		if(R.connected_ai)
			unit_sync = "Slaved to [R.connected_ai]"
		if(!robo_list[unit_sync])
			robo_list[unit_sync] = list()
		robo_list[unit_sync] += R

	dat += "<center><h2>Cyborgs</h2><hr></center>"
	if(!robo_list.len)
		dat += "<center>No cyborg units detected within access parameters.</center><br><br>"
	else
		if(robo_list.len > 1)
			sortTim(robo_list, /proc/cmp_text_asc)
		for(var/ai_unit in robo_list)
			dat += "<center><h3>[ai_unit]</h3></center><div class='statusDisplay'>"
			var/spacer
			for(var/robo in robo_list[ai_unit])
				if(spacer)
					dat += "<br><br>"
				else
					spacer = TRUE
				var/mob/living/silicon/robot/R = robo
				dat += "<b>Name:</b> [R.name]<br>"
				var/can_move = (R.mobility_flags & MOBILITY_MOVE)
				dat += "<b>Status:</b> [R.stat ? "Not Responding" : (can_move ? "Normal" : "Locked Down")]<br>"

				if(can_move)
					dat += "<b>Cell:</b> [R.cell ? "[R.cell.percent()]%" :  "No Cell Detected"]<br>"

				dat += "<b>Module:</b> [R.module ? "[R.module.name] Module" : "No Module Detected"]<br>"
				dat += "<b>Unit Controls:</b> "
				if(issilicon(user) && user != R)
					var/mob/living/silicon/S = user
					if(S.hack_software && !R.emagged)
						dat += "<A href='?src=[REF(src)];magbot=[REF(R)]'>(<font color=blue><i>Hack</i></font>)</A> "
				else if(IsAdminGhost(user) && !R.emagged)
					dat += "<A href='?src=[REF(src)];magbot=[REF(R)]'>(<font color=blue><i>Hack</i></font>)</A> "
				dat += "<A href='?src=[REF(src)];stopbot=[REF(R)]'>(<font color=green><i>[(R.mobility_flags & MOBILITY_MOVE) ? "Lockdown" : "Release"]</i></font>)</A> "
				dat += "<A href='?src=[REF(src)];killbot=[REF(R)]'>(<font color=red><i>Destroy</i></font>)</A>"
			dat += "</div>"

	dat += "<center><h2>Drones</h2></center>"
	var/drones = 0
	for(var/mob/living/simple_animal/drone/D in GLOB.drones_list)
		if(D.hacked)
			continue
		if(z != (get_turf(D)).z)
			continue
		if(drones)
			dat += "<br><br>"
		else
			dat += "<div class='statusDisplay'>"
		drones++
		dat += "<b>Name:</b> [D.name]<br>"
		dat += "<b>Status:</b> [D.stat ? "Not Responding" : "Normal"]<br>"
		dat += "<b>Unit Controls:</b> "
		dat += "<A href='?src=[REF(src)];killdrone=[REF(D)]'>(<font color=red><i>Destroy</i></font>)</A>"

	if(drones)
		dat += "</div>"
	else
		dat += "<hr><center>No drone units detected within access parameters.</center>"

	var/window_height = min((300+((robot_count+drones) * 110)), 800)

	var/datum/browser/popup = new(user, "computer", "Robotics Control Console", 375, window_height)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()

/obj/machinery/computer/robotics/Topic(href, href_list)
	. = ..()
	if(.)
		return

	if (href_list["temp"])
		temp = null

	else if (href_list["killbot"])
		if(allowed(usr))
			var/mob/living/silicon/robot/R = locate(href_list["killbot"]) in GLOB.silicon_mobs
			if(can_control(usr, R))
				var/choice = input("Are you certain you wish to detonate [R.name]?") in list("Confirm", "Abort")
				if(choice == "Confirm" && can_control(usr, R) && !..())
					var/turf/T = get_turf(R)
					message_admins("<span class='notice'>[ADMIN_LOOKUPFLW(usr)] detonated [key_name_admin(R, R.client)] at [ADMIN_VERBOSEJMP(T)]!</span>")
					log_game("\<span class='notice'>[key_name(usr)] detonated [key_name(R)]!</span>")
					if(R.connected_ai)
						to_chat(R.connected_ai, "<br><br><span class='alert'>ALERT - Cyborg detonation detected: [R.name]</span><br>")
					R.self_destruct()
		else
			to_chat(usr, "<span class='danger'>Access Denied.</span>")

	else if (href_list["stopbot"])
		if(allowed(usr))
			var/mob/living/silicon/robot/R = locate(href_list["stopbot"]) in GLOB.silicon_mobs
			if(can_control(usr, R))
				var/choice = input("Are you certain you wish to [!R.lockcharge ? "lock down" : "release"] [R.name]?") in list("Confirm", "Abort")
				if(choice == "Confirm" && can_control(usr, R) && !..())
					message_admins("<span class='notice'>[ADMIN_LOOKUPFLW(usr)] [!R.lockcharge ? "locked down" : "released"] [ADMIN_LOOKUPFLW(R)]!</span>")
					log_game("[key_name(usr)] [!R.lockcharge ? "locked down" : "released"] [key_name(R)]!")
					R.SetLockdown(!R.lockcharge)
					to_chat(R, "[!R.lockcharge ? "<span class='notice'>Your lockdown has been lifted!" : "<span class='alert'>You have been locked down!"]</span>")
					if(R.connected_ai)
						to_chat(R.connected_ai, "[!R.lockcharge ? "<span class='notice'>NOTICE - Cyborg lockdown lifted" : "<span class='alert'>ALERT - Cyborg lockdown detected"]: <a href='?src=[REF(R.connected_ai)];track=[html_encode(R.name)]'>[R.name]</a></span><br>")

		else
			to_chat(usr, "<span class='danger'>Access Denied.</span>")

	else if (href_list["magbot"])
		var/mob/living/silicon/S = usr
		if((istype(S) && S.hack_software) || IsAdminGhost(usr))
			var/mob/living/silicon/robot/R = locate(href_list["magbot"]) in GLOB.silicon_mobs
			if(istype(R) && !R.emagged && (R.connected_ai == usr || IsAdminGhost(usr)) && !R.scrambledcodes && can_control(usr, R))
				log_game("[key_name(usr)] emagged [key_name(R)] using robotic console!")
				message_admins("[ADMIN_LOOKUPFLW(usr)] emagged cyborg [key_name_admin(R)] using robotic console!")
				R.SetEmagged(TRUE)

	else if (href_list["killdrone"])
		if(allowed(usr))
			var/mob/living/simple_animal/drone/D = locate(href_list["killdrone"]) in GLOB.mob_list
			if(D.hacked)
				to_chat(usr, "<span class='danger'>ERROR: [D] is not responding to external commands.</span>")
			else
				var/turf/T = get_turf(D)
				message_admins("[ADMIN_LOOKUPFLW(usr)] detonated [key_name_admin(D)] at [ADMIN_VERBOSEJMP(T)]!")
				log_game("[key_name(usr)] detonated [key_name(D)]!")
				var/datum/effect_system/spark_spread/s = new /datum/effect_system/spark_spread
				s.set_up(3, TRUE, D)
				s.start()
				D.visible_message("<span class='danger'>\the [D] self destructs!</span>")
				D.gib()


	updateUsrDialog()
