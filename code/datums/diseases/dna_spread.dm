/datum/disease/dnaspread
	name = "Space Retrovirus"
	max_stages = 4
	spread_text = "On contact"
	spread_flags = DISEASE_SPREAD_BLOOD | DISEASE_SPREAD_CONTACT_SKIN | DISEASE_SPREAD_CONTACT_FLUIDS
	cure_text = "Mutadone"
	cures = list(/datum/reagent/medicine/mutadone)
	disease_flags = CAN_CARRY|CAN_RESIST|CURABLE
	agent = "S4E1 retrovirus"
	viable_mobtypes = list(/mob/living/carbon/human)
	var/datum/dna/original_dna = null
	var/transformed = 0
	desc = "This disease transplants the genetic code of the initial vector into new hosts."
	severity = DISEASE_SEVERITY_MEDIUM


/datum/disease/dnaspread/stage_act()
	..()
	if(!affected_mob.dna)
		cure()
	if((NOTRANSSTING in affected_mob.dna.species.species_traits) || (NO_DNA_COPY in affected_mob.dna.species.species_traits)) //Only species that can be spread by transformation sting can be spread by the retrovirus
		cure()

	if(!strain_data["dna"])
		//Absorbs the target DNA.
		strain_data["dna"] = new affected_mob.dna.type
		affected_mob.dna.copy_dna(strain_data["dna"])
		carrier = TRUE
		stage = 4
		return

	switch(stage)
		if(2 || 3) //Pretend to be a cold and give time to spread.
			if(prob(8))
				affected_mob.emote("sneeze")
			if(prob(8))
				affected_mob.emote("cough")
			if(prob(1))
				to_chat(affected_mob, "<span class='danger'>Your muscles ache.</span>")
				if(prob(20))
					affected_mob.take_bodypart_damage(1)
			if(prob(1))
				to_chat(affected_mob, "<span class='danger'>Your stomach hurts.</span>")
				if(prob(20))
					affected_mob.adjustToxLoss(2)
					affected_mob.updatehealth()
		if(4)
			if(!transformed && !carrier)
				//Save original dna for when the disease is cured.
				original_dna = new affected_mob.dna.type
				affected_mob.dna.copy_dna(original_dna)

				to_chat(affected_mob, "<span class='danger'>You don't feel like yourself..</span>")
				var/datum/dna/transform_dna = strain_data["dna"]

				transform_dna.transfer_identity(affected_mob, transfer_SE = 1)
				affected_mob.real_name = affected_mob.dna.real_name
				affected_mob.updateappearance(mutcolor_update=1)
				affected_mob.domutcheck()

				transformed = 1
				carrier = 1 //Just chill out at stage 4

	return

/datum/disease/dnaspread/Destroy()
	if (original_dna && transformed && affected_mob)
		original_dna.transfer_identity(affected_mob, transfer_SE = 1)
		affected_mob.real_name = affected_mob.dna.real_name
		affected_mob.updateappearance(mutcolor_update=1)
		affected_mob.domutcheck()

		to_chat(affected_mob, "<span class='notice'>You feel more like yourself.</span>")
	return ..()
