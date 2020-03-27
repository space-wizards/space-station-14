/obj/machinery/vending/dinnerware
	name = "\improper Plasteel Chef's Dinnerware Vendor"
	desc = "A kitchen and restaurant equipment vendor."
	product_ads = "Mm, food stuffs!;Food and food accessories.;Get your plates!;You like forks?;I like forks.;Woo, utensils.;You don't really need these..."
	icon_state = "dinnerware"
	products = list(/obj/item/storage/bag/tray = 8,
					/obj/item/reagent_containers/glass/bowl = 20,
					/obj/item/kitchen/fork = 6,
					/obj/item/reagent_containers/food/drinks/drinkingglass = 8,
					/obj/item/reagent_containers/food/condiment/pack/ketchup = 5,
					/obj/item/reagent_containers/food/condiment/pack/hotsauce = 5,
					/obj/item/reagent_containers/food/condiment/pack/astrotame = 5,
					/obj/item/reagent_containers/food/condiment/saltshaker = 5,
					/obj/item/reagent_containers/food/condiment/peppermill = 5,
					/obj/item/clothing/suit/apron/chef = 2,
					/obj/item/kitchen/rollingpin = 2,
					/obj/item/kitchen/knife = 2,
					/obj/item/book/granter/crafting_recipe/cooking_sweets_101 = 2)
	contraband = list(/obj/item/kitchen/rollingpin = 2,
					  /obj/item/kitchen/knife/butcher = 2)
	refill_canister = /obj/item/vending_refill/dinnerware
	default_price = 50
	extra_price = 250
	payment_department = ACCOUNT_SRV

/obj/item/vending_refill/dinnerware
	machine_name = "Plasteel Chef's Dinnerware Vendor"
	icon_state = "refill_smoke"
