/datum/unit_test/subsystem_init/Run()
	for(var/i in Master.subsystems)
		var/datum/controller/subsystem/ss = i
		if(ss.flags & SS_NO_INIT)
			continue
		if(!ss.initialized)
			Fail("[ss]([ss.type]) is a subsystem meant to initialize but doesn't get set as initialized.")
