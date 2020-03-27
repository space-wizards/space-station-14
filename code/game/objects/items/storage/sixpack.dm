/obj/item/storage/cans
	name = "can ring"
	desc = "Holds up to six drink cans, and select bottles."
	icon = 'icons/obj/storage.dmi'
	icon_state = "canholder"
	item_state = "cola"
	lefthand_file = 'icons/mob/inhands/misc/food_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/food_righthand.dmi'
	custom_materials = list(/datum/material/plastic = 1200)
	max_integrity = 500

/obj/item/storage/cans/suicide_act(mob/living/carbon/user)
	user.visible_message("<span class='suicide'>[user] begins popping open a final cold one with the boys! It looks like [user.p_theyre()] trying to commit suicide!</span>")
	return BRUTELOSS

/obj/item/storage/cans/update_icon_state()
	icon_state = "[initial(icon_state)][contents.len]"

/obj/item/storage/cans/Initialize()
	. = ..()
	update_icon()

/obj/item/storage/cans/ComponentInitialize()
	. = ..()
	var/datum/component/storage/STR = GetComponent(/datum/component/storage)
	STR.max_w_class = WEIGHT_CLASS_SMALL
	STR.max_combined_w_class = 12
	STR.max_items = 6
	STR.set_holdable(list(
		/obj/item/reagent_containers/food/drinks/soda_cans,
		/obj/item/reagent_containers/food/drinks/beer,
		/obj/item/reagent_containers/food/drinks/ale,
		/obj/item/reagent_containers/food/drinks/waterbottle
		))

/obj/item/storage/cans/sixsoda
	name = "soda bottle ring"
	desc = "Holds six soda cans. Remember to recycle when you're done!"

/obj/item/storage/cans/sixsoda/PopulateContents()
	for(var/i in 1 to 6)
		new /obj/item/reagent_containers/food/drinks/soda_cans/cola(src)

/obj/item/storage/cans/sixbeer
	name = "beer bottle ring"
	desc = "Holds six beer bottles. Remember to recycle when you're done!"

/obj/item/storage/cans/sixbeer/PopulateContents()
	for(var/i in 1 to 6)
		new /obj/item/reagent_containers/food/drinks/beer(src)
