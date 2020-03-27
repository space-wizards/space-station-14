/obj/machinery/abductor/experiment
	name = "experimentation machine"
	desc = "A large man-sized tube sporting a complex array of surgical machinery."
	icon = 'icons/obj/abductor.dmi'
	icon_state = "experiment-open"
	density = FALSE
	state_open = TRUE
	var/points = 0
	var/credits = 0
	var/list/history
	var/list/abductee_minds
	var/flash = " - || - "
	var/obj/machinery/abductor/console/console
	var/message_cooldown = 0
	var/breakout_time = 450

/obj/machinery/abductor/experiment/MouseDrop_T(mob/target, mob/user)
	var/mob/living/L = user
	if(user.stat || (isliving(user) && (!(L.mobility_flags & MOBILITY_STAND) || !(L.mobility_flags & MOBILITY_UI))) || !Adjacent(user) || !target.Adjacent(user) || !ishuman(target))
		return
	if(isabductor(target))
		return
	close_machine(target)

/obj/machinery/abductor/experiment/attack_hand(mob/user)
	. = ..()
	if(.)
		return

	experimentUI(user)

/obj/machinery/abductor/experiment/open_machine()
	if(!state_open && !panel_open)
		..()

/obj/machinery/abductor/experiment/close_machine(mob/target)
	for(var/A in loc)
		if(isabductor(A))
			return
	if(state_open && !panel_open)
		..(target)

/obj/machinery/abductor/experiment/relaymove(mob/user)
	if(user.stat != CONSCIOUS)
		return
	if(message_cooldown <= world.time)
		message_cooldown = world.time + 50
		to_chat(user, "<span class='warning'>[src]'s door won't budge!</span>")

/obj/machinery/abductor/experiment/container_resist(mob/living/user)
	user.changeNext_move(CLICK_CD_BREAKOUT)
	user.last_special = world.time + CLICK_CD_BREAKOUT
	user.visible_message("<span class='notice'>You see [user] kicking against the door of [src]!</span>", \
		"<span class='notice'>You lean on the back of [src] and start pushing the door open... (this will take about [DisplayTimeText(breakout_time)].)</span>", \
		"<span class='hear'>You hear a metallic creaking from [src].</span>")
	if(do_after(user,(breakout_time), target = src))
		if(!user || user.stat != CONSCIOUS || user.loc != src || state_open)
			return
		user.visible_message("<span class='warning'>[user] successfully broke out of [src]!</span>", \
			"<span class='notice'>You successfully break out of [src]!</span>")
		open_machine()

/obj/machinery/abductor/experiment/proc/dissection_icon(mob/living/carbon/human/H)
	var/icon/photo = null
	var/g = (H.gender == FEMALE) ? "f" : "m"
	if(H.dna.species.use_skintones)
		photo = icon("icon" = 'icons/mob/human.dmi', "icon_state" = "[H.skin_tone]_[g]")
	else
		photo = icon("icon" = 'icons/mob/human.dmi', "icon_state" = "[H.dna.species.id]_[g]")
		photo.Blend("#[H.dna.features["mcolor"]]", ICON_MULTIPLY)

	var/icon/eyes
	if(EYECOLOR in H.dna.species.species_traits)
		eyes = icon("icon" = 'icons/mob/human_face.dmi', "icon_state" = "eyes")
		eyes.Blend("#[H.eye_color]", ICON_MULTIPLY)

	var/datum/sprite_accessory/S
	S = GLOB.hairstyles_list[H.hairstyle]
	if(S && (HAIR in H.dna.species.species_traits))
		var/icon/hair = icon("icon" = S.icon, "icon_state" = "[S.icon_state]")
		hair.Blend("#[H.hair_color]", ICON_MULTIPLY)
		eyes.Blend(hair, ICON_OVERLAY)

	S = GLOB.facial_hairstyles_list[H.facial_hairstyle]
	if(S && (FACEHAIR in H.dna.species.species_traits))
		var/icon/facial = icon("icon" = S.icon, "icon_state" = "[S.icon_state]")
		facial.Blend("#[H.facial_hair_color]", ICON_MULTIPLY)
		eyes.Blend(facial, ICON_OVERLAY)

	if(eyes)
		photo.Blend(eyes, ICON_OVERLAY)

	var/icon/splat = icon("icon" = 'icons/mob/dam_mob.dmi',"icon_state" = "chest30")
	photo.Blend(splat,ICON_OVERLAY)

	return photo

