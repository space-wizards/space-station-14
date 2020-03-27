
// Modules

/turf/open/floor/plasteel/shuttle/red/syndicate
	name = "floor" //Not Brig Floor

/datum/mapGeneratorModule/bottomLayer/syndieFloor
	spawnableTurfs = list(/turf/open/floor/plasteel/shuttle/red/syndicate = 100)

/datum/mapGeneratorModule/border/syndieWalls
	spawnableAtoms = list()
	spawnableTurfs = list(/turf/closed/wall/r_wall = 100)


/datum/mapGeneratorModule/syndieFurniture
	clusterCheckFlags = CLUSTER_CHECK_ALL
	spawnableTurfs = list()
	spawnableAtoms = list(/obj/structure/table = 20,/obj/structure/chair = 15,/obj/structure/chair/stool = 10, \
		/obj/structure/frame/computer = 15, /obj/item/storage/toolbox/syndicate = 15 ,\
		/obj/structure/closet/syndicate = 25, /obj/machinery/suit_storage_unit/syndicate = 15)

/datum/mapGeneratorModule/splatterLayer/syndieMobs
	spawnableAtoms = list(/mob/living/simple_animal/hostile/syndicate = 30, \
		/mob/living/simple_animal/hostile/syndicate/melee = 20, \
		/mob/living/simple_animal/hostile/syndicate/ranged = 20, \
		/mob/living/simple_animal/hostile/viscerator = 30)
	spawnableTurfs = list()

// Generators

/datum/mapGenerator/syndicate/empty //walls and floor only
	modules = list(/datum/mapGeneratorModule/bottomLayer/syndieFloor, \
		/datum/mapGeneratorModule/border/syndieWalls,\
		/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Pattern: Shuttle Room: Syndicate"

/datum/mapGenerator/syndicate/mobsonly
	modules = list(/datum/mapGeneratorModule/bottomLayer/syndieFloor, \
		/datum/mapGeneratorModule/border/syndieWalls,\
		/datum/mapGeneratorModule/splatterLayer/syndieMobs, \
		/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Pattern: Shuttle Room: Syndicate: Mobs"

/datum/mapGenerator/syndicate/furniture
	modules = list(/datum/mapGeneratorModule/bottomLayer/syndieFloor, \
		/datum/mapGeneratorModule/border/syndieWalls,\
		/datum/mapGeneratorModule/syndieFurniture, \
		/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Pattern: Shuttle Room: Syndicate: Furniture"

/datum/mapGenerator/syndicate/full
	modules = list(/datum/mapGeneratorModule/bottomLayer/syndieFloor, \
		/datum/mapGeneratorModule/border/syndieWalls,\
		/datum/mapGeneratorModule/syndieFurniture, \
		/datum/mapGeneratorModule/splatterLayer/syndieMobs, \
		/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Pattern: Shuttle Room: Syndicate: All"
