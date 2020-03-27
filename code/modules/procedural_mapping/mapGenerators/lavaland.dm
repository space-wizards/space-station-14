
/datum/mapGeneratorModule/bottomLayer/lavaland_default
	spawnableTurfs = list(/turf/open/floor/plating/asteroid/basalt/lava_land_surface = 100)

/datum/mapGeneratorModule/bottomLayer/lavaland_mineral
	spawnableTurfs = list(/turf/closed/mineral/random/volcanic = 100)

/datum/mapGeneratorModule/bottomLayer/lavaland_mineral/dense
	spawnableTurfs = list(/turf/closed/mineral/random/high_chance/volcanic = 100)

/datum/mapGeneratorModule/splatterLayer/lavalandMonsters
	spawnableTurfs = list()
	spawnableAtoms = list(/mob/living/simple_animal/hostile/asteroid/goliath/beast = 10,
	/mob/living/simple_animal/hostile/asteroid/hivelord/legion = 10,
	/mob/living/simple_animal/hostile/asteroid/basilisk/watcher = 10)

/datum/mapGeneratorModule/splatterLayer/lavalandTendrils
	spawnableTurfs = list()
	spawnableAtoms = list(/obj/structure/spawner/lavaland = 5,
	/obj/structure/spawner/lavaland/legion = 5,
	/obj/structure/spawner/lavaland/goliath = 5)

/datum/mapGenerator/lavaland/ground_only
	modules = list(/datum/mapGeneratorModule/bottomLayer/lavaland_default)
	buildmode_name = "Block: Lavaland Floor"

/datum/mapGenerator/lavaland/dense_ores
	modules = list(/datum/mapGeneratorModule/bottomLayer/lavaland_mineral/dense)
	buildmode_name = "Block: Lavaland Ores: Dense"

/datum/mapGenerator/lavaland/normal_ores
	modules = list(/datum/mapGeneratorModule/bottomLayer/lavaland_mineral)
	buildmode_name = "Block: Lavaland Ores"
