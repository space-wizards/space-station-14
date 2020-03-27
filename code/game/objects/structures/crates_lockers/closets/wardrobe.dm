/obj/structure/closet/wardrobe
	name = "wardrobe"
	desc = "It's a storage unit for standard-issue Nanotrasen attire."
	icon_door = "blue"

/obj/structure/closet/wardrobe/PopulateContents()
	..()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/blue(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/blue(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/brown(src)
	return

/obj/structure/closet/wardrobe/pink
	name = "pink wardrobe"
	icon_door = "pink"

/obj/structure/closet/wardrobe/pink/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/pink(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/pink(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/brown(src)
	return

/obj/structure/closet/wardrobe/black
	name = "black wardrobe"
	icon_door = "black"

/obj/structure/closet/wardrobe/black/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/black(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/black(src)
	if(prob(25))
		new /obj/item/clothing/suit/jacket/leather(src)
	if(prob(20))
		new /obj/item/clothing/suit/jacket/leather/overcoat(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/black(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/head/that(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/head/soft/black(src)
	new /obj/item/clothing/mask/bandana/black(src)
	new /obj/item/clothing/mask/bandana/black(src)
	if(prob(40))
		new /obj/item/clothing/mask/bandana/skull(src)
	return


/obj/structure/closet/wardrobe/green
	name = "green wardrobe"
	icon_door = "green"

/obj/structure/closet/wardrobe/green/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/green(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/green(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/black(src)
	new /obj/item/clothing/mask/bandana/green(src)
	new /obj/item/clothing/mask/bandana/green(src)
	return


/obj/structure/closet/wardrobe/orange
	name = "prison wardrobe"
	desc = "It's a storage unit for Nanotrasen-regulation prisoner attire."
	icon_door = "orange"

/obj/structure/closet/wardrobe/orange/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/rank/prisoner(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/rank/prisoner/skirt(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/orange(src)
	return


/obj/structure/closet/wardrobe/yellow
	name = "yellow wardrobe"
	icon_door = "yellow"

/obj/structure/closet/wardrobe/yellow/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/yellow(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/yellow(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/orange(src)
	new /obj/item/clothing/mask/bandana/gold(src)
	new /obj/item/clothing/mask/bandana/gold(src)
	return


/obj/structure/closet/wardrobe/white
	name = "white wardrobe"
	icon_door = "white"

/obj/structure/closet/wardrobe/white/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/white(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/white(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/white(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/head/soft/mime(src)
	return

/obj/structure/closet/wardrobe/pjs
	name = "pajama wardrobe"
	icon_door = "white"

/obj/structure/closet/wardrobe/pjs/PopulateContents()
	new /obj/item/clothing/under/misc/pj/red(src)
	new /obj/item/clothing/under/misc/pj/red(src)
	new /obj/item/clothing/under/misc/pj/blue(src)
	new /obj/item/clothing/under/misc/pj/blue(src)
	for(var/i in 1 to 4)
		new /obj/item/clothing/shoes/sneakers/white(src)
	return


/obj/structure/closet/wardrobe/grey
	name = "grey wardrobe"
	icon_door = "grey"

/obj/structure/closet/wardrobe/grey/PopulateContents()
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/grey(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/under/color/jumpskirt/grey(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/shoes/sneakers/black(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/head/soft/grey(src)
	if(prob(50))
		new /obj/item/storage/backpack/duffelbag(src)
	if(prob(40))
		new /obj/item/clothing/mask/bandana/black(src)
		new /obj/item/clothing/mask/bandana/black(src)
	if(prob(40))
		new /obj/item/clothing/under/misc/assistantformal(src)
	if(prob(40))
		new /obj/item/clothing/under/misc/assistantformal(src)
	if(prob(30))
		new /obj/item/clothing/suit/hooded/wintercoat(src)
		new /obj/item/clothing/shoes/winterboots(src)
	if(prob(30))
		new /obj/item/clothing/accessory/pocketprotector(src)
	return


/obj/structure/closet/wardrobe/mixed
	name = "mixed wardrobe"
	icon_door = "mixed"

/obj/structure/closet/wardrobe/mixed/PopulateContents()
	if(prob(40))
		new /obj/item/clothing/suit/jacket(src)
	if(prob(40))
		new /obj/item/clothing/suit/jacket(src)
	new /obj/item/clothing/under/color/white(src)
	new /obj/item/clothing/under/color/jumpskirt/white(src)
	new /obj/item/clothing/under/color/blue(src)
	new /obj/item/clothing/under/color/jumpskirt/blue(src)
	new /obj/item/clothing/under/color/yellow(src)
	new /obj/item/clothing/under/color/jumpskirt/yellow(src)
	new /obj/item/clothing/under/color/green(src)
	new /obj/item/clothing/under/color/jumpskirt/green(src)
	new /obj/item/clothing/under/color/orange(src)
	new /obj/item/clothing/under/color/jumpskirt/orange(src)
	new /obj/item/clothing/under/color/pink(src)
	new /obj/item/clothing/under/color/jumpskirt/pink(src)
	new /obj/item/clothing/under/color/red(src)
	new /obj/item/clothing/under/color/jumpskirt/red(src)
	new /obj/item/clothing/under/color/darkblue(src)
	new /obj/item/clothing/under/color/jumpskirt/darkblue(src)
	new /obj/item/clothing/under/color/teal(src)
	new /obj/item/clothing/under/color/jumpskirt/teal(src)
	new /obj/item/clothing/under/color/lightpurple(src)
	new /obj/item/clothing/under/color/jumpskirt/lightpurple(src)
	new /obj/item/clothing/under/color/green(src)
	new /obj/item/clothing/under/color/jumpskirt/green(src)
	new /obj/item/clothing/mask/bandana/red(src)
	new /obj/item/clothing/mask/bandana/red(src)
	new /obj/item/clothing/mask/bandana/blue(src)
	new /obj/item/clothing/mask/bandana/blue(src)
	new /obj/item/clothing/mask/bandana/gold(src)
	new /obj/item/clothing/mask/bandana/gold(src)
	new /obj/item/clothing/shoes/sneakers/black(src)
	new /obj/item/clothing/shoes/sneakers/brown(src)
	new /obj/item/clothing/shoes/sneakers/white(src)
	if(prob(30))
		new /obj/item/clothing/suit/hooded/wintercoat(src)
		new /obj/item/clothing/shoes/winterboots(src)
	return
