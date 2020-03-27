/*
//////////////////////////////////////
Alopecia

	Not Noticeable.
	Increases resistance slightly.
	Increases stage speed.
	Transmittable.
	Intense Level.

BONUS
	Makes the mob lose hair.

//////////////////////////////////////
*/

/datum/symptom/shedding
	name = "Alopecia"
	desc = "The virus causes rapid shedding of head and body hair."
	stealth = 0
	resistance = 1
	stage_speed = 2
	transmittable = 2
	level = 4
	severity = 1
	base_message_chance = 50
	symptom_delay_min = 45
	symptom_delay_max = 90

/datum/symptom/shedding/Activate(datum/disease/advance/A)
	if(!..())
		return

	var/mob/living/M = A.affected_mob
	if(prob(base_message_chance))
		to_chat(M, "<span class='warning'>[pick("Your scalp itches.", "Your skin feels flaky.")]</span>")
	if(ishuman(M))
		var/mob/living/carbon/human/H = M
		switch(A.stage)
			if(3, 4)
				if(!(H.hairstyle == "Bald") && !(H.hairstyle == "Balding Hair"))
					to_chat(H, "<span class='warning'>Your hair starts to fall out in clumps...</span>")
					addtimer(CALLBACK(src, .proc/Shed, H, FALSE), 50)
			if(5)
				if(!(H.facial_hairstyle == "Shaved") || !(H.hairstyle == "Bald"))
					to_chat(H, "<span class='warning'>Your hair starts to fall out in clumps...</span>")
					addtimer(CALLBACK(src, .proc/Shed, H, TRUE), 50)

/datum/symptom/shedding/proc/Shed(mob/living/carbon/human/H, fullbald)
	if(fullbald)
		H.facial_hairstyle = "Shaved"
		H.hairstyle = "Bald"
	else
		H.hairstyle = "Balding Hair"
	H.update_hair()
