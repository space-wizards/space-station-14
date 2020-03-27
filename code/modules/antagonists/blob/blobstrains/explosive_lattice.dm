//does aoe brute damage when hitting targets, is immune to explosions
/datum/blobstrain/reagent/explosive_lattice
	name = "Explosive Lattice"
	description = "will do brute damage in an area around targets."
	effectdesc = "will also resist explosions, but takes increased damage from fire and other energy sources."
	analyzerdescdamage = "Does medium brute damage and causes damage to everyone near its targets."
	analyzerdesceffect = "Is highly resistant to explosions, but takes increased damage from fire and other energy sources."
	color = "#8B2500"
	complementary_color = "#00668B"
	blobbernaut_message = "blasts"
	message = "The blob blasts you"
	reagent = /datum/reagent/blob/explosive_lattice

/datum/blobstrain/reagent/explosive_lattice/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if(damage_flag == "bomb")
		return 0
	else if(damage_flag != "melee" && damage_flag != "bullet" && damage_flag != "laser")
		return damage * 1.5
	return ..()

/datum/reagent/blob/explosive_lattice
	name = "Explosive Lattice"
	taste_description = "the bomb"
	color = "#8B2500"

/datum/reagent/blob/explosive_lattice/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	var/initial_volume = reac_volume
	reac_volume = ..()
	if(reac_volume >= 10) //if it's not a spore cloud, bad time incoming
		var/obj/effect/temp_visual/explosion/fast/E = new /obj/effect/temp_visual/explosion/fast(get_turf(M))
		E.alpha = 150
		for(var/mob/living/L in orange(get_turf(M), 1))
			if(ROLE_BLOB in L.faction) //no friendly fire
				continue
			var/aoe_volume = ..(L, TOUCH, initial_volume, 0, L.get_permeability_protection(), O)
			L.apply_damage(0.4*aoe_volume, BRUTE)
		if(M)
			M.apply_damage(0.6*reac_volume, BRUTE)
	else
		M.apply_damage(0.6*reac_volume, BRUTE)
