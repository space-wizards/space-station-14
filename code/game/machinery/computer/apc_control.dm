/obj/machinery/computer/apc_control
	name = "power flow control console"
	desc = "Used to remotely control the flow of power to different parts of the station."
	icon_screen = "solar"
	icon_keyboard = "power_key"
	req_access = list(ACCESS_ENGINE)
	circuit = /obj/item/circuitboard/computer/apc_control
	light_color = LIGHT_COLOR_YELLOW
	var/mob/living/operator //Who's operating the computer right now
	var/obj/machinery/power/apc/active_apc //The APC we're using right now
	var/list/result_filters //For sorting the results
	var/checking_logs = 0
	var/list/logs
	var/auth_id = "\[NULL\]"

/obj/machinery/computer/apc_control/Initialize()
	. = ..()
	result_filters = list("Name" = null, "Charge Above" = null, "Charge Below" = null, "Responsive" = null)

/obj/machinery/computer/apc_control/process()
	if(operator && (!operator.Adjacent(src) || stat))
		operator = null
		if(active_apc)
			if(!active_apc.locked)
				active_apc.say("Remote access canceled. Interface locked.")
				playsound(active_apc, 'sound/machines/boltsdown.ogg', 25, FALSE)
				playsound(active_apc, 'sound/machines/terminal_alert.ogg', 50, FALSE)
			active_apc.locked = TRUE
			active_apc.update_icon()
			active_apc.remote_control = null
			active_apc = null

/obj/machinery/computer/apc_control/attack_ai(mob/user)
	if(!IsAdminGhost(user))
		to_chat(user,"<span class='warning'>[src] does not support AI control.</span>") //You already have APC access, cheater!
		return
	..(user)

/obj/machinery/computer/apc_control/proc/check_apc(obj/machinery/power/apc/APC)
	return APC.z == z && !APC.malfhack && !APC.aidisabled && !(APC.obj_flags & EMAGGED) && !APC.stat && !istype(APC.area, /area/ai_monitored) && !APC.area.outdoors

/obj/machinery/computer/apc_control/ui_interact(mob/living/user)
	. = ..()
	var/dat
	if(authenticated)
		if(!checking_logs)
			dat += "Logged in as [auth_id].<br><br>"
			dat += "<i>Filters</i><br>"
			dat += "<b>Name:</b> <a href='?src=[REF(src)];name_filter=1'>[result_filters["Name"] ? result_filters["Name"] : "None set"]</a><br>"
			dat += "<b>Charge:</b> <a href='?src=[REF(src)];above_filter=1'>\>[result_filters["Charge Above"] ? result_filters["Charge Above"] : "NaN"]%</a> and <a href='?src=[REF(src)];below_filter=1'>\<[result_filters["Charge Below"] ? result_filters["Charge Below"] : "NaN"]%</a><br>"
			dat += "<b>Accessible:</b> <a href='?src=[REF(src)];access_filter=1'>[result_filters["Responsive"] ? "Non-Responsive Only" : "All"]</a><br><br>"
			for(var/A in GLOB.apcs_list)
				if(check_apc(A))
					var/obj/machinery/power/apc/APC = A
					if(result_filters["Name"] && !findtext(APC.name, result_filters["Name"]) && !findtext(APC.area.name, result_filters["Name"]))
						continue
					if(result_filters["Charge Above"] && (!APC.cell || (APC.cell && (APC.cell.charge / APC.cell.maxcharge) < result_filters["Charge Above"] / 100)))
						continue
					if(result_filters["Charge Below"] && APC.cell && (APC.cell.charge / APC.cell.maxcharge) > result_filters["Charge Below"] / 100)
						continue
					if(result_filters["Responsive"] && !APC.aidisabled)
						continue
					dat += "<a href='?src=[REF(src)];access_apc=[REF(APC)]'>[A]</a><br>\
					<b>Charge:</b> [APC.cell ? "[DisplayEnergy(APC.cell.charge)] / [DisplayEnergy(APC.cell.maxcharge)] ([round((APC.cell.charge / APC.cell.maxcharge) * 100)]%)" : "No Powercell Installed"]<br>\
					<b>Area:</b> [APC.area]<br>\
					[APC.aidisabled || APC.panel_open ? "<font color='#FF0000'>APC does not respond to interface query.</font>" : "<font color='#00FF00'>APC responds to interface query.</font>"]<br><br>"
			dat += "<a href='?src=[REF(src)];check_logs=1'>Check Logs</a><br>"
			dat += "<a href='?src=[REF(src)];log_out=1'>Log Out</a><br>"
			if(obj_flags & EMAGGED)
				dat += "<font color='#FF0000'>WARNING: Logging functionality partially disabled from outside source.</font><br>"
				dat += "<a href='?src=[REF(src)];restore_logging=1'>Restore logging functionality?</a><br>"
		else
			if(logs.len)
				for(var/entry in logs)
					dat += "[entry]<br>"
			else
				dat += "<i>No activity has been recorded at this time.</i><br>"
			if(obj_flags & EMAGGED)
				dat += "<a href='?src=[REF(src)];clear_logs=1'><font color='#FF0000'>@#%! CLEAR LOGS</a>"
			dat += "<a href='?src=[REF(src)];check_apcs=1'>Return</a>"
		operator = user
	else
		dat = "<a href='?src=[REF(src)];authenticate=1'>Please swipe a valid ID to log in...</a>"
	var/datum/browser/popup = new(user, "apc_control", name, 600, 400)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()

