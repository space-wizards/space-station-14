/datum/bounty/item/slime
	reward = 3000

/datum/bounty/item/slime/New()
	..()
	description = "Nanotrasen's science lead is hunting for the rare and exotic [name]. A bounty has been offered for finding it."
	reward += rand(0, 4) * 500

/datum/bounty/item/slime/green
	name = "Green Slime Extract"
	wanted_types = list(/obj/item/slime_extract/green)

/datum/bounty/item/slime/pink
	name = "Pink Slime Extract"
	wanted_types = list(/obj/item/slime_extract/pink)

/datum/bounty/item/slime/gold
	name = "Gold Slime Extract"
	wanted_types = list(/obj/item/slime_extract/gold)

/datum/bounty/item/slime/oil
	name = "Oil Slime Extract"
	wanted_types = list(/obj/item/slime_extract/oil)

/datum/bounty/item/slime/black
	name = "Black Slime Extract"
	wanted_types = list(/obj/item/slime_extract/black)

/datum/bounty/item/slime/lightpink
	name = "Light Pink Slime Extract"
	wanted_types = list(/obj/item/slime_extract/lightpink)

/datum/bounty/item/slime/adamantine
	name = "Adamantine Slime Extract"
	wanted_types = list(/obj/item/slime_extract/adamantine)

/datum/bounty/item/slime/rainbow
	name = "Rainbow Slime Extract"
	wanted_types = list(/obj/item/slime_extract/rainbow)
