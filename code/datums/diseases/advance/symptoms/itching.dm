/*
//////////////////////////////////////

Itching

	Not noticable or unnoticable.
	Resistant.
	Increases stage speed.
	Little transmissibility.
	Low Level.

BONUS
	Displays an annoying message!
	Should be used for buffing your disease.

//////////////////////////////////////
*/

/datum/symptom/itching

	name = "Itching"
	desc = "The virus irritates the skin, causing itching."
	stealth = 0
	resistance = 3
	stage_speed = 3
	transmittable = 1
	level = 1
	severity = 1
	symptom_delay_min = 5
	symptom_delay_max = 25
	var/scratch = FALSE
	threshold_descs = list(
		"Transmission 6" = "Increases frequency of itching.",
		"Stage Speed 7" = "The host will scrath itself when itching, causing superficial damage.",
	)

/datum/symptom/itching/Start(datum/disease/advance/A)
	if(!..())
		return
	if(A.properties["transmittable"] >= 6) //itch more often
		symptom_delay_min = 1
		symptom_delay_max = 4
	if(A.properties["stage_rate"] >= 7) //scratch
		scratch = TRUE

/datum/symptom/itching/Activate(datum/disease/advance/A)
	if(!..())
		return
	var/mob/living/carbon/M = A.affected_mob
	var/picked_bodypart = pick(BODY_ZONE_HEAD, BODY_ZONE_CHEST, BODY_ZONE_R_ARM, BODY_ZONE_L_ARM, BODY_ZONE_R_LEG, BODY_ZONE_L_LEG)
	var/obj/item/bodypart/bodypart = M.get_bodypart(picked_bodypart)
	if(bodypart && bodypart.status == BODYPART_ORGANIC && !bodypart.is_pseudopart)	 //robotic limbs will mean less scratching overall (why are golems able to damage themselves with self-scratching, but not androids? the world may never know)
		var/can_scratch = scratch && !M.incapacitated()
		M.visible_message("[can_scratch ? "<span class='warning'>[M] scratches [M.p_their()] [bodypart.name].</span>" : ""]", "<span class='warning'>Your [bodypart.name] itches. [can_scratch ? " You scratch it." : ""]</span>")
		if(can_scratch)
			bodypart.receive_damage(0.5)
