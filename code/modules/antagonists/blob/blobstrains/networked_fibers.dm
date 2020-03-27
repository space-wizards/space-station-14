//does massive brute and burn damage, but can only expand manually
/datum/blobstrain/reagent/networked_fibers
	name = "Networked Fibers"
	description = "will do high brute and burn damage and will generate resources quicker, but can only expand manually."
	shortdesc = "will do high brute and burn damage."
	effectdesc = "will move your core when manually expanding near it."
	analyzerdescdamage = "Does high brute and burn damage."
	analyzerdesceffect = "Is highly mobile and generates resources rapidly."
	color = "#4F4441"
	complementary_color = "#414C4F"
	reagent = /datum/reagent/blob/networked_fibers

/datum/blobstrain/reagent/networked_fibers/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O)
	if(!O && newB.overmind)
		if(!istype(B, /obj/structure/blob/node))
			newB.overmind.add_points(1)
			qdel(newB)
	else
		var/area/A = get_area(T)
		if(!isspaceturf(T) && !istype(A, /area/shuttle))
			for(var/obj/structure/blob/core/C in range(1, newB))
				if(C.overmind == O)
					newB.forceMove(get_turf(C))
					C.forceMove(T)
					C.setDir(get_dir(newB, C))
					O.add_points(1)

//does massive brute and burn damage, but can only expand manually
/datum/reagent/blob/networked_fibers
	name = "Networked Fibers"
	taste_description = "efficiency"
	color = "#4F4441"

/datum/reagent/blob/networked_fibers/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.apply_damage(0.6*reac_volume, BRUTE)
	if(M)
		M.apply_damage(0.6*reac_volume, BURN)
