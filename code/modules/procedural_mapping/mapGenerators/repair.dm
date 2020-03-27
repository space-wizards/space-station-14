/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel
	spawnableTurfs = list(/turf/open/floor/plasteel = 100)
	var/ignore_wall = FALSE
	allowAtomsOnSpace = TRUE

/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel/place(turf/T)
	if(isclosedturf(T) && !ignore_wall)
		return FALSE
	return ..()

/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel/flatten
	ignore_wall = TRUE

/datum/mapGeneratorModule/border/normalWalls
	spawnableAtoms = list()
	spawnableTurfs = list(/turf/closed/wall = 100)
	allowAtomsOnSpace = TRUE

/datum/mapGeneratorModule/reload_station_map/generate()
	if(!istype(mother, /datum/mapGenerator/repair/reload_station_map))
		return
	var/datum/mapGenerator/repair/reload_station_map/mother1 = mother
	GLOB.reloading_map = TRUE
	// This is kind of finicky on multi-Z maps but the reader would need to be
	// changed to allow Z cropping and that's a mess
	var/z_offset = SSmapping.station_start
	var/list/bounds
	for (var/path in SSmapping.config.GetFullMapPaths())
		var/datum/parsed_map/parsed = load_map(file(path), 1, 1, z_offset, measureOnly = FALSE, no_changeturf = FALSE, cropMap=TRUE, x_lower = mother1.x_low, y_lower = mother1.y_low, x_upper = mother1.x_high, y_upper = mother1.y_high)
		bounds = parsed?.bounds
		z_offset += bounds[MAP_MAXZ] - bounds[MAP_MINZ] + 1

	var/list/obj/machinery/atmospherics/atmos_machines = list()
	var/list/obj/structure/cable/cables = list()
	var/list/atom/atoms = list()

	repopulate_sorted_areas()

	for(var/L in block(locate(bounds[MAP_MINX], bounds[MAP_MINY], SSmapping.station_start),
						locate(bounds[MAP_MAXX], bounds[MAP_MAXY], z_offset - 1)))
		set waitfor = FALSE
		var/turf/B = L
		atoms += B
		for(var/A in B)
			atoms += A
			if(istype(A,/obj/structure/cable))
				cables += A
				continue
			if(istype(A,/obj/machinery/atmospherics))
				atmos_machines += A

	SSatoms.InitializeAtoms(atoms)
	SSmachines.setup_template_powernets(cables)
	SSair.setup_template_machinery(atmos_machines)
	GLOB.reloading_map = FALSE

/datum/mapGenerator/repair
	modules = list(/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel,
	/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Repair: Floor"

/datum/mapGenerator/repair/delete_walls
	modules = list(/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel/flatten,
	/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Repair: Floor: Flatten Walls"

/datum/mapGenerator/repair/enclose_room
	modules = list(/datum/mapGeneratorModule/bottomLayer/repairFloorPlasteel/flatten,
	/datum/mapGeneratorModule/border/normalWalls,
	/datum/mapGeneratorModule/bottomLayer/repressurize)
	buildmode_name = "Repair: Generate Aired Room"

/datum/mapGenerator/repair/reload_station_map
	modules = list(/datum/mapGeneratorModule/bottomLayer/massdelete/no_delete_mobs)
	var/x_low = 0
	var/x_high = 0
	var/y_low = 0
	var/y_high = 0
	var/z = 0
	var/cleanload = FALSE
	var/datum/mapGeneratorModule/reload_station_map/loader
	buildmode_name = "Repair: Reload Block \[DO NOT USE\]"

/datum/mapGenerator/repair/reload_station_map/clean
	buildmode_name = "Repair: Reload Block - Mass Delete"
	cleanload = TRUE

/datum/mapGenerator/repair/reload_station_map/clean/in_place
	modules = list(/datum/mapGeneratorModule/bottomLayer/massdelete/regeneration_delete)
	buildmode_name = "Repair: Reload Block - Mass Delete - In Place"

/datum/mapGenerator/repair/reload_station_map/defineRegion(turf/start, turf/end)
	. = ..()
	if(!is_station_level(start.z) || !is_station_level(end.z))
		return
	x_low = min(start.x, end.x)
	y_low = min(start.y, end.y)
	x_high = max(start.x, end.x)
	y_high = max(start.y, end.y)
	z = SSmapping.station_start

GLOBAL_VAR_INIT(reloading_map, FALSE)

/datum/mapGenerator/repair/reload_station_map/generate(clean = cleanload)
	if(!loader)
		loader = new
	if(cleanload)
		..()			//Trigger mass deletion.
	modules |= loader
	syncModules()
	loader.generate()
