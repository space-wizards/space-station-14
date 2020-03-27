/obj/machinery/computer/mecha
	name = "exosuit control console"
	desc = "Used to remotely locate or lockdown exosuits."
	icon_screen = "mecha"
	icon_keyboard = "tech_key"
	req_access = list(ACCESS_ROBOTICS)
	circuit = /obj/item/circuitboard/computer/mecha_control
	var/list/located = list()

/obj/machinery/computer/mecha/ui_interact(mob/user)
	. = ..()
	var/dat = {"<html><head><title>[src.name]</title><style>h3 {margin: 0px; padding: 0px;}</style></head><body><br>
				<h3>Tracking beacons data</h3>"}
	var/list/trackerlist = list()
	for(var/obj/mecha/MC in GLOB.mechas_list)
		trackerlist += MC.trackers
	for(var/obj/item/mecha_parts/mecha_tracking/TR in trackerlist)
		var/answer = TR.get_mecha_info()
		if(answer)
			dat += {"<hr>[answer]<br/><br>
						<a href='?src=[REF(src)];send_message=[REF(TR)]'>Send Message</a> | [TR.recharging?"Recharging EMP Pulse...<br>":"<a style='color: #f00;' href='?src=[REF(src)];shock=[REF(TR)]'>(EMP Pulse)</a><br>"]"}

	dat += "<hr>"
	dat += "<A href='?src=[REF(src)];refresh=1'>(Refresh)</A><BR>"
	dat += "</body></html>"

	user << browse(dat, "window=computer;size=400x500")
	onclose(user, "computer")

/obj/machinery/computer/mecha/Topic(href, href_list)
	if(..())
		return
	if(href_list["send_message"])
		var/obj/item/mecha_parts/mecha_tracking/MT = locate(href_list["send_message"])
		if (!istype(MT))
			return
		var/message = stripped_input(usr,"Input message","Transmit message")
		var/obj/mecha/M = MT.in_mecha()
		if(trim(message) && M)
			M.occupant_message(message)
		return
	if(href_list["shock"])
		var/obj/item/mecha_parts/mecha_tracking/MT = locate(href_list["shock"])
		if (istype(MT) && MT.chassis)
			MT.shock()
			log_game("[key_name(usr)] has activated remote EMP on exosuit [MT.chassis], located at [loc_name(MT.chassis)], which is currently [MT.chassis.occupant? "being piloted by [key_name(MT.chassis.occupant)]." : "without a pilot."] ")
			message_admins("[key_name_admin(usr)][ADMIN_FLW(usr)] has activated remote EMP on exosuit [MT.chassis][ADMIN_JMP(MT.chassis)], which is currently [MT.chassis.occupant? "being piloted by [key_name_admin(MT.chassis.occupant)][ADMIN_FLW(MT.chassis.occupant)]." : "without a pilot."] ")

	updateUsrDialog()
	return

/obj/item/mecha_parts/mecha_tracking
	name = "exosuit tracking beacon"
	desc = "Device used to transmit exosuit data."
	icon = 'icons/obj/device.dmi'
	icon_state = "motion2"
	w_class = WEIGHT_CLASS_SMALL
	var/ai_beacon = FALSE //If this beacon allows for AI control. Exists to avoid using istype() on checking.
	var/recharging = 0
	var/obj/mecha/chassis

/obj/item/mecha_parts/mecha_tracking/proc/get_mecha_info()
	if(!in_mecha())
		return 0
	var/obj/mecha/M = src.loc
	var/cell_charge = M.get_charge()
	var/answer = {"<b>Name:</b> [M.name]<br>
<b>Integrity:</b> [round((M.obj_integrity/M.max_integrity*100), 0.01)]%<br>
<b>Cell Charge:</b> [isnull(cell_charge)?"Not Found":"[M.cell.percent()]%"]<br>
<b>Airtank:</b> [M.internal_tank?"[round(M.return_pressure(), 0.01)]":"Not Equipped"] kPa<br>
<b>Pilot:</b> [M.occupant||"None"]<br>
<b>Location:</b> [get_area_name(M, TRUE)||"Unknown"]<br>
<b>Active Equipment:</b> [M.selected||"None"]"}
	if(istype(M, /obj/mecha/working/ripley))
		var/obj/mecha/working/ripley/RM = M
		answer += "<br><b>Used Cargo Space:</b> [round((RM.cargo.len/RM.cargo_capacity*100), 0.01)]%"

	return answer

/obj/item/mecha_parts/mecha_tracking/emp_act()
	. = ..()
	if(!(. & EMP_PROTECT_SELF))
		qdel(src)

/obj/item/mecha_parts/mecha_tracking/Destroy()
	if(ismecha(loc))
		var/obj/mecha/M = loc
		if(src in M.trackers)
			M.trackers -= src
	chassis = null
	return ..()

/obj/item/mecha_parts/mecha_tracking/try_attach_part(mob/user, obj/mecha/M)
	if(!..())
		return
	M.trackers += src
	M.diag_hud_set_mechtracking()
	chassis = M

/obj/item/mecha_parts/mecha_tracking/proc/in_mecha()
	if(ismecha(loc))
		return loc
	return 0

/obj/item/mecha_parts/mecha_tracking/proc/shock()
	if(recharging)
		return
	var/obj/mecha/M = in_mecha()
	if(M)
		M.emp_act(EMP_HEAVY)
		addtimer(CALLBACK(src, /obj/item/mecha_parts/mecha_tracking/proc/recharge), 5 SECONDS, TIMER_UNIQUE | TIMER_OVERRIDE)
		recharging = 1

/obj/item/mecha_parts/mecha_tracking/proc/recharge()
	recharging = 0

/obj/item/mecha_parts/mecha_tracking/ai_control
	name = "exosuit AI control beacon"
	desc = "A device used to transmit exosuit data. Also allows active AI units to take control of said exosuit."
	ai_beacon = TRUE


/obj/item/storage/box/mechabeacons
	name = "exosuit tracking beacons"

/obj/item/storage/box/mechabeacons/PopulateContents()
	..()
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
	new /obj/item/mecha_parts/mecha_tracking(src)
