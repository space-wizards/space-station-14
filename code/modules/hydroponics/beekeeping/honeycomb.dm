
/obj/item/reagent_containers/honeycomb
	name = "honeycomb"
	desc = "A hexagonal mesh of honeycomb."
	icon = 'icons/obj/hydroponics/harvest.dmi'
	icon_state = "honeycomb"
	possible_transfer_amounts = list()
	spillable = FALSE
	disease_amount = 0
	volume = 10
	amount_per_transfer_from_this = 0
	list_reagents = list(/datum/reagent/consumable/honey = 5)
	grind_results = list()
	var/honey_color = ""

/obj/item/reagent_containers/honeycomb/Initialize()
	. = ..()
	pixel_x = rand(8,-8)
	pixel_y = rand(8,-8)
	update_icon()


/obj/item/reagent_containers/honeycomb/update_overlays()
	. = ..()
	var/mutable_appearance/honey_overlay = mutable_appearance(icon, "honey")
	if(honey_color)
		honey_overlay.icon_state = "greyscale_honey"
		honey_overlay.color = honey_color
	. += honey_overlay


/obj/item/reagent_containers/honeycomb/proc/set_reagent(reagent)
	var/datum/reagent/R = GLOB.chemical_reagents_list[reagent]
	if(istype(R))
		name = "honeycomb ([R.name])"
		honey_color = R.color
		reagents.add_reagent(R.type,5)
	else
		honey_color = ""
	update_icon()
