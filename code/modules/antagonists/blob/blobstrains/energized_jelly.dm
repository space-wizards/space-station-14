//does tons of oxygen damage and a little stamina, immune to tesla bolts, weak to EMP
/datum/blobstrain/reagent/energized_jelly
	name = "Energized Jelly"
	description = "will cause low stamina and high oxygen damage, and cause targets to be unable to breathe."
	effectdesc = "will also conduct electricity, but takes damage from EMPs."
	analyzerdescdamage = "Does low stamina damage, high oxygen damage, and prevents targets from breathing."
	analyzerdesceffect = "Is immune to electricity and will easily conduct it, but is weak to EMPs."
	color = "#EFD65A"
	complementary_color = "#00E5B1"
	reagent = /datum/reagent/blob/energized_jelly

/datum/blobstrain/reagent/energized_jelly/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if((damage_flag == "melee" || damage_flag == "bullet" || damage_flag == "laser") && B.obj_integrity - damage <= 0 && prob(10))
		do_sparks(rand(2, 4), FALSE, B)
	return ..()

/datum/blobstrain/reagent/energized_jelly/tesla_reaction(obj/structure/blob/B, power)
	return 0

/datum/blobstrain/reagent/energized_jelly/emp_reaction(obj/structure/blob/B, severity)
	var/damage = rand(30, 50) - severity * rand(10, 15)
	B.take_damage(damage, BURN, "energy")

/datum/reagent/blob/energized_jelly
	name = "Energized Jelly"
	taste_description = "gelatin"
	color = "#EFD65A"

/datum/reagent/blob/energized_jelly/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.losebreath += round(0.2*reac_volume)
	M.adjustStaminaLoss(reac_volume)
	if(M)
		M.apply_damage(0.6*reac_volume, OXY)
