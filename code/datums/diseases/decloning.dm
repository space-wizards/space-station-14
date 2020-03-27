/datum/disease/decloning
	form = "Virus"
	name = "Cellular Degeneration"
	max_stages = 5
	stage_prob = 1
	cure_text = "Rezadone or death."
	agent = "Severe Genetic Damage"
	viable_mobtypes = list(/mob/living/carbon/human)
	desc = @"If left untreated the subject will [REDACTED]!"
	severity = "Dangerous!"
	cures = list(/datum/reagent/medicine/rezadone)
	disease_flags = CAN_CARRY|CAN_RESIST
	spread_flags = DISEASE_SPREAD_NON_CONTAGIOUS
	process_dead = TRUE

/datum/disease/decloning/stage_act()
	..()
	if(affected_mob.stat == DEAD)
		cure()
		return
	switch(stage)
		if(2)
			if(prob(2))
				affected_mob.emote("itch")
			if(prob(2))
				affected_mob.emote("yawn")
		if(3)
			if(prob(2))
				affected_mob.emote("itch")
			if(prob(2))
				affected_mob.emote("drool")
			if(prob(3))
				affected_mob.adjustCloneLoss(1)
			if(prob(2))
				to_chat(affected_mob, "<span class='danger'>Your skin feels strange.</span>")

		if(4)
			if(prob(2))
				affected_mob.emote("itch")
			if(prob(2))
				affected_mob.emote("drool")
			if(prob(5))
				affected_mob.adjustOrganLoss(ORGAN_SLOT_BRAIN, 1, 170)
				affected_mob.adjustCloneLoss(2)
			if(prob(15))
				affected_mob.stuttering += 3
		if(5)
			if(prob(2))
				affected_mob.emote("itch")
			if(prob(2))
				affected_mob.emote("drool")
			if(prob(5))
				to_chat(affected_mob, "<span class='danger'>Your skin starts degrading!</span>")
			if(prob(10))
				affected_mob.adjustCloneLoss(5)
				affected_mob.adjustOrganLoss(ORGAN_SLOT_BRAIN, 2, 170)
			if(affected_mob.cloneloss >= 100)
				affected_mob.visible_message("<span class='danger'>[affected_mob] skin turns to dust!</span>", "<span class'boldwarning'>Your skin turns to dust!</span>")
				affected_mob.dust()
