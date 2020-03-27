//Asteroid turfs
/datum/mapGeneratorModule/bottomLayer/asteroidTurfs
	spawnableTurfs = list(/turf/open/floor/plating/asteroid = 100)

/datum/mapGeneratorModule/bottomLayer/asteroidWalls
	spawnableTurfs = list(/turf/closed/mineral = 100)

//Border walls
/datum/mapGeneratorModule/border/asteroidWalls
	spawnableAtoms = list()
	spawnableTurfs = list(/turf/closed/mineral = 100)

//Random walls
/datum/mapGeneratorModule/splatterLayer/asteroidWalls
	clusterCheckFlags = CLUSTER_CHECK_NONE
	spawnableAtoms = list()
	spawnableTurfs = list(/turf/closed/mineral = 30)

//Monsters
/datum/mapGeneratorModule/splatterLayer/asteroidMonsters
	spawnableTurfs = list()
	spawnableAtoms = list(/mob/living/simple_animal/hostile/asteroid/basilisk = 10, \
		/mob/living/simple_animal/hostile/asteroid/hivelord = 10, \
		/mob/living/simple_animal/hostile/asteroid/goliath = 10)


// GENERATORS

/datum/mapGenerator/asteroid/hollow
	modules = list(/datum/mapGeneratorModule/bottomLayer/asteroidTurfs, \
		/datum/mapGeneratorModule/border/asteroidWalls)
	buildmode_name = "Pattern: Asteroid Room \[AIRLESS!\]"

/datum/mapGenerator/asteroid/hollow/random
	modules = list(/datum/mapGeneratorModule/bottomLayer/asteroidTurfs, \
		/datum/mapGeneratorModule/border/asteroidWalls, \
		/datum/mapGeneratorModule/splatterLayer/asteroidWalls)
	buildmode_name = "Pattern: Asteroid Room: Splatter Walls \[AIRLESS!\]"

/datum/mapGenerator/asteroid/hollow/random/monsters
	modules = list(/datum/mapGeneratorModule/bottomLayer/asteroidTurfs, \
		/datum/mapGeneratorModule/border/asteroidWalls, \
		/datum/mapGeneratorModule/splatterLayer/asteroidWalls, \
		/datum/mapGeneratorModule/splatterLayer/asteroidMonsters)
	buildmode_name = "Pattern: Asteroid Room: Splatter Walls + Monsters \[AIRLESS!\]"

/datum/mapGenerator/asteroid/filled
	modules = list(/datum/mapGeneratorModule/bottomLayer/asteroidWalls)
	buildmode_name = "Block: Asteroid Walls"
