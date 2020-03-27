SUBSYSTEM_DEF(ping)
	name = "Ping"
	priority = FIRE_PRIORITY_PING
	wait = 3 SECONDS
	flags = SS_NO_INIT
	runlevels = RUNLEVEL_LOBBY | RUNLEVEL_SETUP | RUNLEVEL_GAME | RUNLEVEL_POSTGAME

	var/list/currentrun = list()

/datum/controller/subsystem/ping/stat_entry()
	..("P:[GLOB.clients.len]")


/datum/controller/subsystem/ping/fire(resumed = 0)
	if (!resumed)
		src.currentrun = GLOB.clients.Copy()

	//cache for sanic speed (lists are references anyways)
	var/list/currentrun = src.currentrun

	while (currentrun.len)
		var/client/C = currentrun[currentrun.len]
		currentrun.len--

		if (!C || !C.chatOutput || !C.chatOutput.loaded)
			if (MC_TICK_CHECK)
				return
			continue

		// softPang isn't handled anywhere but it'll always reset the opts.lastPang.
		C.chatOutput.ehjax_send(data = C.is_afk(29) ? "softPang" : "pang")
		if (MC_TICK_CHECK)
			return
