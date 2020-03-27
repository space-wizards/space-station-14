//does brute damage, bonus damage for each nearby blob, and spreads damage out
/datum/blobstrain/reagent/synchronous_mesh
	name = "Synchronous Mesh"
	description = "will do massively increased brute damage for each blob near the target."
	effectdesc = "will also spread damage between each blob near the attacked blob."
	analyzerdescdamage = "Does brute damage, increasing for each blob near the target."
	analyzerdesceffect = "When attacked, spreads damage between all blobs near the attacked blob."
	color = "#65ADA2"
	complementary_color = "#AD6570"
	blobbernaut_message = "synchronously strikes"
	message = "The blobs strike you"
	reagent = /datum/reagent/blob/synchronous_mesh

/datum/blobstrain/reagent/synchronous_mesh/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if(damage_flag == "melee" || damage_flag == "bullet" || damage_flag == "laser") //the cause isn't fire or bombs, so split the damage
		var/damagesplit = 1 //maximum split is 9, reducing the damage each blob takes to 11% but doing that damage to 9 blobs
		for(var/obj/structure/blob/C in orange(1, B))
			if(!istype(C, /obj/structure/blob/core) && !istype(C, /obj/structure/blob/node) && C.overmind && C.overmind.blobstrain.type == B.overmind.blobstrain.type) //if it doesn't have the same chemical or is a core or node, don't split damage to it
				damagesplit += 1
		for(var/obj/structure/blob/C in orange(1, B))
			if(!istype(C, /obj/structure/blob/core) && !istype(C, /obj/structure/blob/node) && C.overmind && C.overmind.blobstrain.type == B.overmind.blobstrain.type) //only hurt blobs that have the same overmind chemical and aren't cores or nodes
				C.take_damage(damage/damagesplit, CLONE, 0, 0)
		return damage / damagesplit
	else
		return damage * 1.25

/datum/reagent/blob/synchronous_mesh
	name = "Synchronous Mesh"
	taste_description = "toxic mold"
	color = "#65ADA2"

/datum/reagent/blob/synchronous_mesh/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.apply_damage(0.2*reac_volume, BRUTE)
	if(M && reac_volume)
		for(var/obj/structure/blob/B in range(1, M)) //if the target is completely surrounded, this is 2.4*reac_volume bonus damage, total of 2.6*reac_volume
			if(M)
				B.blob_attack_animation(M) //show them they're getting a bad time
				M.apply_damage(0.3*reac_volume, BRUTE)
