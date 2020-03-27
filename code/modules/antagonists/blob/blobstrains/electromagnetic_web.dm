//does burn damage and EMPs, slightly fragile
/datum/blobstrain/reagent/electromagnetic_web
	name = "Electromagnetic Web"
	color = "#83ECEC"
	complementary_color = "#EC8383"
	description = "will do high burn damage and EMP targets."
	effectdesc = "will also take massively increased damage and release an EMP when killed."
	analyzerdescdamage = "Does low burn damage and EMPs targets."
	analyzerdesceffect = "Is fragile to all types of damage, but takes massive damage from brute. In addition, releases a small EMP when killed."
	reagent = /datum/reagent/blob/electromagnetic_web

/datum/blobstrain/reagent/electromagnetic_web/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if(damage_type == BRUTE) //take full brute
		switch(B.brute_resist)
			if(0.5)
				return damage * 2
			if(0.25)
				return damage * 4
			if(0.1)
				return damage * 10
	return damage * 1.25 //a laser will do 25 damage, which will kill any normal blob

/datum/blobstrain/reagent/electromagnetic_web/death_reaction(obj/structure/blob/B, damage_flag)
	if(damage_flag == "melee" || damage_flag == "bullet" || damage_flag == "laser")
		empulse(B.loc, 1, 3) //less than screen range, so you can stand out of range to avoid it

/datum/reagent/blob/electromagnetic_web
	name = "Electromagnetic Web"
	taste_description = "pop rocks"
	color = "#83ECEC"

/datum/reagent/blob/electromagnetic_web/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	if(prob(reac_volume*2))
		M.emp_act(EMP_LIGHT)
	if(M)
		M.apply_damage(reac_volume, BURN)
