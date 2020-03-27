/datum/blobstrain/reagent/replicating_foam
	name = "Replicating Foam"
	description = "will do medium brute damage and occasionally expand again when expanding."
	shortdesc = "will do medium brute damage."
	effectdesc = "will also expand when attacked with burn damage, but takes more brute damage."
	color = "#7B5A57"
	complementary_color = "#57787B"
	analyzerdescdamage = "Does medium brute damage."
	analyzerdesceffect = "Expands when attacked with burn damage, will occasionally expand again when expanding, and is fragile to brute damage."
	reagent = /datum/reagent/blob/replicating_foam


/datum/blobstrain/reagent/replicating_foam/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if(damage_type == BRUTE)
		damage = damage * 2
	else if(damage_type == BURN && damage > 0 && B.obj_integrity - damage > 0 && prob(60))
		var/obj/structure/blob/newB = B.expand(null, null, 0)
		if(newB)
			newB.obj_integrity = B.obj_integrity - damage
			newB.update_icon()
	return ..()


/datum/blobstrain/reagent/replicating_foam/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O)
	if(prob(30))
		newB.expand(null, null, 0) //do it again!

/datum/reagent/blob/replicating_foam
	name = "Replicating Foam"
	taste_description = "duplication"
	color = "#7B5A57"

/datum/reagent/blob/replicating_foam/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.apply_damage(0.7*reac_volume, BRUTE)
