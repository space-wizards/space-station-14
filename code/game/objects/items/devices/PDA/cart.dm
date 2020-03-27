
#define CART_SECURITY			(1<<0)
#define CART_ENGINE				(1<<1)
#define CART_ATMOS				(1<<2)
#define CART_MEDICAL			(1<<3)
#define CART_MANIFEST			(1<<4)
#define CART_CLOWN				(1<<5)
#define CART_MIME				(1<<6)
#define CART_JANITOR			(1<<7)
#define CART_REAGENT_SCANNER	(1<<8)
#define CART_NEWSCASTER			(1<<9)
#define CART_REMOTE_DOOR		(1<<10)
#define CART_STATUS_DISPLAY		(1<<11)
#define CART_QUARTERMASTER		(1<<12)
#define CART_HYDROPONICS		(1<<13)
#define CART_DRONEPHONE			(1<<14)


/obj/item/cartridge
	name = "generic cartridge"
	desc = "A data cartridge for portable microcomputers."
	icon = 'icons/obj/pda.dmi'
	icon_state = "cart"
	item_state = "electronic"
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	w_class = WEIGHT_CLASS_TINY

	var/obj/item/integrated_signaler/radio = null

	var/access = 0 //Bit flags for cartridge access

	var/remote_door_id = ""

	var/bot_access_flags = 0 //Bit flags. Selection: SEC_BOT | MULE_BOT | FLOOR_BOT | CLEAN_BOT | MED_BOT | FIRE_BOT
	var/spam_enabled = 0 //Enables "Send to All" Option

	var/obj/item/pda/host_pda = null
	var/menu
	var/datum/data/record/active1 = null //General
	var/datum/data/record/active2 = null //Medical
	var/datum/data/record/active3 = null //Security
	var/obj/machinery/computer/monitor/powmonitor = null // Power Monitor
	var/list/powermonitors = list()
	var/message1	// used for status_displays
	var/message2
	var/list/stored_data = list()
	var/current_channel

	var/mob/living/simple_animal/bot/active_bot
	var/list/botlist = list()

/obj/item/cartridge/Initialize()
	. = ..()
	var/obj/item/pda/pda = loc
	if(istype(pda))
		host_pda = pda

/obj/item/cartridge/engineering
	name = "\improper Power-ON cartridge"
	icon_state = "cart-e"
	access = CART_ENGINE | CART_DRONEPHONE
	bot_access_flags = FLOOR_BOT

/obj/item/cartridge/atmos
	name = "\improper BreatheDeep cartridge"
	icon_state = "cart-a"
	access = CART_ATMOS | CART_DRONEPHONE
	bot_access_flags = FLOOR_BOT | FIRE_BOT

/obj/item/cartridge/medical
	name = "\improper Med-U cartridge"
	icon_state = "cart-m"
	access = CART_MEDICAL
	bot_access_flags = MED_BOT

/obj/item/cartridge/chemistry
	name = "\improper ChemWhiz cartridge"
	icon_state = "cart-chem"
	access = CART_REAGENT_SCANNER
	bot_access_flags = MED_BOT

/obj/item/cartridge/security
	name = "\improper R.O.B.U.S.T. cartridge"
	icon_state = "cart-s"
	access = CART_SECURITY
	bot_access_flags = SEC_BOT

/obj/item/cartridge/detective
	name = "\improper D.E.T.E.C.T. cartridge"
	icon_state = "cart-s"
	access = CART_SECURITY | CART_MEDICAL | CART_MANIFEST
	bot_access_flags = SEC_BOT

/obj/item/cartridge/janitor
	name = "\improper CustodiPRO cartridge"
	desc = "The ultimate in clean-room design."
	icon_state = "cart-j"
	access = CART_JANITOR | CART_DRONEPHONE
	bot_access_flags = CLEAN_BOT

/obj/item/cartridge/lawyer
	name = "\improper P.R.O.V.E. cartridge"
	icon_state = "cart-s"
	access = CART_SECURITY
	spam_enabled = 1

/obj/item/cartridge/curator
	name = "\improper Lib-Tweet cartridge"
	icon_state = "cart-s"
	access = CART_NEWSCASTER

/obj/item/cartridge/roboticist
	name = "\improper B.O.O.P. Remote Control cartridge"
	desc = "Packed with heavy duty quad-bot interlink!"
	bot_access_flags = FLOOR_BOT | CLEAN_BOT | MED_BOT | FIRE_BOT
	access = CART_DRONEPHONE

