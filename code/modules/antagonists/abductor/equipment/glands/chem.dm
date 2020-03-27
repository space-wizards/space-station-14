/obj/item/organ/heart/gland/chem
	true_name = "intrinsic pharma-provider"
	cooldown_low = 50
	cooldown_high = 50
	uses = -1
	icon_state = "viral"
	mind_control_uses = 3
	mind_control_duration = 1200
	var/list/possible_reagents = list()

/obj/item/organ/heart/gland/chem/Initialize()
	. = ..()
	for(var/R in subtypesof(/datum/reagent/drug) + subtypesof(/datum/reagent/medicine) + typesof(/datum/reagent/toxin))
		possible_reagents += R

/obj/item/organ/heart/gland/chem/activate()
	var/chem_to_add = pick(possible_reagents)
	owner.reagents.add_reagent(chem_to_add, 2)
	owner.adjustToxLoss(-5, TRUE, TRUE)
	..()
