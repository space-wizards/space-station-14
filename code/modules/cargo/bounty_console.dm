#define PRINTER_TIMEOUT 10



/obj/machinery/computer/bounty
	name = "Nanotrasen bounty console"
	desc = "Used to check and claim bounties offered by Nanotrasen"
	icon_screen = "bounty"
	circuit = /obj/item/circuitboard/computer/bounty
	light_color = "#E2853D"//orange
	var/printer_ready = 0 //cooldown var

/obj/machinery/computer/bounty/Initialize()
	. = ..()
	printer_ready = world.time + PRINTER_TIMEOUT

/obj/machinery/computer/bounty/proc/print_paper()
	new /obj/item/paper/bounty_printout(loc)

/obj/item/paper/bounty_printout
	name = "paper - Bounties"

/obj/item/paper/bounty_printout/Initialize()
	. = ..()
	info = "<h2>Nanotrasen Cargo Bounties</h2></br>"
	update_icon()

	for(var/datum/bounty/B in GLOB.bounties_list)
		if(B.claimed)
			continue
		info += {"<h3>[B.name]</h3>
		<ul><li>Reward: [B.reward_string()]</li>
		<li>Completed: [B.completion_string()]</li></ul>"}

/obj/machinery/computer/bounty/ui_interact(mob/user)
	. = ..()

	if(!GLOB.bounties_list.len)
		setup_bounties()

	var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_CAR)
	var/list/dat = list({"<a href='?src=[REF(src)];refresh=1'>Refresh</a>
	<a href='?src=[REF(src)];refresh=1;choice=Print'>Print Paper</a>
	<p>Credits: <b>[D.account_balance]</b></p>
	<table style="text-align:center;" border="1" cellspacing="0" width="100%">
	<tr><th>Name</th><th>Description</th><th>Reward</th><th>Completion</th><th>Status</th></tr>"})
	for(var/datum/bounty/B in GLOB.bounties_list)
		if(B.claimed)
			dat += "<tr style='background-color:#294675;'>"
		else if(B.can_claim())
			dat += "<tr style='background-color:#4F7529;'>"
		else
			dat += "<tr style='background-color:#990000;'>"

		if(B.high_priority)
			dat += {"<td><b>[B.name]</b></td>
			<td><b>High Priority:</b> [B.description]</td>
			<td><b>[B.reward_string()]</b></td>"}
		else
			dat += {"<td>[B.name]</td>
			<td>[B.description]</td>
			<td>[B.reward_string()]</td>"}
		dat += "<td>[B.completion_string()]</td>"
		if(B.claimed)
			dat += "<td>Claimed</td>"
		else if(B.can_claim())
			dat += "<td><A href='?src=[REF(src)];refresh=1;choice=Claim;d_rec=[REF(B)]'>Claim</a></td>"
		else
			dat += "<td>Unclaimed</td>"
		dat += "</tr>"
	dat += "</table>"
	dat = dat.Join()
	var/datum/browser/popup = new(user, "bounties", "Nanotrasen Bounties", 700, 600)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(src.icon, src.icon_state))
	popup.open()

/obj/machinery/computer/bounty/Topic(href, href_list)
	if(..())
		return

	switch(href_list["choice"])
		if("Print")
			if(printer_ready < world.time)
				printer_ready = world.time + PRINTER_TIMEOUT
				print_paper()

		if("Claim")
			var/datum/bounty/B = locate(href_list["d_rec"]) in GLOB.bounties_list
			if(B)
				B.claim()

	if(href_list["refresh"])
		playsound(src, "terminal_type", 25, FALSE)

	updateUsrDialog()
