/datum/disease/pierrot_throat
	name = "Pierrot's Throat"
	max_stages = 4
	spread_text = "Airborne"
	cure_text = "Banana products, especially banana bread."
	cures = list(/datum/reagent/consumable/banana)
	cure_chance = 75
	agent = "H0NI<42 Virus"
	viable_mobtypes = list(/mob/living/carbon/human)
	permeability_mod = 0.75
	desc = "If left untreated the subject will probably drive others to insanity."
	severity = DISEASE_SEVERITY_MEDIUM

/datum/disease/pierrot_throat/stage_act()
	..()
	switch(stage)
		if(1)
			if(prob(10))
				to_chat(affected_mob, "<span class='danger'>You feel a little silly.</span>")
		if(2)
			if(prob(10))
				to_chat(affected_mob, "<span class='danger'>You start seeing rainbows.</span>")
		if(3)
			if(prob(10))
				to_chat(affected_mob, "<span class='danger'>Your thoughts are interrupted by a loud <b>HONK!</b></span>")
		if(4)
			if(prob(5))
				affected_mob.say( pick( list("HONK!", "Honk!", "Honk.", "Honk?", "Honk!!", "Honk?!", "Honk...") ) , forced = "pierrot's throat")

/datum/disease/pierrot_throat/after_add()
	RegisterSignal(affected_mob, COMSIG_MOB_SAY, .proc/handle_speech)


/datum/disease/pierrot_throat/proc/handle_speech(datum/source, list/speech_args)
	var/message = speech_args[SPEECH_MESSAGE]
	var/list/split_message = splittext(message, " ") //List each word in the message
	var/applied = 0
	for (var/i in 1 to length(split_message))
		if(prob(3 * stage)) //Stage 1: 3% Stage 2: 6% Stage 3: 9% Stage 4: 12%
			if(findtext(split_message[i], "*") || findtext(split_message[i], ";") || findtext(split_message[i], ":"))
				continue
			split_message[i] = "HONK"
			if (applied++ > stage)
				break
	if (applied)
		speech_args[SPEECH_SPANS] |= SPAN_CLOWN // a little bonus
	message = jointext(split_message, " ")
	speech_args[SPEECH_MESSAGE] = message


/datum/disease/pierrot_throat/Destroy()
	UnregisterSignal(affected_mob, COMSIG_MOB_SAY)
	return ..()

/datum/disease/pierrot_throat/remove_disease()
	UnregisterSignal(affected_mob, COMSIG_MOB_SAY)
	return ..()
