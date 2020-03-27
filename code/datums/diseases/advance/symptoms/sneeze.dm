/*
//////////////////////////////////////

Sneezing

	Very Noticable.
	Increases resistance.
	Doesn't increase stage speed.
	Very transmissible.
	Low Level.

Bonus
	Forces a spread type of AIRBORNE
	with extra range!

//////////////////////////////////////
*/

/datum/symptom/sneeze
	name = "Sneezing"
	desc = "The virus causes irritation of the nasal cavity, making the host sneeze occasionally. Sneezes from this symptom will spread the virus in a 4 meter cone in front of the host."
	stealth = -2
	resistance = 3
	stage_speed = 0
	transmittable = 4
	level = 1
	severity = 1
	symptom_delay_min = 5
	symptom_delay_max = 35
	var/spread_range = 4
	var/cartoon_sneezing = FALSE //ah, ah, AH, AH-CHOO!!
	threshold_descs = list(
		"Transmission 9" = "Increases sneezing range, spreading the virus over 6 meter cone instead of over a 4 meter cone.",
		"Stealth 4" = "The symptom remains hidden until active.",
		"Stage Speed 17" = "The force of each sneeze catapults the host backwards, potentially stunning and lightly damaging them if they hit a wall or another person mid-flight."
	)

/datum/symptom/sneeze/Start(datum/disease/advance/A)
	if(!..())
		return
	if(A.properties["transmittable"] >= 9) //longer spread range
		spread_range = 6
	if(A.properties["stealth"] >= 4)
		suppress_warning = TRUE
	if(A.properties["stage_rate"] >= 17) //Yep, stage speed 17, not stage speed 7. This is a big boy threshold (effect), like the language-scrambling transmission one for the voice change symptom.
		cartoon_sneezing = TRUE //for a really fun time, distribute a disease with this threshold met while the gravity generator is down

/datum/symptom/sneeze/Activate(datum/disease/advance/A)
	if(!..())
		return
	var/mob/living/M = A.affected_mob
	switch(A.stage)
		if(1, 2, 3)
			if(!suppress_warning)
				M.emote("sniff")
		else
			M.emote("sneeze")
			if(M.CanSpreadAirborneDisease()) //don't spread germs if they covered their mouth
				for(var/mob/living/L in oview(spread_range, M))
					if(is_A_facing_B(M, L) && disease_air_spread_walk(get_turf(M), get_turf(L)))
						L.AirborneContractDisease(A, TRUE)
			if(cartoon_sneezing) //Yeah, this can fling you around even if you have a space suit helmet on. It's, uh, bluespace snot, yeah.
				var/sneeze_distance = rand(2,4) //twice as far as a normal baseball bat strike will fling you
				var/turf/target = get_ranged_target_turf(M, turn(M.dir, 180), sneeze_distance)
				M.throw_at(target, sneeze_distance, 7) //flings you at the speed that a normal baseball bat would fling you at