/obj/item/cartridge/signal
	name = "generic signaler cartridge"
	desc = "A data cartridge with an integrated radio signaler module."

/obj/item/cartridge/signal/toxins
	name = "\improper Signal Ace 2 cartridge"
	desc = "Complete with integrated radio signaler!"
	icon_state = "cart-tox"
	access = CART_REAGENT_SCANNER | CART_ATMOS

/obj/item/cartridge/signal/Initialize()
	. = ..()
	radio = new(src)



/obj/item/cartridge/quartermaster
	name = "space parts & space vendors cartridge"
	desc = "Perfect for the Quartermaster on the go!"
	icon_state = "cart-q"
	access = CART_QUARTERMASTER
	bot_access_flags = MULE_BOT

/obj/item/cartridge/head
	name = "\improper Easy-Record DELUXE cartridge"
	icon_state = "cart-h"
	access = CART_MANIFEST | CART_STATUS_DISPLAY

/obj/item/cartridge/hop
	name = "\improper HumanResources9001 cartridge"
	icon_state = "cart-h"
	access = CART_MANIFEST | CART_STATUS_DISPLAY | CART_JANITOR | CART_SECURITY | CART_NEWSCASTER | CART_QUARTERMASTER | CART_DRONEPHONE
	bot_access_flags = MULE_BOT | CLEAN_BOT

/obj/item/cartridge/hos
	name = "\improper R.O.B.U.S.T. DELUXE cartridge"
	icon_state = "cart-hos"
	access = CART_MANIFEST | CART_STATUS_DISPLAY | CART_SECURITY
	bot_access_flags = SEC_BOT


/obj/item/cartridge/ce
	name = "\improper Power-On DELUXE cartridge"
	icon_state = "cart-ce"
	access = CART_MANIFEST | CART_STATUS_DISPLAY | CART_ENGINE | CART_ATMOS | CART_DRONEPHONE
	bot_access_flags = FLOOR_BOT | FIRE_BOT

/obj/item/cartridge/cmo
	name = "\improper Med-U DELUXE cartridge"
	icon_state = "cart-cmo"
	access = CART_MANIFEST | CART_STATUS_DISPLAY | CART_REAGENT_SCANNER | CART_MEDICAL
	bot_access_flags = MED_BOT

/obj/item/cartridge/rd
	name = "\improper Signal Ace DELUXE cartridge"
	icon_state = "cart-rd"
	access = CART_MANIFEST | CART_STATUS_DISPLAY | CART_REAGENT_SCANNER | CART_ATMOS | CART_DRONEPHONE
	bot_access_flags = FLOOR_BOT | CLEAN_BOT | MED_BOT | FIRE_BOT

/obj/item/cartridge/rd/Initialize()
	. = ..()
	radio = new(src)

/obj/item/cartridge/captain
	name = "\improper Value-PAK cartridge"
	desc = "Now with 350% more value!" //Give the Captain...EVERYTHING! (Except Mime, Clown, and Syndie)
	icon_state = "cart-c"
	access = ~(CART_CLOWN | CART_MIME | CART_REMOTE_DOOR)
	bot_access_flags = SEC_BOT | MULE_BOT | FLOOR_BOT | CLEAN_BOT | MED_BOT | FIRE_BOT
	spam_enabled = 1

/obj/item/cartridge/captain/Initialize()
	. = ..()
	radio = new(src)

/obj/item/cartridge/proc/post_status(command, data1, data2)

	var/datum/radio_frequency/frequency = SSradio.return_frequency(FREQ_STATUS_DISPLAYS)

	if(!frequency)
		return

	var/datum/signal/status_signal = new(list("command" = command))
	switch(command)
		if("message")
			status_signal.data["msg1"] = data1
			status_signal.data["msg2"] = data2
		if("alert")
			status_signal.data["picture_state"] = data1

	frequency.post_signal(src, status_signal)

