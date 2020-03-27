//Spacebattle Areas

/area/awaymission/spacebattle
	name = "Space Battle"
	icon_state = "awaycontent1"
	requires_power = FALSE

/area/awaymission/spacebattle/cruiser
	name = "Nanotrasen Cruiser"
	icon_state = "awaycontent2"

/area/awaymission/spacebattle/syndicate1
	name = "Syndicate Assault Ship 1"
	icon_state = "awaycontent3"

/area/awaymission/spacebattle/syndicate2
	name = "Syndicate Assault Ship 2"
	icon_state = "awaycontent4"

/area/awaymission/spacebattle/syndicate3
	name = "Syndicate Assault Ship 3"
	icon_state = "awaycontent5"

/area/awaymission/spacebattle/syndicate4
	name = "Syndicate War Sphere 1"
	icon_state = "awaycontent6"

/area/awaymission/spacebattle/syndicate5
	name = "Syndicate War Sphere 2"
	icon_state = "awaycontent7"

/area/awaymission/spacebattle/syndicate6
	name = "Syndicate War Sphere 3"
	icon_state = "awaycontent8"

/area/awaymission/spacebattle/syndicate7
	name = "Syndicate Fighter"
	icon_state = "awaycontent9"

/area/awaymission/spacebattle/secret
	name = "Hidden Chamber"
	icon_state = "awaycontent10"

/mob/living/simple_animal/hostile/syndicate/ranged/spacebattle
	loot = list(/obj/effect/mob_spawn/human/corpse/syndicatesoldier,
				/obj/item/gun/ballistic/automatic/c20r,
				/obj/item/shield/energy)

/mob/living/simple_animal/hostile/syndicate/melee/spacebattle
	deathmessage = "falls limp as they release their grip from the energy weapons, activating their self-destruct function!"
	loot = list(/obj/effect/mob_spawn/human/corpse/syndicatesoldier)
