/datum/fantasy_affix/cosmetic_suffixes
	placement = AFFIX_SUFFIX
	alignment = AFFIX_GOOD | AFFIX_EVIL

	var/list/goodSuffixes
	var/list/badSuffixes

/datum/fantasy_affix/cosmetic_suffixes/New()
	goodSuffixes = list(
		"dexterity",
		"constitution",
		"intelligence",
		"wisdom",
		"charisma",
		"the forest",
		"the hills",
		"the plains",
		"the sea",
		"the sun",
		"the moon",
		"the void",
		"the world",
		"many secrets",
		"many tales",
		"many colors",
		"rending",
		"sundering",
		"the night",
		"the day",
		)
	badSuffixes = list(
		"draining",
		"burden",
		"discomfort",
		"awkwardness",
		"poor hygiene",
		"timidity",
		)

	weight = (length(goodSuffixes) + length(badSuffixes)) * 10

/datum/fantasy_affix/cosmetic_suffixes/apply(datum/component/fantasy/comp, newName)
	if(comp.quality > 0 || (comp.quality == 0 && prob(50)))
		return "[newName] of [pick(goodSuffixes)]"
	else
		return "[newName] of [pick(badSuffixes)]"

//////////// Good suffixes
/datum/fantasy_affix/bane
	placement = AFFIX_SUFFIX
	alignment = AFFIX_GOOD

/datum/fantasy_affix/bane/apply(datum/component/fantasy/comp, newName)
	. = ..()
	// This is set up to be easy to add to these lists as I expect it will need modifications
	var/static/list/possible_mobtypes
	if(!possible_mobtypes)
		// The base list of allowed mob/species types
		possible_mobtypes = typecacheof(list(
			/mob/living/simple_animal,
			/mob/living/carbon,
			/datum/species,
			))
		// Some particular types to disallow if they're too broad/abstract
		possible_mobtypes -= list(
			/mob/living/simple_animal/hostile,
			)
		// Some types to remove them and their subtypes
		possible_mobtypes -= typecacheof(list(
			/mob/living/carbon/human/species,
			))

	var/mob/picked_mobtype = pick(possible_mobtypes)
	// This works even with the species picks since we're only accessing the name

	var/obj/item/master = comp.parent
	comp.appliedComponents += master.AddComponent(/datum/component/bane, picked_mobtype)
	return "[newName] of [initial(picked_mobtype.name)] slaying"

/datum/fantasy_affix/summoning
	placement = AFFIX_SUFFIX
	alignment = AFFIX_GOOD
	weight = 5

/datum/fantasy_affix/summoning/apply(datum/component/fantasy/comp, newName)
	. = ..()
	// This is set up to be easy to add to these lists as I expect it will need modifications
	var/static/list/possible_mobtypes
	if(!possible_mobtypes)
		// The base list of allowed mob/species types
		possible_mobtypes = typecacheof(list(
			/mob/living/simple_animal,
			/mob/living/carbon,
			/datum/species,
			))
		// Some particular types to disallow if they're too broad/abstract
		possible_mobtypes -= list(
			/mob/living/simple_animal/hostile,
			)
		// Some types to remove them and their subtypes
		possible_mobtypes -= typecacheof(list(
			/mob/living/carbon/human/species,
			/mob/living/simple_animal/hostile/megafauna,
			))

	var/mob/picked_mobtype = pick(possible_mobtypes)
	// This works even with the species picks since we're only accessing the name

	var/obj/item/master = comp.parent
	var/max_mobs = max(CEILING(comp.quality/2, 1), 1)
	var/spawn_delay = 300 - 30 * comp.quality
	comp.appliedComponents += master.AddComponent(/datum/component/summoning, list(picked_mobtype), 100, max_mobs, spawn_delay)
	return "[newName] of [initial(picked_mobtype.name)] summoning"

/datum/fantasy_affix/shrapnel
	placement = AFFIX_SUFFIX
	alignment = AFFIX_GOOD

/datum/fantasy_affix/shrapnel/validate(datum/component/fantasy/comp)
	if(isgun(comp.parent))
		return TRUE
	return FALSE

/datum/fantasy_affix/shrapnel/apply(datum/component/fantasy/comp, newName)
	. = ..()
	// higher means more likely
	var/list/weighted_projectile_types = list(/obj/projectile/meteor = 1,
											  /obj/projectile/energy/nuclear_particle = 1,
											  /obj/projectile/beam/pulse = 1,
											  /obj/projectile/bullet/honker = 15,
											  /obj/projectile/temp = 15,
											  /obj/projectile/ion = 15,
											  /obj/projectile/magic/door = 15,
											  /obj/projectile/magic/locker = 15,
											  /obj/projectile/magic/fetch = 15,
											  /obj/projectile/beam/emitter = 15,
											  /obj/projectile/magic/flying = 15,
											  /obj/projectile/energy/net = 15,
											  /obj/projectile/bullet/incendiary/c9mm = 15,
											  /obj/projectile/temp/hot = 15,
											  /obj/projectile/beam/disabler = 15)

	var/obj/projectile/picked_projectiletype = pickweight(weighted_projectile_types)

	var/obj/item/master = comp.parent
	comp.appliedComponents += master.AddComponent(/datum/component/shrapnel, picked_projectiletype)
	return "[newName] of [initial(picked_projectiletype.name)] shrapnel"

/datum/fantasy_affix/strength
	placement = AFFIX_SUFFIX
	alignment = AFFIX_GOOD

/datum/fantasy_affix/strength/apply(datum/component/fantasy/comp, newName)
	. = ..()
	var/obj/item/master = comp.parent
	comp.appliedComponents += master.AddComponent(/datum/component/knockback, CEILING(comp.quality/2, 1), FLOOR(comp.quality/10, 1))
	return "[newName] of strength"

//////////// Bad suffixes

/datum/fantasy_affix/fool
	placement = AFFIX_SUFFIX
	alignment = AFFIX_EVIL

/datum/fantasy_affix/fool/apply(datum/component/fantasy/comp, newName)
	. = ..()
	var/obj/item/master = comp.parent
	comp.appliedComponents += master.AddComponent(/datum/component/squeak, list('sound/items/bikehorn.ogg'=1), 50)
	return "[newName] of the fool"
