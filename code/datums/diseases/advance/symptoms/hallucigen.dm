/*
//////////////////////////////////////

Hallucigen

	Very noticable.
	Lowers resistance considerably.
	Decreases stage speed.
	Reduced transmittable.
	Critical Level.

Bonus
	Makes the affected mob be hallucinated for short periods of time.

//////////////////////////////////////
*/

/datum/symptom/hallucigen
	name = "Hallucigen"
	desc = "The virus stimulates the brain, causing occasional hallucinations."
	stealth = -1
	resistance = -3
	stage_speed = -3
	transmittable = -1
	level = 5
	severity = 2
	base_message_chance = 25
	symptom_delay_min = 25
	symptom_delay_max = 90
	var/fake_healthy = FALSE
	threshold_descs = list(
		"Stage Speed 7" = "Increases the amount of hallucinations.",
		"Stealth 4" = "The virus mimics positive symptoms.",
	)

/datum/symptom/hallucigen/Start(datum/disease/advance/A)
	if(!..())
		return
	if(A.properties["stealth"] >= 4) //fake good symptom messages
		fake_healthy = TRUE
		base_message_chance = 50
	if(A.properties["stage_rate"] >= 7) //stronger hallucinations
		power = 2

/datum/symptom/hallucigen/Activate(datum/disease/advance/A)
	if(!..())
		return
	var/mob/living/carbon/M = A.affected_mob
	var/list/healthy_messages = list("Your lungs feel great.", "You realize you haven't been breathing.", "You don't feel the need to breathe.",\
					"Your eyes feel great.", "You are now blinking manually.", "You don't feel the need to blink.")
	switch(A.stage)
		if(1, 2)
			if(prob(base_message_chance))
				if(!fake_healthy)
					to_chat(M, "<span class='notice'>[pick("Something appears in your peripheral vision, then winks out.", "You hear a faint whisper with no source.", "Your head aches.")]</span>")
				else
					to_chat(M, "<span class='notice'>[pick(healthy_messages)]</span>")
		if(3, 4)
			if(prob(base_message_chance))
				if(!fake_healthy)
					to_chat(M, "<span class='danger'>[pick("Something is following you.", "You are being watched.", "You hear a whisper in your ear.", "Thumping footsteps slam toward you from nowhere.")]</span>")
				else
					to_chat(M, "<span class='notice'>[pick(healthy_messages)]</span>")
		else
			if(prob(base_message_chance))
				if(!fake_healthy)
					to_chat(M, "<span class='userdanger'>[pick("Oh, your head...", "Your head pounds.", "They're everywhere! Run!", "Something in the shadows...")]</span>")
				else
					to_chat(M, "<span class='notice'>[pick(healthy_messages)]</span>")
			M.hallucination += (45 * power)
