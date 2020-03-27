/datum/disease/brainrot
	name = "Brainrot"
	max_stages = 4
	spread_text = "On contact"
	spread_flags = DISEASE_SPREAD_BLOOD | DISEASE_SPREAD_CONTACT_SKIN | DISEASE_SPREAD_CONTACT_FLUIDS
	cure_text = "Mannitol"
	cures = list(/datum/reagent/medicine/mannitol)
	agent = "Cryptococcus Cosmosis"
	viable_mobtypes = list(/mob/living/carbon/human)
	cure_chance = 15//higher chance to cure, since two reagents are required
	desc = "This disease destroys the braincells, causing brain fever, brain necrosis and general intoxication."
	required_organs = list(/obj/item/organ/brain)
	severity = DISEASE_SEVERITY_HARMFUL

/datum/disease/brainrot/stage_act() //Removed toxloss because damaging diseases are pretty horrible. Last round it killed the entire station because the cure didn't work -- Urist -ACTUALLY Removed rather than commented out, I don't see it returning - RR
	..()

	switch(stage)
		if(2)
			if(prob(2))
				affected_mob.emote("blink")
			if(prob(2))
				affected_mob.emote("yawn")
			if(prob(2))
				to_chat(affected_mob, "<span class='danger'>You don't feel like yourself.</span>")
			if(prob(5))
				affected_mob.adjustOrganLoss(ORGAN_SLOT_BRAIN, 1, 170)
				affected_mob.updatehealth()
		if(3)
			if(prob(2))
				affected_mob.emote("stare")
			if(prob(2))
				affected_mob.emote("drool")
			if(prob(10))
				affected_mob.adjustOrganLoss(ORGAN_SLOT_BRAIN, 2, 170)
				affected_mob.updatehealth()
				if(prob(2))
					to_chat(affected_mob, "<span class='danger'>Your try to remember something important...but can't.</span>")

		if(4)
			if(prob(2))
				affected_mob.emote("stare")
			if(prob(2))
				affected_mob.emote("drool")
			if(prob(15))
				affected_mob.adjustOrganLoss(ORGAN_SLOT_BRAIN, 3, 170)
				affected_mob.updatehealth()
				if(prob(2))
					to_chat(affected_mob, "<span class='danger'>Strange buzzing fills your head, removing all thoughts.</span>")
			if(prob(3))
				to_chat(affected_mob, "<span class='danger'>You lose consciousness...</span>")
				affected_mob.visible_message("<span class='warning'>[affected_mob] suddenly collapses!</span>", \
											"<span class='userdanger'>You suddenly collapse!</span>")
				affected_mob.Unconscious(rand(100,200))
				if(prob(1))
					affected_mob.emote("snore")
			if(prob(15))
				affected_mob.stuttering += 3

	return