/obj/machinery/abductor/experiment/proc/experimentUI(mob/user)
	var/dat
	dat += "<h3> Experiment </h3>"
	if(occupant)
		var/obj/item/photo/P = new
		P.picture = new
		P.picture.picture_image = icon(dissection_icon(occupant), dir = SOUTH)
		user << browse_rsc(P.picture.picture_image, "dissection_img")
		dat += "<table><tr><td>"
		dat += "<img src=dissection_img height=80 width=80>" //Avert your eyes
		dat += "</td><td>"
		dat += "<a href='?src=[REF(src)];experiment=1'>Probe</a><br>"
		dat += "<a href='?src=[REF(src)];experiment=2'>Dissect</a><br>"
		dat += "<a href='?src=[REF(src)];experiment=3'>Analyze</a><br>"
		dat += "</td></tr></table>"
	else
		dat += "<span class='linkOff'>Experiment </span>"

	if(!occupant)
		dat += "<h3>Machine Unoccupied</h3>"
	else
		dat += "<h3>Subject Status : </h3>"
		dat += "[occupant.name] => "
		var/mob/living/mob_occupant = occupant
		switch(mob_occupant.stat)
			if(CONSCIOUS)
				dat += "<span class='good'>Conscious</span>"
			if(UNCONSCIOUS)
				dat += "<span class='average'>Unconscious</span>"
			else // DEAD
				dat += "<span class='bad'>Deceased</span>"
	dat += "<br>"
	dat += "[flash]"
	dat += "<br>"
	dat += "<a href='?src=[REF(src)];refresh=1'>Scan</a>"
	dat += "<a href='?src=[REF(src)];[state_open ? "close=1'>Close</a>" : "open=1'>Open</a>"]"
	var/datum/browser/popup = new(user, "experiment", "Probing Console", 300, 300)
	popup.set_title_image(user.browse_rsc_icon(icon, icon_state))
	popup.set_content(dat)
	popup.open()

/obj/machinery/abductor/experiment/Topic(href, href_list)
	if(..() || usr == occupant)
		return
	usr.set_machine(src)
	if(href_list["refresh"])
		updateUsrDialog()
		return
	if(href_list["open"])
		open_machine()
		return
	if(href_list["close"])
		close_machine()
		return
	if(occupant)
		var/mob/living/mob_occupant = occupant
		if(mob_occupant.stat != DEAD)
			if(href_list["experiment"])
				flash = Experiment(occupant,href_list["experiment"],usr)
	updateUsrDialog()
	add_fingerprint(usr)

/obj/machinery/abductor/experiment/proc/Experiment(mob/occupant,type,mob/user)
	LAZYINITLIST(history)
	var/mob/living/carbon/human/H = occupant

	var/datum/antagonist/abductor/user_abductor = user.mind.has_antag_datum(/datum/antagonist/abductor)
	if(!user_abductor)
		return "<span class='bad'>Authorization failure. Contact mothership immidiately.</span>"

	var/point_reward = 0
	if(H in history)
		return "<span class='bad'>Specimen already in database.</span>"
	if(H.stat == DEAD)
		say("Specimen deceased - please provide fresh sample.")
		return "<span class='bad'>Specimen deceased.</span>"
	var/obj/item/organ/heart/gland/GlandTest = locate() in H.internal_organs
	if(!GlandTest)
		say("Experimental dissection not detected!")
		return "<span class='bad'>No glands detected!</span>"
	if(H.mind != null && H.ckey != null)
		LAZYINITLIST(abductee_minds)
		LAZYADD(history, H)
		LAZYADD(abductee_minds, H.mind)
		say("Processing specimen...")
		sleep(5)
		switch(text2num(type))
			if(1)
				to_chat(H, "<span class='warning'>You feel violated.</span>")
			if(2)
				to_chat(H, "<span class='warning'>You feel yourself being sliced apart and put back together.</span>")
			if(3)
				to_chat(H, "<span class='warning'>You feel intensely watched.</span>")
		sleep(5)
		user_abductor.team.abductees += H.mind
		H.mind.add_antag_datum(/datum/antagonist/abductee)

		for(var/obj/item/organ/heart/gland/G in H.internal_organs)
			G.Start()
			point_reward++
		if(point_reward > 0)
			open_machine()
			SendBack(H)
			playsound(src.loc, 'sound/machines/ding.ogg', 50, TRUE)
			points += point_reward
			credits += point_reward
			return "<span class='good'>Experiment successful! [point_reward] new data-points collected.</span>"
		else
			playsound(src.loc, 'sound/machines/buzz-sigh.ogg', 50, TRUE)
			return "<span class='bad'>Experiment failed! No replacement organ detected.</span>"
	else
		say("Brain activity nonexistent - disposing sample...")
		open_machine()
		SendBack(H)
		return "<span class='bad'>Specimen braindead - disposed.</span>"


/obj/machinery/abductor/experiment/proc/SendBack(mob/living/carbon/human/H)
	H.Sleeping(160)
	H.uncuff()
	if(console && console.pad && console.pad.teleport_target)
		H.forceMove(console.pad.teleport_target)
		return
	//Area not chosen / It's not safe area - teleport to arrivals
	SSjob.SendToLateJoin(H, FALSE)
	return


/obj/machinery/abductor/experiment/update_icon_state()
	if(state_open)
		icon_state = "experiment-open"
	else
		icon_state = "experiment"
