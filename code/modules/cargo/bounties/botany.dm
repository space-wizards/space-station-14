/datum/bounty/item/botany
	reward = 5000
	var/datum/bounty/item/botany/multiplier = 0 //adds bonus reward money; increased for higher tier or rare mutations
	var/datum/bounty/item/botany/bonus_desc //for adding extra flavor text to bounty descriptions
	var/datum/bounty/item/botany/foodtype = "meal" //same here

/datum/bounty/item/botany/New()
	..()
	description = "Central Command's head chef is looking to prepare a fine [foodtype] with [name]. [bonus_desc]"
	reward += multiplier * 1000
	required_count = rand(5, 10)

/datum/bounty/item/botany/ambrosia_vulgaris
	name = "Ambrosia Vulgaris Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/ambrosia/vulgaris)
	foodtype = "stew"

/datum/bounty/item/botany/ambrosia_gaia
	name = "Ambrosia Gaia Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/ambrosia/gaia)
	multiplier = 4
	foodtype = "stew"

/datum/bounty/item/botany/apple_golden
	name = "Golden Apples"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/apple/gold)
	multiplier = 4
	foodtype = "dessert"

/datum/bounty/item/botany/banana
	name = "Bananas"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/banana)
	exclude_types = list(/obj/item/reagent_containers/food/snacks/grown/banana/bluespace)
	foodtype = "banana split"

/datum/bounty/item/botany/banana_bluespace
	name = "Bluespace Bananas"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/banana/bluespace)
	multiplier = 2
	foodtype = "banana split"

/datum/bounty/item/botany/beans_koi
	name = "Koi Beans"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/koibeans)
	multiplier = 2

/datum/bounty/item/botany/berries_death
	name = "Death Berries"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/berries/death)
	multiplier = 2
	bonus_desc = "He insists that \"he knows what he's doing\"."
	foodtype = "sorbet"

/datum/bounty/item/botany/berries_glow
	name = "Glow-Berries"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/berries/glow)
	multiplier = 2
	foodtype = "sorbet"

/datum/bounty/item/botany/cannabis
	name = "Cannabis Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/cannabis)
	exclude_types = list(/obj/item/reagent_containers/food/snacks/grown/cannabis/white, /obj/item/reagent_containers/food/snacks/grown/cannabis/death, /obj/item/reagent_containers/food/snacks/grown/cannabis/ultimate)
	multiplier = 4 //hush money
	bonus_desc = "Do not mention this shipment to security."
	foodtype = "batch of \"muffins\""
	
/datum/bounty/item/botany/cannabis_white
	name = "Lifeweed Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/cannabis/white)
	multiplier = 6
	bonus_desc = "Do not mention this shipment to security."
	foodtype = "\"meal\""

/datum/bounty/item/botany/cannabis_death
	name = "Deathweed Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/cannabis/death)
	multiplier = 6
	bonus_desc = "Do not mention this shipment to security."
	foodtype = "\"meal\""

/datum/bounty/item/botany/cannabis_ultimate
	name = "Omega Weed Leaves"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/cannabis/ultimate)
	multiplier = 6
	bonus_desc = "Under no circumstances mention this shipment to security."
	foodtype = "batch of \"brownies\""

/datum/bounty/item/botany/wheat
	name = "Wheat Grains"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/wheat)

/datum/bounty/item/botany/rice
	name = "Rice Grains"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/rice)

/datum/bounty/item/botany/chili
	name = "Chili Peppers"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/chili)

/datum/bounty/item/botany/ice_chili
	name = "Chilly Peppers"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/icepepper)
	multiplier = 2

/datum/bounty/item/botany/ghost_chili
	name = "Ghost Chili Peppers"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/ghost_chili)
	multiplier = 2

/datum/bounty/item/botany/citrus_lime
	name = "Limes"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/citrus/lime)
	foodtype = "sorbet"

/datum/bounty/item/botany/citrus_lemon
	name = "Lemons"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/citrus/lemon)
	foodtype = "sorbet"

/datum/bounty/item/botany/citrus_oranges
	name = "Oranges"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/citrus/orange)
	bonus_desc = "Do not ship lemons or limes." //I vanted orahnge!
	foodtype = "sorbet"

/datum/bounty/item/botany/eggplant
	name = "Eggplants"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/eggplant)
	bonus_desc = "Not to be confused with egg-plants."

/datum/bounty/item/botany/eggplant_eggy
	name = "Egg-plants"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/shell/eggy)
	bonus_desc = "Not to be confused with eggplants."
	multiplier = 2

/datum/bounty/item/botany/kudzu
	name = "Kudzu Pods"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/kudzupod)
	bonus_desc = "Store in a dry, dark place."
	multiplier = 4

/datum/bounty/item/botany/watermelon
	name = "Watermelons"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/watermelon)
	foodtype = "dessert"

/datum/bounty/item/botany/watermelon_holy
	name = "Holy Melons"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/holymelon)
	multiplier = 2
	foodtype = "dessert"

/datum/bounty/item/botany/glowshroom
	name = "Glowshrooms"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/mushroom/glowshroom)
	exclude_types = list(/obj/item/reagent_containers/food/snacks/grown/mushroom/glowshroom/glowcap, /obj/item/reagent_containers/food/snacks/grown/mushroom/glowshroom/shadowshroom)
	foodtype = "omelet"

/datum/bounty/item/botany/glowshroom_cap
	name = "Glowcaps"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/mushroom/glowshroom/glowcap)
	multiplier = 2
	foodtype = "omelet"

/datum/bounty/item/botany/glowshroom_shadow
	name = "Shadowshrooms"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/mushroom/glowshroom/shadowshroom)
	multiplier = 2
	foodtype = "omelet"

/datum/bounty/item/botany/nettles_death
	name = "Death Nettles"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/nettle/death)
	multiplier = 2
	bonus_desc = "Wear protection when handling them."
	foodtype = "cheese"

/datum/bounty/item/botany/pineapples
	name = "Pineapples"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/pineapple)
	bonus_desc = "Not for human consumption."
	foodtype = "ashtray"

/datum/bounty/item/botany/tomato
	name = "Tomatoes"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/tomato)
	exclude_types = list(/obj/item/reagent_containers/food/snacks/grown/tomato/blue)

/datum/bounty/item/botany/tomato_bluespace
	name = "Bluespace Tomatoes"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/tomato/blue/bluespace)
	multiplier = 4

/datum/bounty/item/botany/oatz
	name = "Oats"
	wanted_types = list(/obj/item/reagent_containers/food/snacks/grown/oat)
	multiplier = 2
	foodtype = "batch of oatmeal"
	bonus_desc = "Squats and oats. We're all out of oats."