/obj/machinery/computer/apc_control/Topic(href, href_list)
	if(..())
		return
	if(!usr || !usr.canUseTopic(src, !issilicon(usr)) || stat || QDELETED(src))
		return
	if(href_list["authenticate"])
		var/obj/item/card/id/ID = usr.get_idcard(TRUE)
		if(ID && istype(ID))
			if(check_access(ID))
				authenticated = TRUE
				auth_id = "[ID.registered_name] ([ID.assignment])"
				log_activity("logged in")
				playsound(src, 'sound/machines/terminal_on.ogg', 50, FALSE)
	if(href_list["log_out"])
		log_activity("logged out")
		playsound(src, 'sound/machines/terminal_off.ogg', 50, FALSE)
		authenticated = FALSE
		auth_id = "\[NULL\]"
	if(href_list["restore_logging"])
		to_chat(usr, "<span class='robot notice'>[icon2html(src, usr)] Logging functionality restored from backup data.</span>")
		obj_flags &= ~EMAGGED
		LAZYADD(logs, "<b>-=- Logging restored to full functionality at this point -=-</b>")
	if(href_list["access_apc"])
		playsound(src, "terminal_type", 50, FALSE)
		var/obj/machinery/power/apc/APC = locate(href_list["access_apc"]) in GLOB.apcs_list
		if(!APC || APC.aidisabled || APC.panel_open || QDELETED(APC))
			to_chat(usr, "<span class='robot danger'>[icon2html(src, usr)] APC does not return interface request. Remote access may be disabled.</span>")
			return
		if(active_apc)
			to_chat(usr, "<span class='robot danger'>[icon2html(src, usr)] Disconnected from [active_apc].</span>")
			active_apc.say("Remote access canceled. Interface locked.")
			playsound(active_apc, 'sound/machines/boltsdown.ogg', 25, FALSE)
			playsound(active_apc, 'sound/machines/terminal_alert.ogg', 50, FALSE)
			active_apc.locked = TRUE
			active_apc.update_icon()
			active_apc.remote_control = null
			active_apc = null
		to_chat(usr, "<span class='robot notice'>[icon2html(src, usr)] Connected to APC in [get_area_name(APC.area, TRUE)]. Interface request sent.</span>")
		log_activity("remotely accessed APC in [get_area_name(APC.area, TRUE)]")
		APC.remote_control = src
		APC.ui_interact(usr)
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
		message_admins("[ADMIN_LOOKUPFLW(usr)] remotely accessed [APC] from [src] at [AREACOORD(src)].")
		log_game("[key_name(usr)] remotely accessed [APC] from [src] at [AREACOORD(src)].")
		if(APC.locked)
			APC.say("Remote access detected. Interface unlocked.")
			playsound(APC, 'sound/machines/boltsup.ogg', 25, FALSE)
			playsound(APC, 'sound/machines/terminal_alert.ogg', 50, FALSE)
		APC.locked = FALSE
		APC.update_icon()
		active_apc = APC
	if(href_list["name_filter"])
		playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
		var/new_filter = stripped_input(usr, "What name are you looking for?", name)
		if(!src || !usr || !usr.canUseTopic(src, !issilicon(usr)) || stat || QDELETED(src))
			return
		log_activity("changed name filter to \"[new_filter]\"")
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
		result_filters["Name"] = new_filter
	if(href_list["above_filter"])
		playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
		var/new_filter = input(usr, "Enter a percentage from 1-100 to sort by (greater than).", name) as null|num
		if(!src || !usr || !usr.canUseTopic(src, !issilicon(usr)) || stat || QDELETED(src))
			return
		log_activity("changed greater than charge filter to \"[new_filter]\"")
		if(new_filter)
			new_filter = CLAMP(new_filter, 0, 100)
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
		result_filters["Charge Above"] = new_filter
	if(href_list["below_filter"])
		playsound(src, 'sound/machines/terminal_prompt.ogg', 50, FALSE)
		var/new_filter = input(usr, "Enter a percentage from 1-100 to sort by (lesser than).", name) as null|num
		if(!src || !usr || !usr.canUseTopic(src, !issilicon(usr)) || stat || QDELETED(src))
			return
		log_activity("changed lesser than charge filter to \"[new_filter]\"")
		if(new_filter)
			new_filter = CLAMP(new_filter, 0, 100)
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
		result_filters["Charge Below"] = new_filter
	if(href_list["access_filter"])
		if(isnull(result_filters["Responsive"]))
			result_filters["Responsive"] = 1
			log_activity("sorted by non-responsive APCs only")
		else
			result_filters["Responsive"] = !result_filters["Responsive"]
			log_activity("sorted by all APCs")
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 50, FALSE)
	if(href_list["check_logs"])
		checking_logs = TRUE
		log_activity("checked logs")
	if(href_list["check_apcs"])
		checking_logs = FALSE
		log_activity("checked APCs")
	if(href_list["clear_logs"])
		logs = list()
	ui_interact(usr) //Refresh the UI after a filter changes

/obj/machinery/computer/apc_control/emag_act(mob/user)
	if(!authenticated)
		to_chat(user, "<span class='warning'>You bypass [src]'s access requirements using your emag.</span>")
		authenticated = TRUE
		log_activity("logged in")
	else if(!(obj_flags & EMAGGED))
		if(user)
			user.visible_message("<span class='warning'>You emag [src], disabling precise logging and allowing you to clear logs.</span>")
		log_game("[key_name(user)] emagged [src] at [AREACOORD(src)], disabling operator tracking.")
		obj_flags |= EMAGGED
	playsound(src, "sparks", 50, TRUE)

/obj/machinery/computer/apc_control/proc/log_activity(log_text)
	var/op_string = operator && !(obj_flags & EMAGGED) ? operator : "\[NULL OPERATOR\]"
	LAZYADD(logs, "<b>([station_time_timestamp()])</b> [op_string] [log_text]")

/mob/proc/using_power_flow_console()
	for(var/obj/machinery/computer/apc_control/A in range(1, src))
		if(A.operator && A.operator == src && !A.stat)
			return TRUE
	return
