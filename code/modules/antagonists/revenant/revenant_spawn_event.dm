#define REVENANT_SPAWN_THRESHOLD 20

/datum/round_event_control/revenant
	name = "Spawn Revenant" // Did you mean 'griefghost'?
	typepath = /datum/round_event/ghost_role/revenant
	weight = 7
	max_occurrences = 1
	min_players = 5


/datum/round_event/ghost_role/revenant
	var/ignore_mobcheck = FALSE
	role_name = "revenant"

/datum/round_event/ghost_role/revenant/New(my_processing = TRUE, new_ignore_mobcheck = FALSE)
	..()
	ignore_mobcheck = new_ignore_mobcheck

/datum/round_event/ghost_role/revenant/spawn_role()
	if(!ignore_mobcheck)
		var/deadMobs = 0
		for(var/mob/M in GLOB.dead_mob_list)
			deadMobs++
		if(deadMobs < REVENANT_SPAWN_THRESHOLD)
			message_admins("Event attempted to spawn a revenant, but there were only [deadMobs]/[REVENANT_SPAWN_THRESHOLD] dead mobs.")
			return WAITING_FOR_SOMETHING

	var/list/candidates = get_candidates(ROLE_REVENANT, null, ROLE_REVENANT)
	if(!candidates.len)
		return NOT_ENOUGH_PLAYERS

	var/mob/dead/observer/selected = pick_n_take(candidates)

	var/list/spawn_locs = list()

	for(var/mob/living/L in GLOB.dead_mob_list) //look for any dead bodies
		var/turf/T = get_turf(L)
		if(T && is_station_level(T.z))
			spawn_locs += T
	if(!spawn_locs.len || spawn_locs.len < 15) //look for any morgue trays, crematoriums, ect if there weren't alot of dead bodies on the station to pick from
		for(var/obj/structure/bodycontainer/bc in GLOB.bodycontainers)
			var/turf/T = get_turf(bc)
			if(T && is_station_level(T.z))
				spawn_locs += T
	if(!spawn_locs.len) //If we can't find any valid spawnpoints, try the carp spawns
		for(var/obj/effect/landmark/carpspawn/L in GLOB.landmarks_list)
			if(isturf(L.loc))
				spawn_locs += L.loc
	if(!spawn_locs.len) //If we can't find either, just spawn the revenant at the player's location
		spawn_locs += get_turf(selected)
	if(!spawn_locs.len) //If we can't find THAT, then just give up and cry
		return MAP_ERROR

	var/mob/living/simple_animal/revenant/revvie = new(pick(spawn_locs))
	revvie.key = selected.key
	message_admins("[ADMIN_LOOKUPFLW(revvie)] has been made into a revenant by an event.")
	log_game("[key_name(revvie)] was spawned as a revenant by an event.")
	spawned_mobs += revvie
	return SUCCESSFUL_SPAWN
