/obj/item/organ/heart/gland/slime
	true_name = "gastric animation galvanizer"
	cooldown_low = 600
	cooldown_high = 1200
	uses = -1
	icon_state = "slime"
	mind_control_uses = 1
	mind_control_duration = 2400

/obj/item/organ/heart/gland/slime/Insert(mob/living/carbon/M, special = 0)
	..()
	owner.faction |= "slime"
	owner.grant_language(/datum/language/slime, TRUE, TRUE, LANGUAGE_GLAND)

/obj/item/organ/heart/gland/slime/Remove(mob/living/carbon/M, special = 0)
	..()
	owner.faction -= "slime"
	owner.remove_language(/datum/language/slime, TRUE, TRUE, LANGUAGE_GLAND)

/obj/item/organ/heart/gland/slime/activate()
	to_chat(owner, "<span class='warning'>You feel nauseated!</span>")
	owner.vomit(20)

	var/mob/living/simple_animal/slime/Slime = new(get_turf(owner), "grey")
	Slime.Friends = list(owner)
	Slime.Leader = owner