/obj/item/cartridge/proc/generate_menu(mob/user)
	if(!host_pda)
		return
	switch(host_pda.mode)
		if(40) //signaller
			menu = "<h4>[PDAIMG(signaler)] Remote Signaling System</h4>"

			menu += {"
<a href='byond://?src=[REF(src)];choice=Send Signal'>Send Signal</A><BR>
Frequency:
<a href='byond://?src=[REF(src)];choice=Signal Frequency;sfreq=-10'>-</a>
<a href='byond://?src=[REF(src)];choice=Signal Frequency;sfreq=-2'>-</a>
[format_frequency(radio.frequency)]
<a href='byond://?src=[REF(src)];choice=Signal Frequency;sfreq=2'>+</a>
<a href='byond://?src=[REF(src)];choice=Signal Frequency;sfreq=10'>+</a><br>
<br>
Code:
<a href='byond://?src=[REF(src)];choice=Signal Code;scode=-5'>-</a>
<a href='byond://?src=[REF(src)];choice=Signal Code;scode=-1'>-</a>
[radio.code]
<a href='byond://?src=[REF(src)];choice=Signal Code;scode=1'>+</a>
<a href='byond://?src=[REF(src)];choice=Signal Code;scode=5'>+</a><br>"}
		if (41) //crew manifest

			menu = "<h4>[PDAIMG(notes)] Crew Manifest</h4>"
			menu += "Entries cannot be modified from this terminal.<br><br>"
			if(GLOB.data_core.general)
				for (var/datum/data/record/t in sortRecord(GLOB.data_core.general))
					menu += "[t.fields["name"]] - [t.fields["rank"]]<br>"
			menu += "<br>"


		if (42) //status displays
			menu = "<h4>[PDAIMG(status)] Station Status Display Interlink</h4>"

			menu += "\[ <A HREF='?src=[REF(src)];choice=Status;statdisp=blank'>Clear</A> \]<BR>"
			menu += "\[ <A HREF='?src=[REF(src)];choice=Status;statdisp=shuttle'>Shuttle ETA</A> \]<BR>"
			menu += "\[ <A HREF='?src=[REF(src)];choice=Status;statdisp=message'>Message</A> \]"
			menu += "<ul><li> Line 1: <A HREF='?src=[REF(src)];choice=Status;statdisp=setmsg1'>[ message1 ? message1 : "(none)"]</A>"
			menu += "<li> Line 2: <A HREF='?src=[REF(src)];choice=Status;statdisp=setmsg2'>[ message2 ? message2 : "(none)"]</A></ul><br>"
			menu += "\[ Alert: <A HREF='?src=[REF(src)];choice=Status;statdisp=alert;alert=default'>None</A> |"
			menu += " <A HREF='?src=[REF(src)];choice=Status;statdisp=alert;alert=redalert'>Red Alert</A> |"
			menu += " <A HREF='?src=[REF(src)];choice=Status;statdisp=alert;alert=lockdown'>Lockdown</A> |"
			menu += " <A HREF='?src=[REF(src)];choice=Status;statdisp=alert;alert=biohazard'>Biohazard</A> \]<BR>"

		if (43)
			menu = "<h4>[PDAIMG(power)] Power Monitors - Please select one</h4><BR>"
			powmonitor = null
			powermonitors = list()
			var/powercount = 0



			var/turf/pda_turf = get_turf(src)
			for(var/obj/machinery/computer/monitor/pMon in GLOB.machines)
				if(pMon.stat & (NOPOWER | BROKEN)) //check to make sure the computer is functional
					continue
				if(pda_turf.z != pMon.z) //and that we're on the same zlevel as the computer (lore: limited signal strength)
					continue
				if(pMon.is_secret_monitor) //make sure it isn't a secret one (ie located on a ruin), allowing people to metagame that the location exists
					continue
				powercount++
				powermonitors += pMon


			if(!powercount)
				menu += "<span class='danger'>No connection<BR></span>"
			else

				menu += "<FONT SIZE=-1>"
				var/count = 0
				for(var/obj/machinery/computer/monitor/pMon in powermonitors)
					count++
					menu += "<a href='byond://?src=[REF(src)];choice=Power Select;target=[count]'>[pMon] - [get_area_name(pMon, TRUE)] </a><BR>"

				menu += "</FONT>"

		if (433)
			menu = "<h4>[PDAIMG(power)] Power Monitor </h4><BR>"
			if(!powmonitor || !powmonitor.get_powernet())
				menu += "<span class='danger'>No connection<BR></span>"
			else
				var/list/L = list()
				var/datum/powernet/connected_powernet = powmonitor.get_powernet()
				for(var/obj/machinery/power/terminal/term in connected_powernet.nodes)
					if(istype(term.master, /obj/machinery/power/apc))
						var/obj/machinery/power/apc/A = term.master
						L += A

				menu += "<PRE>Location: [get_area_name(powmonitor, TRUE)]<BR>Total power: [DisplayPower(connected_powernet.viewavail)]<BR>Total load:  [DisplayPower(connected_powernet.viewload)]<BR>"

				menu += "<FONT SIZE=-1>"

				if(L.len > 0)
					menu += "Area                           Eqp./Lgt./Env.  Load   Cell<HR>"

					var/list/S = list(" Off","AOff","  On", " AOn")
					var/list/chg = list("N","C","F")
//Neither copytext nor copytext_char is appropriate here; neither 30 UTF-8 code units nor 30 code points equates to 30 columns of output.
//Some glyphs are very tall or very wide while others are small or even take up no space at all.
//Emojis can take modifiers which are many characters but render as only one glyph.
//A proper solution here (as far as Unicode goes, maybe not ideal as far as markup goes, a table would be better)
//would be to use <span style="width: NNNpx; overflow: none;">[A.area.name]</span>
					for(var/obj/machinery/power/apc/A in L)
						menu += copytext_char(add_trailing(A.area.name, 30, " "), 1, 30)
						menu += " [S[A.equipment+1]] [S[A.lighting+1]] [S[A.environ+1]] [add_leading(DisplayPower(A.lastused_total), 6, " ")]  [A.cell ? "[add_leading(round(A.cell.percent()), 3, " ")]% [chg[A.charging+1]]" : "  N/C"]<BR>"

				menu += "</FONT></PRE>"

		if (44) //medical records //This thing only displays a single screen so it's hard to really get the sub-menu stuff working.
			menu = "<h4>[PDAIMG(medical)] Medical Record List</h4>"
			if(GLOB.data_core.general)
				for(var/datum/data/record/R in sortRecord(GLOB.data_core.general))
					menu += "<a href='byond://?src=[REF(src)];choice=Medical Records;target=[R.fields["id"]]'>[R.fields["id"]]: [R.fields["name"]]<br>"
			menu += "<br>"
		if(441)
			menu = "<h4>[PDAIMG(medical)] Medical Record</h4>"

			if(active1 in GLOB.data_core.general)
				menu += "Name: [active1.fields["name"]] ID: [active1.fields["id"]]<br>"
				menu += "Gender: [active1.fields["gender"]]<br>"
				menu += "Age: [active1.fields["age"]]<br>"
				menu += "Rank: [active1.fields["rank"]]<br>"
				menu += "Fingerprint: [active1.fields["fingerprint"]]<br>"
				menu += "Physical Status: [active1.fields["p_stat"]]<br>"
				menu += "Mental Status: [active1.fields["m_stat"]]<br>"
			else
				menu += "<b>Record Lost!</b><br>"

			menu += "<br>"

			menu += "<h4>[PDAIMG(medical)] Medical Data</h4>"
			if(active2 in GLOB.data_core.medical)
				menu += "Blood Type: [active2.fields["blood_type"]]<br><br>"

				menu += "Minor Disabilities: [active2.fields["mi_dis"]]<br>"
				menu += "Details: [active2.fields["mi_dis_d"]]<br><br>"

				menu += "Major Disabilities: [active2.fields["ma_dis"]]<br>"
				menu += "Details: [active2.fields["ma_dis_d"]]<br><br>"

				menu += "Allergies: [active2.fields["alg"]]<br>"
				menu += "Details: [active2.fields["alg_d"]]<br><br>"

				menu += "Current Diseases: [active2.fields["cdi"]]<br>"
				menu += "Details: [active2.fields["cdi_d"]]<br><br>"

				menu += "Important Notes: [active2.fields["notes"]]<br>"
			else
				menu += "<b>Record Lost!</b><br>"

			menu += "<br>"
		if (45) //security records
			menu = "<h4>[PDAIMG(cuffs)] Security Record List</h4>"
			if(GLOB.data_core.general)
				for (var/datum/data/record/R in sortRecord(GLOB.data_core.general))
					menu += "<a href='byond://?src=[REF(src)];choice=Security Records;target=[R.fields["id"]]'>[R.fields["id"]]: [R.fields["name"]]<br>"

			menu += "<br>"
		if(451)
			menu = "<h4>[PDAIMG(cuffs)] Security Record</h4>"

			if(active1 in GLOB.data_core.general)
				menu += "Name: [active1.fields["name"]] ID: [active1.fields["id"]]<br>"
				menu += "Gender: [active1.fields["gender"]]<br>"
				menu += "Age: [active1.fields["age"]]<br>"
				menu += "Rank: [active1.fields["rank"]]<br>"
				menu += "Fingerprint: [active1.fields["fingerprint"]]<br>"
				menu += "Physical Status: [active1.fields["p_stat"]]<br>"
				menu += "Mental Status: [active1.fields["m_stat"]]<br>"
			else
				menu += "<b>Record Lost!</b><br>"

			menu += "<br>"

			menu += "<h4>[PDAIMG(cuffs)] Security Data</h4>"
			if(active3 in GLOB.data_core.security)
				menu += "Criminal Status: [active3.fields["criminal"]]<br>"

				menu += text("<BR>\nMinor Crimes:")

				menu +={"<table style="text-align:center;" border="1" cellspacing="0" width="100%">
<tr>
<th>Crime</th>
<th>Details</th>
<th>Author</th>
<th>Time Added</th>
</tr>"}
				for(var/datum/data/crime/c in active3.fields["mi_crim"])
					menu += "<tr><td>[c.crimeName]</td>"
					menu += "<td>[c.crimeDetails]</td>"
					menu += "<td>[c.author]</td>"
					menu += "<td>[c.time]</td>"
					menu += "</tr>"
				menu += "</table>"

				menu += text("<BR>\nMajor Crimes:")

				menu +={"<table style="text-align:center;" border="1" cellspacing="0" width="100%">
<tr>
<th>Crime</th>
<th>Details</th>
<th>Author</th>
<th>Time Added</th>
</tr>"}
				for(var/datum/data/crime/c in active3.fields["ma_crim"])
					menu += "<tr><td>[c.crimeName]</td>"
					menu += "<td>[c.crimeDetails]</td>"
					menu += "<td>[c.author]</td>"
					menu += "<td>[c.time]</td>"
					menu += "</tr>"
				menu += "</table>"

				menu += "<BR>\nImportant Notes:<br>"
				menu += "[active3.fields["notes"]]"
			else
				menu += "<b>Record Lost!</b><br>"

			menu += "<br>"

		if (47) //quartermaster order records
			menu = "<h4>[PDAIMG(crate)] Supply Record Interlink</h4>"

			menu += "<BR><B>Supply shuttle</B><BR>"
			menu += "Location: "
			switch(SSshuttle.supply.mode)
				if(SHUTTLE_CALL)
					menu += "Moving to "
					if(!is_station_level(SSshuttle.supply.z))
						menu += "station"
					else
						menu += "CentCom"
					menu += " ([SSshuttle.supply.timeLeft(600)] Mins)"
				else
					menu += "At "
					if(!is_station_level(SSshuttle.supply.z))
						menu += "CentCom"
					else
						menu += "station"
			menu += "<BR>Current approved orders: <BR><ol>"
			for(var/S in SSshuttle.shoppinglist)
				var/datum/supply_order/SO = S
				menu += "<li>#[SO.id] - [SO.pack.name] approved by [SO.orderer] [SO.reason ? "([SO.reason])":""]</li>"
			menu += "</ol>"

			menu += "Current requests: <BR><ol>"
			for(var/S in SSshuttle.requestlist)
				var/datum/supply_order/SO = S
				menu += "<li>#[SO.id] - [SO.pack.name] requested by [SO.orderer]</li>"
			menu += "</ol><font size=\"-3\">Upgrade NOW to Space Parts & Space Vendors PLUS for full remote order control and inventory management."

		if (48) // quartermaster ore logs
			menu = list("<h4>[PDAIMG(crate)] Ore Silo Logs</h4>")
			if (GLOB.ore_silo_default)
				var/list/logs = GLOB.silo_access_logs[REF(GLOB.ore_silo_default)]
				var/len = LAZYLEN(logs)
				var/i = 0
				for(var/M in logs)
					if (++i > 30)
						menu += "(... older logs not shown ...)"
						break
					var/datum/ore_silo_log/entry = M
					menu += "[len - i]. [entry.formatted]<br><br>"
				if(i == 0)
					menu += "Nothing!"
			else
				menu += "<b>No ore silo detected!</b>"
			menu = jointext(menu, "")

		if (49) //janitorial locator
			menu = "<h4>[PDAIMG(bucket)] Persistent Custodial Object Locator</h4>"

			var/turf/cl = get_turf(src)
			if (cl)
				menu += "Current Orbital Location: <b>\[[cl.x],[cl.y]\]</b>"

				menu += "<h4>Located Mops:</h4>"

				var/ldat
				for (var/obj/item/mop/M in world)
					var/turf/ml = get_turf(M)

					if(ml)
						if (ml.z != cl.z)
							continue
						var/direction = get_dir(src, M)
						ldat += "Mop - <b>\[[ml.x],[ml.y] ([uppertext(dir2text(direction))])\]</b> - [M.reagents.total_volume ? "Wet" : "Dry"]<br>"

				if (!ldat)
					menu += "None"
				else
					menu += "[ldat]"

				menu += "<h4>Located Janitorial Cart:</h4>"

				ldat = null
				for (var/obj/structure/janitorialcart/B in world)
					var/turf/bl = get_turf(B)

					if(bl)
						if (bl.z != cl.z)
							continue
						var/direction = get_dir(src, B)
						ldat += "Cart - <b>\[[bl.x],[bl.y] ([uppertext(dir2text(direction))])\]</b> - Water level: [B.reagents.total_volume]/100<br>"

				if (!ldat)
					menu += "None"
				else
					menu += "[ldat]"

				menu += "<h4>Located Cleanbots:</h4>"

				ldat = null
				for (var/mob/living/simple_animal/bot/cleanbot/B in GLOB.alive_mob_list)
					var/turf/bl = get_turf(B)

					if(bl)
						if (bl.z != cl.z)
							continue
						var/direction = get_dir(src, B)
						ldat += "Cleanbot - <b>\[[bl.x],[bl.y] ([uppertext(dir2text(direction))])\]</b> - [B.on ? "Online" : "Offline"]<br>"

				if (!ldat)
					menu += "None"
				else
					menu += "[ldat]"

			else
				menu += "ERROR: Unable to determine current location."
			menu += "<br><br><A href='byond://?src=[REF(src)];choice=49'>Refresh GPS Locator</a>"

		if (53) // Newscaster
			menu = "<h4>[PDAIMG(notes)] Newscaster Access</h4>"
			menu += "<br> Current Newsfeed: <A href='byond://?src=[REF(src)];choice=Newscaster Switch Channel'>[current_channel ? current_channel : "None"]</a> <br>"
			var/datum/newscaster/feed_channel/current
			for(var/datum/newscaster/feed_channel/chan in GLOB.news_network.network_channels)
				if (chan.channel_name == current_channel)
					current = chan
			if(!current)
				menu += "<h5> ERROR : NO CHANNEL FOUND </h5>"
				return menu
			var/i = 1
			for(var/datum/newscaster/feed_message/msg in current.messages)
				menu +="-[msg.returnBody(-1)] <BR><FONT SIZE=1>\[Story by <FONT COLOR='maroon'>[msg.returnAuthor(-1)]</FONT>\]</FONT><BR>"
				menu +="<b><font size=1>[msg.comments.len] comment[msg.comments.len > 1 ? "s" : ""]</font></b><br>"
				if(msg.img)
					user << browse_rsc(msg.img, "tmp_photo[i].png")
					menu +="<img src='tmp_photo[i].png' width = '180'><BR>"
				i++
				for(var/datum/newscaster/feed_comment/comment in msg.comments)
					menu +="<font size=1><small>[comment.body]</font><br><font size=1><small><small><small>[comment.author] [comment.time_stamp]</small></small></small></small></font><br>"
			menu += "<br> <A href='byond://?src=[REF(src)];choice=Newscaster Message'>Post Message</a>"

		if (54) // Beepsky, Medibot, Floorbot, and Cleanbot access
			menu = "<h4>[PDAIMG(medbot)] Bots Interlink</h4>"
			bot_control()
		if (55) // Emoji Guidebook for mimes
			menu = "<h4>[PDAIMG(emoji)] Emoji Guidebook</h4>"
			var/static/list/emoji_icon_states
			var/static/emoji_table
			if(!emoji_table)
				var/datum/asset/spritesheet/sheet = get_asset_datum(/datum/asset/spritesheet/goonchat)
				var/list/collate = list("<br><table>")
				for(var/emoji in sortList(icon_states(icon('icons/emoji.dmi'))))
					var/tag = sheet.icon_tag("emoji-[emoji]")
					collate += "<tr><td>[emoji]</td><td>[tag]</td></tr>"
				collate += "</table><br>"
				emoji_table = collate.Join()

			menu += "<br> To use an emoji in a pda message, refer to the guide and add \":\" around the emoji. Your PDA supports the following emoji:<br>"
			menu += emoji_table

		if (99) //Newscaster message permission error
			menu = "<h5> ERROR : NOT AUTHORIZED [host_pda.id ? "" : "- ID SLOT EMPTY"] </h5>"

	return menu

/obj/item/cartridge/Topic(href, href_list)
	..()

	if(!usr.canUseTopic(src, !issilicon(usr)))
		usr.unset_machine()
		usr << browse(null, "window=pda")
		return

	switch(href_list["choice"])
		if("Medical Records")
			active1 = find_record("id", href_list["target"], GLOB.data_core.general)
			if(active1)
				active2 = find_record("id", href_list["target"], GLOB.data_core.medical)
			host_pda.mode = 441
			if(!active2)
				active1 = null

		if("Security Records")
			active1 = find_record("id", href_list["target"], GLOB.data_core.general)
			if(active1)
				active3 = find_record("id", href_list["target"], GLOB.data_core.security)
			host_pda.mode = 451
			if(!active3)
				active1 = null

		if("Send Signal")
			INVOKE_ASYNC(radio, /obj/item/integrated_signaler.proc/send_activation)

		if("Signal Frequency")
			var/new_frequency = sanitize_frequency(radio.frequency + text2num(href_list["sfreq"]))
			radio.set_frequency(new_frequency)

		if("Signal Code")
			radio.code += text2num(href_list["scode"])
			radio.code = round(radio.code)
			radio.code = min(100, radio.code)
			radio.code = max(1, radio.code)

		if("Status")
			switch(href_list["statdisp"])
				if("message")
					post_status("message", message1, message2)
				if("alert")
					post_status("alert", href_list["alert"])
				if("setmsg1")
					message1 = reject_bad_text(input("Line 1", "Enter Message Text", message1) as text|null, 40)
					updateSelfDialog()
				if("setmsg2")
					message2 = reject_bad_text(input("Line 2", "Enter Message Text", message2) as text|null, 40)
					updateSelfDialog()
				else
					post_status(href_list["statdisp"])
		if("Power Select")
			var/pnum = text2num(href_list["target"])
			powmonitor = powermonitors[pnum]
			host_pda.mode = 433

		if("Supply Orders")
			host_pda.mode =47

		if("Newscaster Access")
			host_pda.mode = 53

		if("Newscaster Message")
			var/host_pda_owner_name = host_pda.id ? "[host_pda.id.registered_name] ([host_pda.id.assignment])" : "Unknown"
			var/message = host_pda.msg_input()
			var/datum/newscaster/feed_channel/current
			for(var/datum/newscaster/feed_channel/chan in GLOB.news_network.network_channels)
				if (chan.channel_name == current_channel)
					current = chan
			if(current.locked && current.author != host_pda_owner_name)
				host_pda.mode = 99
				host_pda.Topic(null,list("choice"="Refresh"))
				return
			GLOB.news_network.SubmitArticle(message,host_pda.owner,current_channel)
			host_pda.Topic(null,list("choice"=num2text(host_pda.mode)))
			return

		if("Newscaster Switch Channel")
			current_channel = host_pda.msg_input()
			host_pda.Topic(null,list("choice"=num2text(host_pda.mode)))
			return

	//emoji previews
	if(href_list["emoji"])
		var/parse = emoji_parse(":[href_list["emoji"]]:")
		to_chat(usr, parse)

	//Bot control section! Viciously ripped from radios for being laggy and terrible.
	if(href_list["op"])
		switch(href_list["op"])

			if("control")
				active_bot = locate(href_list["bot"]) in GLOB.bots_list

			if("botlist")
				active_bot = null
			if("summon") //Args are in the correct order, they are stated here just as an easy reminder.
				active_bot.bot_control("summon", usr, host_pda.GetAccess())
			else //Forward all other bot commands to the bot itself!
				active_bot.bot_control(href_list["op"], usr)

	if(href_list["mule"]) //MULEbots are special snowflakes, and need different args due to how they work.
		var/mob/living/simple_animal/bot/mulebot/mule = active_bot
		if (istype(mule))
			mule.bot_control(href_list["mule"], usr, pda=TRUE)

	if(!host_pda)
		return
	host_pda.attack_self(usr)


/obj/item/cartridge/proc/bot_control()
	if(active_bot)
		menu += "<B>[active_bot]</B><BR> Status: (<A href='byond://?src=[REF(src)];op=control;bot=[REF(active_bot)]'>[PDAIMG(refresh)]<i>refresh</i></A>)<BR>"
		menu += "Model: [active_bot.model]<BR>"
		menu += "Location: [get_area(active_bot)]<BR>"
		menu += "Mode: [active_bot.get_mode()]"
		if(active_bot.allow_pai)
			menu += "<BR>pAI: "
			if(active_bot.paicard && active_bot.paicard.pai)
				menu += "[active_bot.paicard.pai.name]"
				if(active_bot.bot_core.allowed(usr))
					menu += " (<A href='byond://?src=[REF(src)];op=ejectpai'><i>eject</i></A>)"
			else
				menu += "<i>none</i>"

		//MULEs!
		if(active_bot.bot_type == MULE_BOT)
			var/mob/living/simple_animal/bot/mulebot/MULE = active_bot
			var/atom/Load = MULE.load
			menu += "<BR>Current Load: [ !Load ? "<i>none</i>" : "[Load.name] (<A href='byond://?src=[REF(src)];mule=unload'><i>unload</i></A>)" ]<BR>"
			menu += "Destination: [MULE.destination ? MULE.destination : "<i>None</i>"] (<A href='byond://?src=[REF(src)];mule=destination'><i>set</i></A>)<BR>"
			menu += "Set ID: [MULE.suffix] <A href='byond://?src=[REF(src)];mule=setid'><i> Modify</i></A><BR>"
			menu += "Power: [MULE.cell ? MULE.cell.percent() : 0]%<BR>"
			menu += "Home: [!MULE.home_destination ? "<i>none</i>" : MULE.home_destination ]<BR>"
			menu += "Delivery Reporting: <A href='byond://?src=[REF(src)];mule=report'>[MULE.report_delivery ? "(<B>On</B>)": "(<B>Off</B>)"]</A><BR>"
			menu += "Auto Return Home: <A href='byond://?src=[REF(src)];mule=autoret'>[MULE.auto_return ? "(<B>On</B>)": "(<B>Off</B>)"]</A><BR>"
			menu += "Auto Pickup Crate: <A href='byond://?src=[REF(src)];mule=autopick'>[MULE.auto_pickup ? "(<B>On</B>)": "(<B>Off</B>)"]</A><BR><BR>" //Hue.

			menu += "\[<A href='byond://?src=[REF(src)];mule=stop'>Stop</A>\] "
			menu += "\[<A href='byond://?src=[REF(src)];mule=go'>Proceed</A>\] "
			menu += "\[<A href='byond://?src=[REF(src)];mule=home'>Return Home</A>\]<BR>"

		else
			menu += "<BR>\[<A href='byond://?src=[REF(src)];op=patroloff'>Stop Patrol</A>\] "	//patrolon
			menu += "\[<A href='byond://?src=[REF(src)];op=patrolon'>Start Patrol</A>\] "	//patroloff
			menu += "\[<A href='byond://?src=[REF(src)];op=summon'>Summon Bot</A>\]<BR>"		//summon
			menu += "Keep an ID inserted to upload access codes upon summoning."

		menu += "<HR><A href='byond://?src=[REF(src)];op=botlist'>[PDAIMG(back)]Return to bot list</A>"
	else
		menu += "<BR><A href='byond://?src=[REF(src)];op=botlist'>[PDAIMG(refresh)]Scan for active bots</A><BR><BR>"
		var/turf/current_turf = get_turf(src)
		var/zlevel = current_turf.z
		var/botcount = 0
		for(var/B in GLOB.bots_list) //Git da botz
			var/mob/living/simple_animal/bot/Bot = B
			if(!Bot.on || Bot.z != zlevel || Bot.remote_disabled || !(bot_access_flags & Bot.bot_type)) //Only non-emagged bots on the same Z-level are detected!
				continue //Also, the PDA must have access to the bot type.
			menu += "<A href='byond://?src=[REF(src)];op=control;bot=[REF(Bot)]'><b>[Bot.name]</b> ([Bot.get_mode()])<BR>"
			botcount++
		if(!botcount) //No bots at all? Lame.
			menu += "No bots found.<BR>"
			return

	return menu

//If the cartridge adds a special line to the top of the messaging app
/obj/item/cartridge/proc/message_header()
	return ""

//If the cartridge adds something to each potetial messaging target
/obj/item/cartridge/proc/message_special(obj/item/pda/target)
	return ""

//This is called for special abilities of cartridges
/obj/item/cartridge/proc/special(mob/living/user, list/params)
