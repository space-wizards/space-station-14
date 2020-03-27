GLOBAL_LIST_INIT(high_priority_sentience, typecacheof(list(
	/mob/living/simple_animal/pet,
	/mob/living/simple_animal/parrot,
	/mob/living/simple_animal/hostile/lizard,
	/mob/living/simple_animal/sloth,
	/mob/living/simple_animal/mouse/brown/Tom,
	/mob/living/simple_animal/hostile/retaliate/goat,
	/mob/living/simple_animal/chicken,
	/mob/living/simple_animal/cow,
	/mob/living/simple_animal/hostile/retaliate/bat,
	/mob/living/simple_animal/hostile/carp/cayenne,
	/mob/living/simple_animal/butterfly,
	/mob/living/simple_animal/hostile/retaliate/poison/snake,
	/mob/living/simple_animal/hostile/retaliate/goose/vomit,
	/mob/living/simple_animal/bot/mulebot,
	/mob/living/simple_animal/bot/secbot/beepsky
)))

/datum/round_event_control/sentience
	name = "Random Human-level Intelligence"
	typepath = /datum/round_event/ghost_role/sentience
	weight = 10


/datum/round_event/ghost_role/sentience
	minimum_required = 1
	role_name = "random animal"
	var/animals = 1
	var/one = "one"
	fakeable = TRUE

/datum/round_event/ghost_role/sentience/announce(fake)
	var/sentience_report = ""

	var/data = pick("scans from our long-range sensors", "our sophisticated probabilistic models", "our omnipotence", "the communications traffic on your station", "energy emissions we detected", "\[REDACTED\]")
	var/pets = pick("animals/bots", "bots/animals", "pets", "simple animals", "lesser lifeforms", "\[REDACTED\]")
	var/strength = pick("human", "moderate", "lizard", "security", "command", "clown", "low", "very low", "\[REDACTED\]")

	sentience_report += "Based on [data], we believe that [one] of the station's [pets] has developed [strength] level intelligence, and the ability to communicate."

	priority_announce(sentience_report,"[command_name()] Medium-Priority Update")

/datum/round_event/ghost_role/sentience/spawn_role()
	var/list/mob/dead/observer/candidates
	candidates = get_candidates(ROLE_ALIEN, null, ROLE_ALIEN)

	// find our chosen mob to breathe life into
	// Mobs have to be simple animals, mindless, on station, and NOT holograms.
	// prioritize starter animals that people will recognise


	var/list/potential = list()

	var/list/hi_pri = list()
	var/list/low_pri = list()

	for(var/mob/living/simple_animal/L in GLOB.alive_mob_list)
		var/turf/T = get_turf(L)
		if(!T || !is_station_level(T.z))
			continue
		if((L in GLOB.player_list) || L.mind || (L.flags_1 & HOLOGRAM_1))
			continue
		if(is_type_in_typecache(L, GLOB.high_priority_sentience))
			hi_pri += L
		else
			low_pri += L

	shuffle_inplace(hi_pri)
	shuffle_inplace(low_pri)

	potential = hi_pri + low_pri

	if(!potential.len)
		return WAITING_FOR_SOMETHING
	if(!candidates.len)
		return NOT_ENOUGH_PLAYERS

	var/spawned_animals = 0
	while(spawned_animals < animals && candidates.len && potential.len)
		var/mob/living/simple_animal/SA = popleft(potential)
		var/mob/dead/observer/SG = pick_n_take(candidates)

		spawned_animals++

		SA.key = SG.key

		SA.grant_all_languages(TRUE, FALSE, FALSE)

		SA.sentience_act()

		SA.maxHealth = max(SA.maxHealth, 200)
		SA.health = SA.maxHealth
		SA.del_on_death = FALSE

		spawned_mobs += SA

		to_chat(SA, "<span class='userdanger'>Hello world!</span>")
		to_chat(SA, "<span class='warning'>Due to freak radiation and/or chemicals \
			and/or lucky chance, you have gained human level intelligence \
			and the ability to speak and understand human language!</span>")

	return SUCCESSFUL_SPAWN

/datum/round_event_control/sentience/all
	name = "Station-wide Human-level Intelligence"
	typepath = /datum/round_event/ghost_role/sentience/all
	weight = 0

/datum/round_event/ghost_role/sentience/all
	one = "all"
	animals = INFINITY // as many as there are ghosts and animals
	// cockroach pride, station wide
