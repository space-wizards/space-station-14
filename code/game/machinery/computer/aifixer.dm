/obj/machinery/computer/aifixer
	name = "\improper AI system integrity restorer"
	desc = "Used with intelliCards containing nonfunctional AIs to restore them to working order."
	req_access = list(ACCESS_CAPTAIN, ACCESS_ROBOTICS, ACCESS_HEADS)
	var/mob/living/silicon/ai/occupier = null
	var/active = 0
	circuit = /obj/item/circuitboard/computer/aifixer
	icon_keyboard = "tech_key"
	icon_screen = "ai-fixer"
	light_color = LIGHT_COLOR_PINK

/obj/machinery/computer/aifixer/screwdriver_act(mob/living/user, obj/item/I)
	if(occupier)
		if(stat & (NOPOWER|BROKEN))
			to_chat(user, "<span class='warning'>The screws on [name]'s screen won't budge.</span>")
		else
			to_chat(user, "<span class='warning'>The screws on [name]'s screen won't budge and it emits a warning beep.</span>")
	else
		return ..()

/obj/machinery/computer/aifixer/ui_interact(mob/user)
	. = ..()

	var/dat = ""

	if (src.occupier)
		var/laws
		dat += "<h3>Stored AI: [src.occupier.name]</h3>"
		dat += "<b>System integrity:</b> [(src.occupier.health+100)/2]%<br>"

		if (src.occupier.laws.zeroth)
			laws += "<b>0:</b> [src.occupier.laws.zeroth]<BR>"

		for (var/index = 1, index <= src.occupier.laws.hacked.len, index++)
			var/law = src.occupier.laws.hacked[index]
			if (length(law) > 0)
				var/num = ionnum()
				laws += "<b>[num]:</b> [law]<BR>"

		for (var/index = 1, index <= src.occupier.laws.ion.len, index++)
			var/law = src.occupier.laws.ion[index]
			if (length(law) > 0)
				var/num = ionnum()
				laws += "<b>[num]:</b> [law]<BR>"

		var/number = 1
		for (var/index = 1, index <= src.occupier.laws.inherent.len, index++)
			var/law = src.occupier.laws.inherent[index]
			if (length(law) > 0)
				laws += "<b>[number]:</b> [law]<BR>"
				number++

		for (var/index = 1, index <= src.occupier.laws.supplied.len, index++)
			var/law = src.occupier.laws.supplied[index]
			if (length(law) > 0)
				laws += "<b>[number]:</b> [law]<BR>"
				number++

		dat += "<b>Laws:</b><br>[laws]<br>"

		if (src.occupier.stat == DEAD)
			dat += "<span class='bad'>AI non-functional</span>"
		else
			dat += "<span class='good'>AI functional</span>"
		if (!src.active)
			dat += {"<br><br><A href='byond://?src=[REF(src)];fix=1'>Begin Reconstruction</A>"}
		else
			dat += "<br><br>Reconstruction in process, please wait.<br>"
	dat += {"<br><A href='?src=[REF(user)];mach_close=computer'>Close</A>"}
	var/datum/browser/popup = new(user, "computer", "AI System Integrity Restorer", 400, 500)
	popup.set_content(dat)
	popup.set_title_image(user.browse_rsc_icon(src.icon, src.icon_state))
	popup.open()
	return

/obj/machinery/computer/aifixer/proc/Fix()
	use_power(1000)
	occupier.adjustOxyLoss(-5, 0)
	occupier.adjustFireLoss(-5, 0)
	occupier.adjustToxLoss(-5, 0)
	occupier.adjustBruteLoss(-5, 0)
	occupier.updatehealth()
	if(occupier.health >= 0 && occupier.stat == DEAD)
		occupier.revive(full_heal = FALSE, admin_revive = FALSE)
		if(!occupier.radio_enabled)
			occupier.radio_enabled = TRUE
			to_chat(occupier, "<span class='warning'>Your Subspace Transceiver has been enabled!</span>")
	return occupier.health < 100

/obj/machinery/computer/aifixer/process()
	if(..())
		if(active)
			var/oldstat = occupier.stat
			active = Fix()
			if(oldstat != occupier.stat)
				update_icon()
		updateDialog()

/obj/machinery/computer/aifixer/Topic(href, href_list)
	if(..())
		return
	if(href_list["fix"])
		to_chat(usr, "<span class='notice'>Reconstruction in progress. This will take several minutes.</span>")
		playsound(src, 'sound/machines/terminal_prompt_confirm.ogg', 25, FALSE)
		active = TRUE
		if(occupier)
			var/mob/living/silicon/ai/A = occupier
			A.notify_ghost_cloning("Your core files are being restored!", source = src)
		add_fingerprint(usr)
	updateUsrDialog()

/obj/machinery/computer/aifixer/update_overlays()
	. = ..()
	if(stat & (NOPOWER|BROKEN))
		return
	
	if(active)
		. += "ai-fixer-on"
	if (occupier)
		switch (occupier.stat)
			if (0)
				. += "ai-fixer-full"
			if (2)
				. += "ai-fixer-404"
	else
		. += "ai-fixer-empty"

/obj/machinery/computer/aifixer/transfer_ai(interaction, mob/user, mob/living/silicon/ai/AI, obj/item/aicard/card)
	if(!..())
		return
	//Downloading AI from card to terminal.
	if(interaction == AI_TRANS_FROM_CARD)
		if(stat & (NOPOWER|BROKEN))
			to_chat(user, "<span class='alert'>[src] is offline and cannot take an AI at this time.</span>")
			return
		AI.forceMove(src)
		occupier = AI
		AI.control_disabled = TRUE
		AI.radio_enabled = FALSE
		to_chat(AI, "<span class='alert'>You have been uploaded to a stationary terminal. Sadly, there is no remote access from here.</span>")
		to_chat(user, "<span class='notice'>Transfer successful</span>: [AI.name] ([rand(1000,9999)].exe) installed and executed successfully. Local copy has been removed.")
		card.AI = null
		update_icon()

	else //Uploading AI from terminal to card
		if(occupier && !active)
			to_chat(occupier, "<span class='notice'>You have been downloaded to a mobile storage device. Still no remote access.</span>")
			to_chat(user, "<span class='notice'>Transfer successful</span>: [occupier.name] ([rand(1000,9999)].exe) removed from host terminal and stored within local memory.")
			occupier.forceMove(card)
			card.AI = occupier
			occupier = null
			update_icon()
		else if (active)
			to_chat(user, "<span class='alert'>ERROR: Reconstruction in progress.</span>")
		else if (!occupier)
			to_chat(user, "<span class='alert'>ERROR: Unable to locate artificial intelligence.</span>")

/obj/machinery/computer/aifixer/on_deconstruction()
	if(occupier)
		QDEL_NULL(occupier)
