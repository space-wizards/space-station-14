/obj/item/seeds/onion
	name = "pack of onion seeds"
	desc = "These seeds grow into onions."
	icon_state = "seed-onion"
	species = "onion"
	plantname = "Onion Sprouts"
	product = /obj/item/reagent_containers/food/snacks/grown/onion
	lifespan = 20
	maturation = 3
	production = 4
	yield = 6
	endurance = 25
	growthstages = 3
	weed_chance = 3
	growing_icon = 'icons/obj/hydroponics/growing_vegetables.dmi'
	reagents_add = list(/datum/reagent/consumable/nutriment/vitamin = 0.04, /datum/reagent/consumable/nutriment = 0.1)
	mutatelist = list(/obj/item/seeds/onion/red)

/obj/item/reagent_containers/food/snacks/grown/onion
	seed = /obj/item/seeds/onion
	name = "onion"
	desc = "Nothing to cry over."
	icon_state = "onion"
	filling_color = "#C0C9A0"
	bitesize_mod = 2
	tastes = list("onions" = 1)
	slice_path = /obj/item/reagent_containers/food/snacks/onion_slice
	slices_num = 2
	wine_power = 30

/obj/item/seeds/onion/red
	name = "pack of red onion seeds"
	desc = "For growing exceptionally potent onions."
	icon_state = "seed-onionred"
	species = "onion_red"
	plantname = "Red Onion Sprouts"
	weed_chance = 1
	product = /obj/item/reagent_containers/food/snacks/grown/onion/red
	reagents_add = list(/datum/reagent/consumable/nutriment/vitamin = 0.04, /datum/reagent/consumable/nutriment = 0.1, /datum/reagent/consumable/tearjuice = 0.05)

/obj/item/reagent_containers/food/snacks/grown/onion/red
	seed = /obj/item/seeds/onion/red
	name = "red onion"
	desc = "Purple despite the name."
	icon_state = "onion_red"
	filling_color = "#C29ACF"
	slice_path = /obj/item/reagent_containers/food/snacks/onion_slice/red
	wine_power = 60

/obj/item/reagent_containers/food/snacks/grown/onion/slice(accuracy, obj/item/W, mob/user)
	var/datum/effect_system/smoke_spread/chem/S = new	//Since the onion is destroyed when it's sliced,
	var/splat_location = get_turf(src)	//we need to set up the smoke beforehand
	S.attach(splat_location)
	S.set_up(reagents, 0, splat_location, 0)
	if(..())
		S.start()
		return TRUE
	qdel(S)

/obj/item/reagent_containers/food/snacks/onion_slice
	name = "onion slices"
	desc = "Rings, not for wearing."
	icon_state = "onionslice"
	list_reagents = list(/datum/reagent/consumable/nutriment = 5, /datum/reagent/consumable/nutriment/vitamin = 2)
	filling_color = "#C0C9A0"
	gender = PLURAL
	cooked_type = /obj/item/reagent_containers/food/snacks/onionrings

/obj/item/reagent_containers/food/snacks/onion_slice/red
	name = "red onion slices"
	desc = "They shine like exceptionally low quality amethyst."
	icon_state = "onionslice_red"
	filling_color = "#C29ACF"
	list_reagents = list(/datum/reagent/consumable/nutriment = 5, /datum/reagent/consumable/nutriment/vitamin = 2, /datum/reagent/consumable/tearjuice = 2.5)
