/datum/disease/heart_failure
	form = "Condition"
	name = "Myocardial Infarction"
	max_stages = 5
	stage_prob = 2
	cure_text = "Heart replacement surgery to cure. Defibrillation (or as a last resort, uncontrolled electric shocking) may also be effective after the onset of cardiac arrest. Penthrite can also mitigate cardiac arrest."
	agent = "Shitty Heart"
	viable_mobtypes = list(/mob/living/carbon/human)
	permeability_mod = 1
	desc = "If left untreated the subject will die!"
	severity = "Dangerous!"
	disease_flags = CAN_CARRY|CAN_RESIST
	spread_flags = DISEASE_SPREAD_NON_CONTAGIOUS
	visibility_flags = HIDDEN_PANDEMIC
	required_organs = list(/obj/item/organ/heart)
	bypasses_immunity = TRUE // Immunity is based on not having an appendix; this isn't a virus
	var/sound = FALSE

/datum/disease/heart_failure/Copy()
	var/datum/disease/heart_failure/D = ..()
	D.sound = sound
	return D

/datum/disease/heart_failure/stage_act()
	..()
	var/obj/item/organ/heart/O = affected_mob.getorgan(/obj/item/organ/heart)
	var/mob/living/carbon/H = affected_mob
	if(O && H.can_heartattack())
		switch(stage)
			if(1 to 2)
				if(prob(2))
					to_chat(H, "<span class='warning'>You feel [pick("discomfort", "pressure", "a burning sensation", "pain")] in your chest.</span>")
				if(prob(2))
					to_chat(H, "<span class='warning'>You feel dizzy.</span>")
					H.confused += 6
				if(prob(3))
					to_chat(H, "<span class='warning'>You feel [pick("full", "nauseated", "sweaty", "weak", "tired", "short on breath", "uneasy")].</span>")
			if(3 to 4)
				if(!sound)
					H.playsound_local(H, 'sound/health/slowbeat.ogg',40,0, channel = CHANNEL_HEARTBEAT)
					sound = TRUE
				if(prob(3))
					to_chat(H, "<span class='danger'>You feel a sharp pain in your chest!</span>")
					if(prob(25))
						affected_mob.vomit(95)
					H.emote("cough")
					H.Paralyze(40)
					H.losebreath += 4
				if(prob(3))
					to_chat(H, "<span class='danger'>You feel very weak and dizzy...</span>")
					H.confused += 8
					H.adjustStaminaLoss(40)
					H.emote("cough")
			if(5)
				H.stop_sound_channel(CHANNEL_HEARTBEAT)
				H.playsound_local(H, 'sound/effects/singlebeat.ogg', 100, 0)
				if(H.stat == CONSCIOUS)
					H.visible_message("<span class='danger'>[H] clutches at [H.p_their()] chest as if [H.p_their()] heart is stopping!</span>", \
						"<span class='userdanger'>You feel a terrible pain in your chest, as if your heart has stopped!</span>")
				H.adjustStaminaLoss(60)
				H.set_heartattack(TRUE)
				H.reagents.add_reagent(/datum/reagent/medicine/C2/penthrite, 3) // To give the victim a final chance to shock their heart before losing consciousness
				cure()
	else
		cure()
