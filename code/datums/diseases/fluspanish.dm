/datum/disease/fluspanish
	name = "Spanish inquisition Flu"
	max_stages = 3
	spread_text = "Airborne"
	cure_text = "Spaceacillin & Anti-bodies to the common flu"
	cures = list(/datum/reagent/medicine/spaceacillin)
	cure_chance = 10
	agent = "1nqu1s1t10n flu virion"
	viable_mobtypes = list(/mob/living/carbon/human)
	permeability_mod = 0.75
	desc = "If left untreated the subject will burn to death for being a heretic."
	severity = DISEASE_SEVERITY_DANGEROUS

/datum/disease/fluspanish/stage_act()
	..()
	switch(stage)
		if(2)
			affected_mob.adjust_bodytemperature(10)
			if(prob(5))
				affected_mob.emote("sneeze")
			if(prob(5))
				affected_mob.emote("cough")
			if(prob(1))
				to_chat(affected_mob, "<span class='danger'>You're burning in your own skin!</span>")
				affected_mob.take_bodypart_damage(0,5)

		if(3)
			affected_mob.adjust_bodytemperature(20)
			if(prob(5))
				affected_mob.emote("sneeze")
			if(prob(5))
				affected_mob.emote("cough")
			if(prob(5))
				to_chat(affected_mob, "<span class='danger'>You're burning in your own skin!</span>")
				affected_mob.take_bodypart_damage(0,5)
	return
