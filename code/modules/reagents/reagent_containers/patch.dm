/obj/item/reagent_containers/pill/patch
	name = "chemical patch"
	desc = "A chemical patch for touch based applications."
	icon = 'icons/obj/chemical.dmi'
	icon_state = "bandaid"
	item_state = "bandaid"
	possible_transfer_amounts = list()
	volume = 40
	apply_type = PATCH
	apply_method = "apply"
	self_delay = 30		// three seconds
	dissolvable = FALSE

/obj/item/reagent_containers/pill/patch/attack(mob/living/L, mob/user)
	if(ishuman(L))
		var/obj/item/bodypart/affecting = L.get_bodypart(check_zone(user.zone_selected))
		if(!affecting)
			to_chat(user, "<span class='warning'>The limb is missing!</span>")
			return
		if(affecting.status != BODYPART_ORGANIC)
			to_chat(user, "<span class='notice'>Medicine won't work on a robotic limb!</span>")
			return
	..()

/obj/item/reagent_containers/pill/patch/canconsume(mob/eater, mob/user)
	if(!iscarbon(eater))
		return 0
	return 1 // Masks were stopping people from "eating" patches. Thanks, inheritance.

/obj/item/reagent_containers/pill/patch/libital
	name = "libital patch (brute)"
	desc = "A pain reliever. Does minor liver damage. Diluted with Granibitaluri."
	list_reagents = list(/datum/reagent/medicine/C2/libital = 2, /datum/reagent/medicine/granibitaluri = 8) //10 iterations
	icon_state = "bandaid_brute"

/obj/item/reagent_containers/pill/patch/aiuri
	name = "aiuri patch (burn)"
	desc = "Helps with burn injuries. Does minor eye damage. Diluted with Granibitaluri."
	list_reagents = list(/datum/reagent/medicine/C2/aiuri = 1, /datum/reagent/medicine/granibitaluri = 9)
	icon_state = "bandaid_burn"

/obj/item/reagent_containers/pill/patch/instabitaluri
	name = "instabitaluri patch"
	desc = "Helps with brute and burn injuries. Slightly toxic."
	list_reagents = list(/datum/reagent/medicine/C2/instabitaluri = 20)
	icon_state = "bandaid_both"
