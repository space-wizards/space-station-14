//does brute damage, shifts away when damaged
/datum/blobstrain/reagent/shifting_fragments
	name = "Shifting Fragments"
	description = "will do medium brute damage."
	effectdesc = "will also cause blob parts to shift away when attacked."
	analyzerdescdamage = "Does medium brute damage."
	analyzerdesceffect = "When attacked, may shift away from the attacker."
	color = "#C8963C"
	complementary_color = "#3C6EC8"
	reagent = /datum/reagent/blob/shifting_fragments

/datum/blobstrain/reagent/shifting_fragments/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O)
	if(istype(B, /obj/structure/blob/normal) || (istype(B, /obj/structure/blob/shield) && prob(25)))
		newB.forceMove(get_turf(B))
		B.forceMove(T)

/datum/blobstrain/reagent/shifting_fragments/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if((damage_flag == "melee" || damage_flag == "bullet" || damage_flag == "laser") && damage > 0 && B.obj_integrity - damage > 0 && prob(60-damage))
		var/list/blobstopick = list()
		for(var/obj/structure/blob/OB in orange(1, B))
			if((istype(OB, /obj/structure/blob/normal) || (istype(OB, /obj/structure/blob/shield) && prob(25))) && OB.overmind && OB.overmind.blobstrain.type == B.overmind.blobstrain.type)
				blobstopick += OB //as long as the blob picked is valid; ie, a normal or shield blob that has the same chemical as we do, we can swap with it
		if(blobstopick.len)
			var/obj/structure/blob/targeted = pick(blobstopick) //randomize the blob chosen, because otherwise it'd tend to the lower left
			var/turf/T = get_turf(targeted)
			targeted.forceMove(get_turf(B))
			B.forceMove(T) //swap the blobs
	return ..()

/datum/reagent/blob/shifting_fragments
	name = "Shifting Fragments"
	color = "#C8963C"

/datum/reagent/blob/shifting_fragments/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.apply_damage(0.7*reac_volume, BRUTE)
