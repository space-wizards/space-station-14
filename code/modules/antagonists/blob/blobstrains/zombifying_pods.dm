//kills sleeping targets and turns them into blob zombies, produces fragile spores when killed or on expanding
/datum/blobstrain/reagent/zombifying_pods
	name = "Zombifying Pods"
	description = "will do very low toxin damage and harvest sleeping targets for additional resources and a blob zombie."
	effectdesc = "will also produce fragile spores when killed and on expanding."
	shortdesc = "will do very low toxin damage and harvest sleeping targets for additional resources(for your overmind) and a blob zombie."
	analyzerdescdamage = "Does very low toxin damage and kills unconscious humans, turning them into blob zombies."
	analyzerdesceffect = "Produces spores when expanding and when killed."
	color = "#E88D5D"
	complementary_color = "#823ABB"
	message_living = ", and you feel tired"
	reagent = /datum/reagent/blob/zombifying_pods

/datum/blobstrain/reagent/zombifying_pods/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag)
	if((damage_flag == "melee" || damage_flag == "bullet" || damage_flag == "laser") && damage <= 20 && B.obj_integrity - damage <= 0 && prob(30)) //if the cause isn't fire or a bomb, the damage is less than 21, we're going to die from that damage, 20% chance of a shitty spore.
		B.visible_message("<span class='warning'><b>A spore floats free of the blob!</b></span>")
		var/mob/living/simple_animal/hostile/blob/blobspore/weak/BS = new/mob/living/simple_animal/hostile/blob/blobspore/weak(B.loc)
		BS.overmind = B.overmind
		BS.update_icons()
		B.overmind.blob_mobs.Add(BS)
	return ..()

/datum/blobstrain/reagent/zombifying_pods/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O)
	if(prob(10))
		var/mob/living/simple_animal/hostile/blob/blobspore/weak/BS = new/mob/living/simple_animal/hostile/blob/blobspore/weak(T)
		BS.overmind = B.overmind
		BS.update_icons()
		newB.overmind.blob_mobs.Add(BS)

/datum/reagent/blob/zombifying_pods
	name = "Zombifying Pods"
	color = "#E88D5D"

/datum/reagent/blob/zombifying_pods/reaction_mob(mob/living/M, method=TOUCH, reac_volume, show_message, touch_protection, mob/camera/blob/O)
	reac_volume = ..()
	M.apply_damage(0.6*reac_volume, TOX)
	if(O && ishuman(M) && M.stat == UNCONSCIOUS)
		M.death() //sleeping in a fight? bad plan.
		var/points = rand(5, 10)
		var/mob/living/simple_animal/hostile/blob/blobspore/BS = new/mob/living/simple_animal/hostile/blob/blobspore/weak(get_turf(M))
		BS.overmind = O
		BS.update_icons()
		O.blob_mobs.Add(BS)
		BS.Zombify(M)
		O.add_points(points)
		to_chat(O, "<span class='notice'>Gained [points] resources from the zombification of [M].</span>")
