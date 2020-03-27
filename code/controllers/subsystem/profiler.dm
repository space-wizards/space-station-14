#define PROFILER_FILENAME "profiler.json"

SUBSYSTEM_DEF(profiler)
	name = "Profiler"
	init_order = INIT_ORDER_PROFILER
	runlevels = RUNLEVELS_DEFAULT | RUNLEVEL_LOBBY
	wait = 3000
	flags = SS_NO_TICK_CHECK 
	var/fetch_cost = 0
	var/write_cost = 0

/datum/controller/subsystem/profiler/stat_entry(msg)
	msg += "F:[round(fetch_cost,1)]ms"
	msg += "|W:[round(write_cost,1)]ms"
	..(msg)

/datum/controller/subsystem/profiler/Initialize()
	if(CONFIG_GET(flag/auto_profile))
		StartProfiling()
	else
		StopProfiling() //Stop the early start from world/New
	return ..()

/datum/controller/subsystem/profiler/fire()
	if(CONFIG_GET(flag/auto_profile))
		DumpFile()

/datum/controller/subsystem/profiler/Shutdown()
	if(CONFIG_GET(flag/auto_profile))
		DumpFile()
	return ..()

/datum/controller/subsystem/profiler/proc/StartProfiling()
#if DM_BUILD < 1506 || DM_VERSION < 513
	stack_trace("Auto profiling unsupported on this byond version")
	CONFIG_SET(flag/auto_profile, FALSE)
#else
	world.Profile(PROFILE_START)
#endif

/datum/controller/subsystem/profiler/proc/StopProfiling()
#if DM_BUILD >= 1506 && DM_VERSION >= 513
	world.Profile(PROFILE_STOP)
#endif

/datum/controller/subsystem/profiler/proc/DumpFile()
#if DM_BUILD < 1506 || DM_VERSION < 513
	stack_trace("Auto profiling unsupported on this byond version")
	CONFIG_SET(flag/auto_profile, FALSE)
#else
	var/timer = TICK_USAGE_REAL
	var/current_profile_data = world.Profile(PROFILE_REFRESH,format="json")
	fetch_cost = MC_AVERAGE(fetch_cost, TICK_DELTA_TO_MS(TICK_USAGE_REAL - timer))
	CHECK_TICK
	if(!length(current_profile_data)) //Would be nice to have explicit proc to check this
		stack_trace("Warning, profiling stopped manually before dump.")
	var/json_file = file("[GLOB.log_directory]/[PROFILER_FILENAME]")
	if(fexists(json_file))
		fdel(json_file)
	timer = TICK_USAGE_REAL
	WRITE_FILE(json_file, current_profile_data)
	write_cost = MC_AVERAGE(write_cost, TICK_DELTA_TO_MS(TICK_USAGE_REAL - timer))
#endif
