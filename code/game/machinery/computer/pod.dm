/obj/machinery/computer/pod
	name = "mass driver launch control"
	desc = "A combined blastdoor and mass driver control unit."
	var/obj/machinery/mass_driver/connected = null
	var/title = "Mass Driver Controls"
	var/id = 1
	var/timing = 0
	var/time = 30
	var/range = 4


/obj/machinery/computer/pod/Initialize()
	. = ..()
	for(var/obj/machinery/mass_driver/M in range(range, src))
		if(M.id == id)
			connected = M


/obj/machinery/computer/pod/proc/alarm()
	if(stat & (NOPOWER|BROKEN))
		return

	if(!connected)
		say("Cannot locate mass driver connector. Cancelling firing sequence!")
		return

	for(var/obj/machinery/door/poddoor/M in range(range, src))
		if(M.id == id)
			M.open()

	sleep(20)
	for(var/obj/machinery/mass_driver/M in range(range, src))
		if(M.id == id)
			M.power = connected.power
			M.drive()

	sleep(50)
	for(var/obj/machinery/door/poddoor/M in range(range, src))
		if(M.id == id)
			M.close()

/obj/machinery/computer/pod/ui_interact(mob/user)
	. = ..()
	if(!allowed(user))
		to_chat(user, "<span class='warning'>Access denied.</span>")
		return

	var/dat = ""
	if(connected)
		var/d2
		if(timing)	//door controls do not need timers.
			d2 = "<A href='?src=[REF(src)];time=0'>Stop Time Launch</A>"
		else
			d2 = "<A href='?src=[REF(src)];time=1'>Initiate Time Launch</A>"
		dat += "<HR>\nTimer System: [d2]\nTime Left: [DisplayTimeText((time SECONDS))] <A href='?src=[REF(src)];tp=-30'>-</A> <A href='?src=[REF(src)];tp=-1'>-</A> <A href='?src=[REF(src)];tp=1'>+</A> <A href='?src=[REF(src)];tp=30'>+</A>"
		var/temp = ""
		var/list/L = list( 0.25, 0.5, 1, 2, 4, 8, 16 )
		for(var/t in L)
			if(t == connected.power)
				temp += "[t] "
			else
				temp += "<A href = '?src=[REF(src)];power=[t]'>[t]</A> "
		dat += "<HR>\nPower Level: [temp]<BR>\n<A href = '?src=[REF(src)];alarm=1'>Firing Sequence</A><BR>\n<A href = '?src=[REF(src)];drive=1'>Test Fire Driver</A><BR>\n<A href = '?src=[REF(src)];door=1'>Toggle Outer Door</A><BR>"
	else
		dat += "<BR>\n<A href = '?src=[REF(src)];door=1'>Toggle Outer Door</A><BR>"
	dat += "<BR><BR><A href='?src=[REF(user)];mach_close=computer'>Close</A>"
	add_fingerprint(usr)
	var/datum/browser/popup = new(user, "computer", title, 400, 500)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.open()

/obj/machinery/computer/pod/process()
	if(!..())
		return
	if(timing)
		if(time > 0)
			time = round(time) - 1
		else
			alarm()
			time = 0
			timing = 0
		updateDialog()


/obj/machinery/computer/pod/Topic(href, href_list)
	if(..())
		return
	if(usr.contents.Find(src) || (in_range(src, usr) && isturf(loc)) || issilicon(usr))
		usr.set_machine(src)
		if(href_list["power"])
			var/t = text2num(href_list["power"])
			t = min(max(0.25, t), 16)
			if(connected)
				connected.power = t
		if(href_list["alarm"])
			alarm()
		if(href_list["time"])
			timing = text2num(href_list["time"])
		if(href_list["tp"])
			var/tp = text2num(href_list["tp"])
			time += tp
			time = min(max(round(time), 0), 120)
		if(href_list["door"])
			for(var/obj/machinery/door/poddoor/M in range(range, src))
				if(M.id == id)
					if(M.density)
						M.open()
					else
						M.close()
		if(href_list["drive"])
			for(var/obj/machinery/mass_driver/M in range(range, src))
				if(M.id == id)
					M.power = connected.power
					M.drive()
		updateUsrDialog()

/obj/machinery/computer/pod/old
	name = "\improper DoorMex control console"
	title = "Door Controls"
	icon_state = "oldcomp"
	icon_screen = "library"
	icon_keyboard = null

/obj/machinery/computer/pod/old/syndicate
	name = "\improper ProComp Executive IIc"
	desc = "The Syndicate operate on a tight budget. Operates external airlocks."
	title = "External Airlock Controls"
	req_access = list(ACCESS_SYNDICATE)

/obj/machinery/computer/pod/old/swf
	name = "\improper Magix System IV"
	desc = "An arcane artifact that holds much magic. Running E-Knock 2.2: Sorcerer's Edition."
