/*
//////////////////////////////////////
Eternal Youth

	Moderate stealth boost.
	Increases resistance tremendously.
	Increases stage speed tremendously.
	Reduces transmission tremendously.
	Critical Level.

BONUS
	Gives you immortality and eternal youth!!!
	Can be used to buff your virus

//////////////////////////////////////
*/

/datum/symptom/youth

	name = "Eternal Youth"
	desc = "The virus becomes symbiotically connected to the cells in the host's body, preventing and reversing aging. \
	The virus, in turn, becomes more resistant, spreads faster, and is harder to spot, although it doesn't thrive as well without a host."
	stealth = 3
	resistance = 4
	stage_speed = 4
	transmittable = -4
	level = 5
	base_message_chance = 100
	symptom_delay_min = 25
	symptom_delay_max = 50

/datum/symptom/youth/Activate(datum/disease/advance/A)
	if(!..())
		return
	var/mob/living/M = A.affected_mob
	if(ishuman(M))
		var/mob/living/carbon/human/H = M
		switch(A.stage)
			if(1)
				if(H.age > 41)
					H.age = 41
					to_chat(H, "<span class='notice'>You haven't had this much energy in years!</span>")
			if(2)
				if(H.age > 36)
					H.age = 36
					to_chat(H, "<span class='notice'>You're suddenly in a good mood.</span>")
			if(3)
				if(H.age > 31)
					H.age = 31
					to_chat(H, "<span class='notice'>You begin to feel more lithe.</span>")
			if(4)
				if(H.age > 26)
					H.age = 26
					to_chat(H, "<span class='notice'>You feel reinvigorated.</span>")
			if(5)
				if(H.age > 21)
					H.age = 21
					to_chat(H, "<span class='notice'>You feel like you can take on the world!</span>")
