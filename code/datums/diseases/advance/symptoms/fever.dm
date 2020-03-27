/*
//////////////////////////////////////

Fever

	No change to hidden.
	Increases resistance.
	Increases stage speed.
	Little transmittable.
	Low level.

Bonus
	Heats up your body.

//////////////////////////////////////
*/

/datum/symptom/fever
	name = "Fever"
	desc = "The virus causes a febrile response from the host, raising its body temperature."
	stealth = 0
	resistance = 3
	stage_speed = 3
	transmittable = 2
	level = 2
	severity = 2
	base_message_chance = 20
	symptom_delay_min = 10
	symptom_delay_max = 30
	var/unsafe = FALSE //over the heat threshold
	threshold_descs = list(
		"Resistance 5" = "Increases fever intensity, fever can overheat and harm the host.",
		"Resistance 10" = "Further increases fever intensity.",
	)

/datum/symptom/fever/Start(datum/disease/advance/A)
	if(!..())
		return
	if(A.properties["resistance"] >= 5) //dangerous fever
		power = 1.5
		unsafe = TRUE
	if(A.properties["resistance"] >= 10)
		power = 2.5

/datum/symptom/fever/Activate(datum/disease/advance/A)
	if(!..())
		return
	var/mob/living/carbon/M = A.affected_mob
	if(!unsafe || A.stage < 4)
		to_chat(M, "<span class='warning'>[pick("You feel hot.", "You feel like you're burning.")]</span>")
	else
		to_chat(M, "<span class='userdanger'>[pick("You feel too hot.", "You feel like your blood is boiling.")]</span>")
	if(M.bodytemperature < BODYTEMP_HEAT_DAMAGE_LIMIT || unsafe)
		Heat(M, A)

/datum/symptom/fever/proc/Heat(mob/living/M, datum/disease/advance/A)
	var/get_heat = 6 * power
	if(!unsafe)
		M.adjust_bodytemperature(get_heat * A.stage, 0, BODYTEMP_HEAT_DAMAGE_LIMIT - 1)
	else
		M.adjust_bodytemperature(get_heat * A.stage)
	return 1
