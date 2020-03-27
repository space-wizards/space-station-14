/obj/machinery/computer/warrant//TODO:SANITY
	name = "security warrant console"
	desc = "Used to view crewmember security records"
	icon_screen = "security"
	icon_keyboard = "security_key"
	circuit = /obj/item/circuitboard/computer/warrant
	light_color = LIGHT_COLOR_RED
	var/screen = null
	var/datum/data/record/current = null

/obj/machinery/computer/warrant/ui_interact(mob/user)
	. = ..()

	var/list/dat = list("Logged in as: ")
	if(authenticated)
		dat += {"<a href='?src=[REF(src)];choice=Logout'>[authenticated]</a><hr>"}
		if(current)
			var/background
			var/notice = ""
			switch(current.fields["criminal"])
				if("*Arrest*")
					background = "background-color:#990000;"
					notice = "<br>**REPORT TO THE BRIG**"
				if("Incarcerated")
					background = "background-color:#CD6500;"
				if("Paroled")
					background = "background-color:#CD6500;"
				if("Discharged")
					background = "background-color:#006699;"
				if("None")
					background = "background-color:#4F7529;"
				if("")
					background = "''" //"'background-color:#FFFFFF;'"
			dat += "<font size='4'><b>Warrant Data</b></font>"
			dat += {"<table>
			<tr><td>Name:</td><td>&nbsp;[current.fields["name"]]&nbsp;</td></tr>
			<tr><td>ID:</td><td>&nbsp;[current.fields["id"]]&nbsp;</td></tr>
			</table>"}
			dat += {"Criminal Status:<br>
			<div style='[background] padding: 3px; text-align: center;'>
			<strong>[current.fields["criminal"]][notice]</strong>
			</div>"}

			dat += "<br><br>Citations:"

			dat +={"<table style="text-align:center;" border="1" cellspacing="0" width="100%">
			<tr>
			<th>Crime</th>
			<th>Fine</th>
			<th>Author</th>
			<th>Time Added</th>
			<th>Amount Due</th>
			<th>Make Payment</th>
			</tr>"}
			for(var/datum/data/crime/c in current.fields["citation"])
				var/owed = c.fine - c.paid
				dat += {"<tr><td>[c.crimeName]</td>
				<td>[c.fine] cr</td>
				<td>[c.author]</td>
				<td>[c.time]</td>"}
				if(owed > 0)
					dat += {"<td>[owed] cr</td>
					<td><A href='?src=[REF(src)];choice=Pay;field=citation_pay;cdataid=[c.dataId]'>\[Pay\]</A></td>"}
				else
					dat += "<td colspan='2'>All Paid Off</td>"
				dat += "</tr>"
			dat += "</table>"

			dat += "<br>Minor Crimes:"
			dat +={"<table style="text-align:center;" border="1" cellspacing="0" width="100%">
			<tr>
			<th>Crime</th>
			<th>Details</th>
			<th>Author</th>
			<th>Time Added</th>
			</tr>"}
			for(var/datum/data/crime/c in current.fields["mi_crim"])
				dat += {"<tr><td>[c.crimeName]</td>
				<td>[c.crimeDetails]</td>
				<td>[c.author]</td>
				<td>[c.time]</td>
				</tr>"}
			dat += "</table>"

			dat += "<br>Major Crimes:"
			dat +={"<table style="text-align:center;" border="1" cellspacing="0" width="100%">
			<tr>
			<th>Crime</th>
			<th>Details</th>
			<th>Author</th>
			<th>Time Added</th>
			</tr>"}
			for(var/datum/data/crime/c in current.fields["ma_crim"])
				dat += {"<tr><td>[c.crimeName]</td>
				<td>[c.crimeDetails]</td>
				<td>[c.author]</td>
				<td>[c.time]</td>
				</tr>"}
			dat += "</table>"
		else
			dat += {"<span>** No security record found for this ID **</span>"}
	else
		dat += {"<a href='?src=[REF(src)];choice=Login'>------------</a><hr>"}

	var/datum/browser/popup = new(user, "warrant", "Security Warrant Console", 600, 400)
	popup.set_content(dat.Join())
	popup.set_title_image(user.browse_rsc_icon(src.icon, src.icon_state))
	popup.open()

/obj/machinery/computer/warrant/Topic(href, href_list)
	if(..())
		return
	var/mob/M = usr
	switch(href_list["choice"])
		if("Login")
			var/obj/item/card/id/scan = M.get_idcard(TRUE)
			authenticated = scan.registered_name
			if(authenticated)
				for(var/datum/data/record/R in GLOB.data_core.security)
					if(R.fields["name"] == authenticated)
						current = R
				playsound(src, 'sound/machines/terminal_on.ogg', 50, FALSE)
		if("Logout")
			current = null
			authenticated = null
			playsound(src, 'sound/machines/terminal_off.ogg', 50, FALSE)

		if("Pay")
			for(var/datum/data/crime/p in current.fields["citation"])
				if(p.dataId == text2num(href_list["cdataid"]))
					var/obj/item/holochip/C = M.is_holding_item_of_type(/obj/item/holochip)
					if(C && istype(C))
						var/pay = C.get_item_credit_value()
						if(!pay)
							to_chat(M, "<span class='warning'>[C] doesn't seem to be worth anything!</span>")
						else
							var/diff = p.fine - p.paid
							GLOB.data_core.payCitation(current.fields["id"], text2num(href_list["cdataid"]), pay)
							to_chat(M, "<span class='notice'>You have paid [pay] credit\s towards your fine.</span>")
							if (pay == diff || pay > diff || pay >= diff)
								investigate_log("Citation Paid off: <strong>[p.crimeName]</strong> Fine: [p.fine] | Paid off by [key_name(usr)]", INVESTIGATE_RECORDS)
								to_chat(M, "<span class='notice'>The fine has been paid in full.</span>")
							qdel(C)
							playsound(src, "terminal_type", 25, FALSE)
					else
						to_chat(M, "<span class='warning'>Fines can only be paid with holochips!</span>")
	updateUsrDialog()
	add_fingerprint(M)
