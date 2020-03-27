/obj/effect/spawner/bundle
	name = "bundle spawner"
	icon = 'icons/mob/screen_gen.dmi'
	icon_state = "x2"
	color = "#00FF00"

	var/list/items

/obj/effect/spawner/bundle/Initialize(mapload)
	..()
	if(items && items.len)
		var/turf/T = get_turf(src)
		for(var/path in items)
			new path(T)
	return INITIALIZE_HINT_QDEL

/obj/effect/spawner/bundle/costume/chicken
	name = "chicken costume spawner"
	items = list(
		/obj/item/clothing/suit/chickensuit,
		/obj/item/clothing/head/chicken,
		/obj/item/reagent_containers/food/snacks/egg)

/obj/effect/spawner/bundle/costume/gladiator
	name = "gladiator costume spawner"
	items = list(
		/obj/item/clothing/under/costume/gladiator,
		/obj/item/clothing/head/helmet/gladiator)

/obj/effect/spawner/bundle/costume/madscientist
	name = "mad scientist costume spawner"
	items = list(
		/obj/item/clothing/under/rank/captain/suit,
		/obj/item/clothing/head/flatcap,
		/obj/item/clothing/suit/toggle/labcoat/mad)

/obj/effect/spawner/bundle/costume/elpresidente
	name = "el presidente costume spawner"
	items = list(
		/obj/item/clothing/under/rank/captain/suit,
		/obj/item/clothing/head/flatcap,
		/obj/item/clothing/mask/cigarette/cigar/havana,
		/obj/item/clothing/shoes/jackboots)

/obj/effect/spawner/bundle/costume/nyangirl
	name = "nyangirl costume spawner"
	items = list(
		/obj/item/clothing/under/costume/schoolgirl,
		/obj/item/clothing/head/kitty,
		/obj/item/clothing/glasses/blindfold)

/obj/effect/spawner/bundle/costume/maid
	name = "maid costume spawner"
	items = list(
		/obj/item/clothing/under/dress/skirt,
		/obj/effect/spawner/lootdrop/minor/beret_or_rabbitears,
		/obj/item/clothing/glasses/blindfold)


/obj/effect/spawner/bundle/costume/butler
	name = "butler costume spawner"
	items = list(
		/obj/item/clothing/accessory/waistcoat,
		/obj/item/clothing/under/suit/black,
		/obj/item/clothing/head/that)

/obj/effect/spawner/bundle/costume/highlander
	name = "highlander costume spawner"
	items = list(
		/obj/item/clothing/under/costume/kilt,
		/obj/item/clothing/head/beret)

/obj/effect/spawner/bundle/costume/prig
	name = "prig costume spawner"
	items = list(
		/obj/item/clothing/accessory/waistcoat,
		/obj/item/clothing/glasses/monocle,
		/obj/effect/spawner/lootdrop/minor/bowler_or_that,
		/obj/item/clothing/shoes/sneakers/black,
		/obj/item/cane,
		/obj/item/clothing/under/suit/sl,
		/obj/item/clothing/mask/fakemoustache)

/obj/effect/spawner/bundle/costume/plaguedoctor
	name = "plague doctor costume spawner"
	items = list(
		/obj/item/clothing/suit/bio_suit/plaguedoctorsuit,
		/obj/item/clothing/head/plaguedoctorhat,
		/obj/item/clothing/mask/gas/plaguedoctor)

/obj/effect/spawner/bundle/costume/nightowl
	name = "night owl costume spawner"
	items = list(
		/obj/item/clothing/suit/toggle/owlwings,
		/obj/item/clothing/under/costume/owl,
		/obj/item/clothing/mask/gas/owl_mask)

/obj/effect/spawner/bundle/costume/griffin
	name = "griffin costume spawner"
	items = list(
		/obj/item/clothing/suit/toggle/owlwings/griffinwings,
		/obj/item/clothing/shoes/griffin,
		/obj/item/clothing/under/costume/griffin,
		/obj/item/clothing/head/griffin)

/obj/effect/spawner/bundle/costume/waiter
	name = "waiter costume spawner"
	items = list(
		/obj/item/clothing/under/suit/waiter,
		/obj/effect/spawner/lootdrop/minor/kittyears_or_rabbitears,
		/obj/item/clothing/suit/apron)

/obj/effect/spawner/bundle/costume/pirate
	name = "pirate costume spawner"
	items = list(
		/obj/item/clothing/under/costume/pirate,
		/obj/item/clothing/suit/pirate,
		/obj/effect/spawner/lootdrop/minor/pirate_or_bandana,
		/obj/item/clothing/glasses/eyepatch)

/obj/effect/spawner/bundle/costume/commie
	name = "commie costume spawner"
	items = list(
		/obj/item/clothing/under/costume/soviet,
		/obj/item/clothing/head/ushanka)

/obj/effect/spawner/bundle/costume/imperium_monk
	name = "imperium monk costume spawner"
	items = list(
		/obj/item/clothing/suit/imperium_monk,
		/obj/effect/spawner/lootdrop/minor/twentyfive_percent_cyborg_mask)

/obj/effect/spawner/bundle/costume/holiday_priest
	name = "holiday priest costume spawner"
	items = list(
		/obj/item/clothing/suit/chaplainsuit/holidaypriest)

/obj/effect/spawner/bundle/costume/marisawizard
	name = "marisa wizard costume spawner"
	items = list(
		/obj/item/clothing/shoes/sandal/marisa,
		/obj/item/clothing/head/wizard/marisa/fake,
		/obj/item/clothing/suit/wizrobe/marisa/fake)

/obj/effect/spawner/bundle/costume/cutewitch
	name = "cute witch costume spawner"
	items = list(
		/obj/item/clothing/under/dress/sundress,
		/obj/item/clothing/head/witchwig,
		/obj/item/staff/broom)

/obj/effect/spawner/bundle/costume/wizard
	name = "wizard costume spawner"
	items = list(
		/obj/item/clothing/shoes/sandal,
		/obj/item/clothing/suit/wizrobe/fake,
		/obj/item/clothing/head/wizard/fake,
		/obj/item/staff)

/obj/effect/spawner/bundle/costume/sexyclown
	name = "sexy clown costume spawner"
	items = list(
		/obj/item/clothing/mask/gas/sexyclown,
		/obj/item/clothing/under/rank/civilian/clown/sexy)

/obj/effect/spawner/bundle/costume/sexymime
	name = "sexy mime costume spawner"
	items = list(
		/obj/item/clothing/mask/gas/sexymime,
		/obj/item/clothing/under/rank/civilian/mime/sexy)
