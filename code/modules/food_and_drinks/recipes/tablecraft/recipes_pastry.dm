
// see code/module/crafting/table.dm

////////////////////////////////////////////////DONUTS////////////////////////////////////////////////

/datum/crafting_recipe/food/donut
	time = 15
	name = "Donut"
	reqs = list(
		/datum/reagent/consumable/sugar = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/plain
	subcategory = CAT_PASTRY


/datum/crafting_recipe/food/donut/chaos
	name = "Chaos donut"
	reqs = list(
		/datum/reagent/consumable/frostoil = 5,
		/datum/reagent/consumable/capsaicin = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/chaos

datum/crafting_recipe/food/donut/meat
	time = 15
	name = "Meat donut"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/meat/rawcutlet = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/meat

/datum/crafting_recipe/food/donut/jelly
	name = "Jelly donut"
	reqs = list(
		/datum/reagent/consumable/berryjuice = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/plain

/datum/crafting_recipe/food/donut/slimejelly
	name = "Slime jelly donut"
	reqs = list(
		/datum/reagent/toxin/slimejelly = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain


/datum/crafting_recipe/food/donut/berry
	name = "Berry Donut"
	reqs = list(
		/datum/reagent/consumable/berryjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/berry

/datum/crafting_recipe/food/donut/trumpet
	name = "Spaceman's Donut"
	reqs = list(
		/datum/reagent/medicine/polypyr = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)

	result = /obj/item/reagent_containers/food/snacks/donut/trumpet

/datum/crafting_recipe/food/donut/apple
	name = "Apple Donut"
	reqs = list(
		/datum/reagent/consumable/applejuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/apple

/datum/crafting_recipe/food/donut/caramel
	name = "Caramel Donut"
	reqs = list(
		/datum/reagent/consumable/caramel = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/caramel

/datum/crafting_recipe/food/donut/choco
	name = "Chocolate Donut"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/chocolatebar = 1,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/choco

/datum/crafting_recipe/food/donut/blumpkin
	name = "Blumpkin Donut"
	reqs = list(
		/datum/reagent/consumable/blumpkinjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/blumpkin

/datum/crafting_recipe/food/donut/bungo
	name = "Bungo Donut"
	reqs = list(
		/datum/reagent/consumable/bungojuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/bungo

/datum/crafting_recipe/food/donut/matcha
	name = "Matcha Donut"
	reqs = list(
		/datum/reagent/toxin/teapowder = 3,
		/obj/item/reagent_containers/food/snacks/donut/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/matcha

////////////////////////////////////////////////////JELLY DONUTS///////////////////////////////////////////////////////

/datum/crafting_recipe/food/donut/jelly/berry
	name = "Berry Jelly Donut"
	reqs = list(
		/datum/reagent/consumable/berryjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/berry

/datum/crafting_recipe/food/donut/jelly/trumpet
	name = "Spaceman's Jelly Donut"
	reqs = list(
		/datum/reagent/medicine/polypyr = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)

	result = /obj/item/reagent_containers/food/snacks/donut/jelly/trumpet

/datum/crafting_recipe/food/donut/jelly/apple
	name = "Apple Jelly Donut"
	reqs = list(
		/datum/reagent/consumable/applejuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/apple

/datum/crafting_recipe/food/donut/jelly/caramel
	name = "Caramel Jelly Donut"
	reqs = list(
		/datum/reagent/consumable/caramel = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/caramel

/datum/crafting_recipe/food/donut/jelly/choco
	name = "Chocolate Jelly Donut"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/chocolatebar = 1,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/choco

/datum/crafting_recipe/food/donut/jelly/blumpkin
	name = "Blumpkin Jelly Donut"
	reqs = list(
		/datum/reagent/consumable/blumpkinjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/blumpkin

/datum/crafting_recipe/food/donut/jelly/bungo
	name = "Bungo Jelly Donut"
	reqs = list(
		/datum/reagent/consumable/bungojuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/bungo

/datum/crafting_recipe/food/donut/jelly/matcha
	name = "Matcha Jelly Donut"
	reqs = list(
		/datum/reagent/toxin/teapowder = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/matcha

////////////////////////////////////////////////////SLIME  DONUTS///////////////////////////////////////////////////////

/datum/crafting_recipe/food/donut/slimejelly/berry
	name = "Berry Slime Donut"
	reqs = list(
		/datum/reagent/consumable/berryjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/berry

/datum/crafting_recipe/food/donut/slimejelly/trumpet
	name = "Spaceman's Slime Donut"
	reqs = list(
		/datum/reagent/medicine/polypyr = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)

	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/trumpet

/datum/crafting_recipe/food/donut/slimejelly/apple
	name = "Apple Slime Donut"
	reqs = list(
		/datum/reagent/consumable/applejuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/apple

/datum/crafting_recipe/food/donut/slimejelly/caramel
	name = "Caramel Slime Donut"
	reqs = list(
		/datum/reagent/consumable/caramel = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/caramel

/datum/crafting_recipe/food/donut/slimejelly/choco
	name = "Chocolate Slime Donut"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/chocolatebar = 1,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/choco

/datum/crafting_recipe/food/donut/slimejelly/blumpkin
	name = "Blumpkin Slime Donut"
	reqs = list(
		/datum/reagent/consumable/blumpkinjuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/blumpkin

/datum/crafting_recipe/food/donut/slimejelly/bungo
	name = "Bungo Slime Donut"
	reqs = list(
		/datum/reagent/consumable/bungojuice = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/bungo

/datum/crafting_recipe/food/donut/slimejelly/matcha
	name = "Matcha Slime Donut"
	reqs = list(
		/datum/reagent/toxin/teapowder = 3,
		/obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donut/jelly/slimejelly/matcha

////////////////////////////////////////////////WAFFLES AND PANCAKES////////////////////////////////////////////////

/datum/crafting_recipe/food/waffles
	time = 15
	name = "Waffles"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 2
	)
	result = /obj/item/reagent_containers/food/snacks/waffles
	subcategory = CAT_PASTRY


/datum/crafting_recipe/food/soylenviridians
	name = "Soylent viridians"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 2,
		/obj/item/reagent_containers/food/snacks/grown/soybeans = 1
	)
	result = /obj/item/reagent_containers/food/snacks/soylenviridians
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/soylentgreen
	name = "Soylent green"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 2,
		/obj/item/reagent_containers/food/snacks/meat/slab/human = 2
	)
	result = /obj/item/reagent_containers/food/snacks/soylentgreen
	subcategory = CAT_PASTRY


/datum/crafting_recipe/food/rofflewaffles
	name = "Roffle waffles"
	reqs = list(
		/datum/reagent/drug/mushroomhallucinogen = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 2
	)
	result = /obj/item/reagent_containers/food/snacks/rofflewaffles
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/pancakes
	name = "Pancake"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/pancakes
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/bbpancakes
	name = "Blueberry pancake"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/berries = 1
	)
	result = /obj/item/reagent_containers/food/snacks/pancakes/blueberry
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/ccpancakes
	name = "Chocolate chip pancake"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/chocolatebar = 1
	)
	result = /obj/item/reagent_containers/food/snacks/pancakes/chocolatechip
	subcategory = CAT_PASTRY


////////////////////////////////////////////////DONKPOCCKETS////////////////////////////////////////////////

/datum/crafting_recipe/food/donkpocket
	time = 15
	name = "Donk-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/dankpocket
	time = 15
	name = "Dank-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/cannabis = 1
	)
	result = /obj/item/reagent_containers/food/snacks/dankpocket
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/spicy
	time = 15
	name = "Spicy-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1,
		/obj/item/reagent_containers/food/snacks/grown/chili
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/spicy
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/teriyaki
	time = 15
	name = "Teriyaki-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1,
		/datum/reagent/consumable/soysauce = 3
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/teriyaki
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/pizza
	time = 15
	name = "Pizza-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1,
		/obj/item/reagent_containers/food/snacks/grown/tomato = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/pizza
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/honk
	time = 15
	name = "Honk-Pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/banana = 1,
		/datum/reagent/consumable/sugar = 3
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/honk
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/berry
	time = 15
	name = "Berry-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/berries = 1
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/berry
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/donkpocket/gondola
	time = 15
	name = "Gondola-pocket"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1,
		/datum/reagent/tranquility = 5
	)
	result = /obj/item/reagent_containers/food/snacks/donkpocket/gondola
	subcategory = CAT_PASTRY

////////////////////////////////////////////////MUFFINS////////////////////////////////////////////////

/datum/crafting_recipe/food/muffin
	time = 15
	name = "Muffin"
	reqs = list(
		/datum/reagent/consumable/milk = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/muffin
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/berrymuffin
	name = "Berry muffin"
	reqs = list(
		/datum/reagent/consumable/milk = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/berries = 1
	)
	result = /obj/item/reagent_containers/food/snacks/muffin/berry
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/booberrymuffin
	name = "Booberry muffin"
	reqs = list(
		/datum/reagent/consumable/milk = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/berries = 1,
		/obj/item/ectoplasm = 1
	)
	result = /obj/item/reagent_containers/food/snacks/muffin/booberry
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/chawanmushi
	name = "Chawanmushi"
	reqs = list(
		/datum/reagent/water = 5,
		/datum/reagent/consumable/soysauce = 5,
		/obj/item/reagent_containers/food/snacks/boiledegg = 2,
		/obj/item/reagent_containers/food/snacks/grown/mushroom/chanterelle = 1
	)
	result = /obj/item/reagent_containers/food/snacks/chawanmushi
	subcategory = CAT_PASTRY

////////////////////////////////////////////OTHER////////////////////////////////////////////

/datum/crafting_recipe/food/hotdog
	name = "Hot dog"
	reqs = list(
		/datum/reagent/consumable/ketchup = 5,
		/obj/item/reagent_containers/food/snacks/bun = 1,
		/obj/item/reagent_containers/food/snacks/sausage = 1
	)
	result = /obj/item/reagent_containers/food/snacks/hotdog
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/meatbun
	name = "Meat bun"
	reqs = list(
		/datum/reagent/consumable/soysauce = 5,
		/obj/item/reagent_containers/food/snacks/bun = 1,
		/obj/item/reagent_containers/food/snacks/faggot = 1,
		/obj/item/reagent_containers/food/snacks/grown/cabbage = 1
	)
	result = /obj/item/reagent_containers/food/snacks/meatbun
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/khachapuri
	name = "Khachapuri"
	reqs = list(
		/datum/reagent/consumable/eggyolk = 5,
		/obj/item/reagent_containers/food/snacks/cheesewedge = 1,
		/obj/item/reagent_containers/food/snacks/store/bread/plain = 1
	)
	result = /obj/item/reagent_containers/food/snacks/khachapuri
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/sugarcookie
	time = 15
	name = "Sugar cookie"
	reqs = list(
		/datum/reagent/consumable/sugar = 5,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/sugarcookie
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/fortunecookie
	time = 15
	name = "Fortune cookie"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/paper = 1
	)
	parts =	list(
		/obj/item/paper = 1
	)
	result = /obj/item/reagent_containers/food/snacks/fortunecookie
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/poppypretzel
	time = 15
	name = "Poppy pretzel"
	reqs = list(
		/obj/item/seeds/poppy = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1
	)
	result = /obj/item/reagent_containers/food/snacks/poppypretzel
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/plumphelmetbiscuit
	time = 15
	name = "Plumphelmet biscuit"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/mushroom/plumphelmet = 1
	)
	result = /obj/item/reagent_containers/food/snacks/plumphelmetbiscuit
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/cracker
	time = 15
	name = "Cracker"
	reqs = list(
		/datum/reagent/consumable/sodiumchloride = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
	)
	result = /obj/item/reagent_containers/food/snacks/cracker
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/chococornet
	name = "Choco cornet"
	reqs = list(
		/datum/reagent/consumable/sodiumchloride = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/chocolatebar = 1
	)
	result = /obj/item/reagent_containers/food/snacks/chococornet
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/oatmealcookie
	name = "Oatmeal cookie"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/oat = 1
	)
	result = /obj/item/reagent_containers/food/snacks/oatmealcookie
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/raisincookie
	name = "Raisin cookie"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/no_raisin = 1,
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/oat = 1
	)
	result = /obj/item/reagent_containers/food/snacks/raisincookie
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/cherrycupcake
	name = "Cherry cupcake"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/cherries = 1
	)
	result = /obj/item/reagent_containers/food/snacks/cherrycupcake
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/bluecherrycupcake
	name = "Blue cherry cupcake"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/obj/item/reagent_containers/food/snacks/grown/bluecherries = 1
	)
	result = /obj/item/reagent_containers/food/snacks/bluecherrycupcake
	subcategory = CAT_PASTRY

/datum/crafting_recipe/food/honeybun
	name = "Honey bun"
	reqs = list(
		/obj/item/reagent_containers/food/snacks/pastrybase = 1,
		/datum/reagent/consumable/honey = 5
	)
	result = /obj/item/reagent_containers/food/snacks/honeybun
	subcategory = CAT_PASTRY
