/obj/item/organ/heart/gland/blood
	true_name = "pseudonuclear hemo-destabilizer"
	cooldown_low = 1200
	cooldown_high = 1800
	uses = -1
	icon_state = "egg"
	lefthand_file = 'icons/mob/inhands/misc/food_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/food_righthand.dmi'
	mind_control_uses = 3
	mind_control_duration = 1500

/obj/item/organ/heart/gland/blood/activate()
	if(!ishuman(owner) || !owner.dna.species)
		return
	var/mob/living/carbon/human/H = owner
	var/datum/species/species = H.dna.species
	to_chat(H, "<span class='warning'>You feel your blood heat up for a moment.</span>")
	species.exotic_blood = get_random_reagent_id()
