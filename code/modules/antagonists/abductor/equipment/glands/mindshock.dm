/obj/item/organ/heart/gland/mindshock
	true_name = "neural crosstalk uninhibitor"
	cooldown_low = 400
	cooldown_high = 700
	uses = -1
	icon_state = "mindshock"
	mind_control_uses = 1
	mind_control_duration = 6000
	var/list/mob/living/carbon/human/broadcasted_mobs = list()

/obj/item/organ/heart/gland/mindshock/activate()
	to_chat(owner, "<span class='notice'>You get a headache.</span>")

	var/turf/T = get_turf(owner)
	for(var/mob/living/carbon/H in orange(4,T))
		if(H == owner)
			continue
		switch(pick(1,3))
			if(1)
				to_chat(H, "<span class='userdanger'>You hear a loud buzz in your head, silencing your thoughts!</span>")
				H.Stun(50)
			if(2)
				to_chat(H, "<span class='warning'>You hear an annoying buzz in your head.</span>")
				H.confused += 15
				H.adjustOrganLoss(ORGAN_SLOT_BRAIN, 10, 160)
			if(3)
				H.hallucination += 60

/obj/item/organ/heart/gland/mindshock/mind_control(command, mob/living/user)
	if(!ownerCheck() || !mind_control_uses || active_mind_control)
		return FALSE
	mind_control_uses--
	for(var/mob/M in oview(7, owner))
		if(!ishuman(M))
			continue
		var/mob/living/carbon/human/H = M
		if(H.stat)
			continue

		broadcasted_mobs += H
		to_chat(H, "<span class='userdanger'>You suddenly feel an irresistible compulsion to follow an order...</span>")
		to_chat(H, "<span class='mind_control'>[command]</span>")

		message_admins("[key_name(user)] broadcasted an abductor mind control message from [key_name(owner)] to [key_name(H)]: [command]")

		var/obj/screen/alert/mind_control/mind_alert = H.throw_alert("mind_control", /obj/screen/alert/mind_control)
		mind_alert.command = command

	if(LAZYLEN(broadcasted_mobs))
		active_mind_control = TRUE
		addtimer(CALLBACK(src, .proc/clear_mind_control), mind_control_duration)

	update_gland_hud()
	return TRUE

/obj/item/organ/heart/gland/mindshock/clear_mind_control()
	if(!active_mind_control || !LAZYLEN(broadcasted_mobs))
		return FALSE
	for(var/M in broadcasted_mobs)
		var/mob/living/carbon/human/H = M
		to_chat(H, "<span class='userdanger'>You feel the compulsion fade, and you <i>completely forget</i> about your previous orders.</span>")
		H.clear_alert("mind_control")
	active_mind_control = FALSE
	return TRUE
