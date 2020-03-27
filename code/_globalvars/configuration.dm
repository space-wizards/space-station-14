GLOBAL_REAL(config, /datum/controller/configuration)

GLOBAL_DATUM(revdata, /datum/getrev)

GLOBAL_VAR(host)
GLOBAL_VAR(station_name)
GLOBAL_VAR_INIT(game_version, "/tg/ Station 13")
GLOBAL_VAR_INIT(changelog_hash, "")
GLOBAL_VAR_INIT(hub_visibility, FALSE)

GLOBAL_VAR_INIT(ooc_allowed, TRUE)	// used with admin verbs to disable ooc - not a config option apparently
GLOBAL_VAR_INIT(dooc_allowed, TRUE)
GLOBAL_VAR_INIT(enter_allowed, TRUE)
GLOBAL_VAR_INIT(shuttle_frozen, FALSE)
GLOBAL_VAR_INIT(shuttle_left, FALSE)
GLOBAL_VAR_INIT(tinted_weldhelh, TRUE)


// Debug is used exactly once (in living.dm) but is commented out in a lot of places.  It is not set anywhere and only checked.
// Debug2 is used in conjunction with a lot of admin verbs and therefore is actually legit.
GLOBAL_VAR_INIT(Debug, FALSE)	// global debug switch
GLOBAL_VAR_INIT(Debug2, FALSE)

//This was a define, but I changed it to a variable so it can be changed in-game.(kept the all-caps definition because... code...) -Errorage
//Protecting these because the proper way to edit them is with the config/secrets
GLOBAL_VAR_INIT(MAX_EX_DEVESTATION_RANGE, 3)
GLOBAL_PROTECT(MAX_EX_DEVESTATION_RANGE)
GLOBAL_VAR_INIT(MAX_EX_HEAVY_RANGE, 7)
GLOBAL_PROTECT(MAX_EX_HEAVY_RANGE)
GLOBAL_VAR_INIT(MAX_EX_LIGHT_RANGE, 14)
GLOBAL_PROTECT(MAX_EX_LIGHT_RANGE)
GLOBAL_VAR_INIT(MAX_EX_FLASH_RANGE, 14)
GLOBAL_PROTECT(MAX_EX_FLASH_RANGE)
GLOBAL_VAR_INIT(MAX_EX_FLAME_RANGE, 14)
GLOBAL_PROTECT(MAX_EX_FLAME_RANGE)
GLOBAL_VAR_INIT(DYN_EX_SCALE, 0.5)
