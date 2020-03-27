/datum/bounty/item/science/boh
	name = "Bag of Holding"
	description = "Nanotrasen would make good use of high-capacity backpacks. If you have any, please ship them."
	reward = 10000
	wanted_types = list(/obj/item/storage/backpack/holding)

/datum/bounty/item/science/tboh
	name = "Trash Bag of Holding"
	description = "Nanotrasen would make good use of high-capacity trash bags. If you have any, please ship them."
	reward = 10000
	wanted_types = list(/obj/item/storage/backpack/holding)

/datum/bounty/item/science/bluespace_syringe
	name = "Bluespace Syringe"
	description = "Nanotrasen would make good use of high-capacity syringes. If you have any, please ship them."
	reward = 10000
	wanted_types = list(/obj/item/reagent_containers/syringe/bluespace)

/datum/bounty/item/science/bluespace_body_bag
	name = "Bluespace Body Bag"
	description = "Nanotrasen would make good use of high-capacity body bags. If you have any, please ship them."
	reward = 10000
	wanted_types = list(/obj/item/bodybag/bluespace)

/datum/bounty/item/science/nightvision_goggles
	name = "Night Vision Goggles"
	description = "An electrical storm has busted all the lights at CentCom. While management is waiting for replacements, perhaps some night vision goggles can be shipped?"
	reward = 10000
	wanted_types = list(/obj/item/clothing/glasses/night, /obj/item/clothing/glasses/meson/night, /obj/item/clothing/glasses/hud/health/night, /obj/item/clothing/glasses/hud/security/night, /obj/item/clothing/glasses/hud/diagnostic/night)

/datum/bounty/item/science/experimental_welding_tool
	name = "Experimental Welding Tool"
	description = "A recent accident has left most of CentCom's welding tools exploded. Ship replacements to be rewarded."
	reward = 10000
	required_count = 3
	wanted_types = list(/obj/item/weldingtool/experimental)

/datum/bounty/item/science/cryostasis_beaker
	name = "Cryostasis Beaker"
	description = "Chemists at Central Command have discovered a new chemical that can only be held in cryostasis beakers. The only problem is they don't have any! Rectify this to receive payment."
	reward = 10000
	wanted_types = list(/obj/item/reagent_containers/glass/beaker/noreact)

/datum/bounty/item/science/diamond_drill
	name = "Diamond Mining Drill"
	description = "Central Command is willing to pay three months salary in exchange for one diamond mining drill."
	reward = 15000
	wanted_types = list(/obj/item/pickaxe/drill/diamonddrill, /obj/item/mecha_parts/mecha_equipment/drill/diamonddrill)

/datum/bounty/item/science/floor_buffer
	name = "Floor Buffer Upgrade"
	description = "One of CentCom's janitors made a small fortune betting on carp races. Now they'd like to commission an upgrade to their floor buffer."
	reward = 10000
	wanted_types = list(/obj/item/janiupgrade)

/datum/bounty/item/science/advanced_mop
	name = "Advanced Mop"
	description = "Excuse me. I'd like to request 17 cr for a push broom rebristling. Either that, or an advanced mop."
	reward = 10000
	wanted_types = list(/obj/item/mop/advanced)

/datum/bounty/item/science/advanced_egun
	name = "Advanced Energy Gun"
	description = "With the price of rechargers on the rise, upper management is interested in purchasing guns that are self-powered. If you ship one, they'll pay."
	reward = 10000
	wanted_types = list(/obj/item/gun/energy/e_gun/nuclear)
