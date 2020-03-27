/*

Swarmer Beacon

A strange machine appears anywhere a normal lavaland mob can it produces a swarmer at a rate of
1/15 seconds, until there are GetTotalAISwarmerCap()/2 swarmers, after this it is up to the swarmers themselves to
increase their population (it will repopulate them should they fall under GetTotalAISwarmerCap()/2 again)

tl;dr A million of the little hellraisers spawn (controlled by AI) and try to eat mining

Loot: Not much, besides a shit load of artificial bluespace crystals, Oh and mining doesn't get eaten
that's a plus I suppose.

Difficulty: Special

*/

GLOBAL_LIST_EMPTY(AISwarmers)
GLOBAL_LIST_EMPTY(AISwarmersByType)//AISwarmersByType[.../resource] = list(1st, 2nd, nth), AISwarmersByType[../ranged] = list(1st, 2nd, nth) etc.
GLOBAL_LIST_INIT(AISwarmerCapsByType, list(/mob/living/simple_animal/hostile/swarmer/ai/resource = 30, /mob/living/simple_animal/hostile/swarmer/ai/ranged_combat = 20, /mob/living/simple_animal/hostile/swarmer/ai/melee_combat = 10))


//returns a type of AI swarmer that is NOT at max cap
//type order is shuffled, to prevent bias
/proc/GetUncappedAISwarmerType()
	var/static/list/swarmerTypes = subtypesof(/mob/living/simple_animal/hostile/swarmer/ai)
	LAZYINITLIST(GLOB.AISwarmersByType)
	for(var/t in shuffle(swarmerTypes))
		var/list/amount = GLOB.AISwarmersByType[t]
		if(!amount || amount.len <  GLOB.AISwarmerCapsByType[t])
			return t


//Total of all subtype caps
/proc/GetTotalAISwarmerCap()
	var/static/list/swarmerTypes = subtypesof(/mob/living/simple_animal/hostile/swarmer/ai)
	. = 0
	LAZYINITLIST(GLOB.AISwarmersByType)
	for(var/t in swarmerTypes)
		. += GLOB.AISwarmerCapsByType[t]


/mob/living/simple_animal/hostile/megafauna/swarmer_swarm_beacon
	name = "swarmer beacon"
	desc = "That name is a bit of a mouthful, but stop paying attention to your mouth they're eating everything!"
	icon = 'icons/mob/swarmer.dmi'
	icon_state = "swarmer_console"
	health = 750
	maxHealth = 750 //""""low-ish"""" HP because it's a passive boss, and the swarm itself is the real foe
	mob_biotypes = MOB_ROBOTIC
	gps_name = "Hungry Signal"
	achievement_type = /datum/award/achievement/boss/swarmer_beacon_kill
	crusher_achievement_type = /datum/award/achievement/boss/swarmer_beacon_crusher
	score_achievement_type = /datum/award/score/swarmer_beacon_score
	faction = list("mining", "boss", "swarmer")
	weather_immunities = list("lava","ash")
	stop_automated_movement = TRUE
	wander = FALSE
	layer = BELOW_MOB_LAYER
	AIStatus = AI_OFF
	del_on_death = TRUE
	var/swarmer_spawn_cooldown = 0
	var/swarmer_spawn_cooldown_amt = 150 //Deciseconds between the swarmers we spawn
	var/call_help_cooldown = 0
	var/call_help_cooldown_amt = 150 //Deciseconds between calling swarmers to help us when attacked
	var/static/list/swarmer_caps


/mob/living/simple_animal/hostile/megafauna/swarmer_swarm_beacon/Initialize()
	. = ..()
	swarmer_caps = GLOB.AISwarmerCapsByType //for admin-edits
	for(var/ddir in GLOB.cardinals)
		new /obj/structure/swarmer/blockade (get_step(src, ddir))
		var/mob/living/simple_animal/hostile/swarmer/ai/resource/R = new(loc)
		step(R, ddir) //Step the swarmers, instead of spawning them there, incase the turf is solid


/mob/living/simple_animal/hostile/megafauna/swarmer_swarm_beacon/Life()
	. = ..()
	if(.)
		var/createtype = GetUncappedAISwarmerType()
		if(createtype && world.time > swarmer_spawn_cooldown && GLOB.AISwarmers.len < (GetTotalAISwarmerCap()*0.5))
			swarmer_spawn_cooldown = world.time + swarmer_spawn_cooldown_amt
			new createtype(loc)


/mob/living/simple_animal/hostile/megafauna/swarmer_swarm_beacon/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	. = ..()
	if(. > 0 && world.time > call_help_cooldown)
		call_help_cooldown = world.time + call_help_cooldown_amt
		summon_backup(25) //long range, only called max once per 15 seconds, so it's not deathlag


//SWARMER AI
//AI versions of the swarmer mini-antag
//This is an Abstract Base, it re-enables AI, but does not give the swarmer any goals/targets
/mob/living/simple_animal/hostile/swarmer/ai
	wander = 1
	faction = list("swarmer", "mining")
	weather_immunities = list("ash") //wouldn't be fun otherwise
	AIStatus = AI_ON

/mob/living/simple_animal/hostile/swarmer/ai/Initialize()
	. = ..()
	ToggleLight() //so you can see them eating you out of house and home/shooting you/stunlocking you for eternity
	LAZYINITLIST(GLOB.AISwarmersByType[type])
	GLOB.AISwarmers += src
	GLOB.AISwarmersByType[type] += src


/mob/living/simple_animal/hostile/swarmer/ai/Destroy()
	GLOB.AISwarmers -= src
	GLOB.AISwarmersByType[type] -= src
	return ..()


/mob/living/simple_animal/hostile/swarmer/ai/SwarmerTypeToCreate()
	return GetUncappedAISwarmerType()


/mob/living/simple_animal/hostile/swarmer/ai/resource/handle_automated_action()
	. = ..()
	if(.)
		if(!stop_automated_movement)
			if(health < maxHealth*0.25)
				StartAction(100)
				RepairSelf()
				return


/mob/living/simple_animal/hostile/swarmer/ai/Move(atom/newloc)
	if(newloc)
		if(newloc.z == z) //so these actions are Z-specific
			if(islava(newloc))
				var/turf/open/lava/L = newloc
				if(!L.is_safe())
					StartAction(20)
					new /obj/structure/lattice/catwalk/swarmer_catwalk(newloc)
					return FALSE

			if(ischasm(newloc) && !throwing)
				throw_at(get_edge_target_turf(src, get_dir(src, newloc)), 7 , 3, src, FALSE) //my planet needs me
				return FALSE

		return ..()


/mob/living/simple_animal/hostile/swarmer/ai/proc/StartAction(deci = 0)
	stop_automated_movement = TRUE
	AIStatus = AI_OFF
	addtimer(CALLBACK(src, .proc/EndAction), deci)


/mob/living/simple_animal/hostile/swarmer/ai/proc/EndAction()
	stop_automated_movement = FALSE
	AIStatus = AI_ON




//RESOURCE SWARMER:
//Similar to the original Player-Swarmers, these dismantle things to obtain the metal inside
//They then use this medal to produce more swarmers or traps/barricades

/mob/living/simple_animal/hostile/swarmer/ai/resource
	search_objects = 1
	attack_all_objects = TRUE //attempt to nibble everything
	lose_patience_timeout = 150
	var/static/list/sharedWanted = typecacheof(list(/turf/closed/mineral, /turf/closed/wall)) //eat rocks and walls
	var/static/list/sharedIgnore = list()

//This handles viable things to eat/attack
//Place specific cases of AI derpiness here
//Most can be left to the automatic Gain/LosePatience() system
/mob/living/simple_animal/hostile/swarmer/ai/resource/CanAttack(atom/the_target)

	//SPECIFIC CASES:
	//Smash fulltile windows before grilles
	if(istype(the_target, /obj/structure/grille))
		for(var/obj/structure/window/rogueWindow in get_turf(the_target))
			if(rogueWindow.fulltile) //done this way because the subtypes are weird.
				the_target = rogueWindow
				break

	//GENERAL CASES:
	if(is_type_in_typecache(the_target, sharedIgnore)) //always ignore
		return FALSE
	if(is_type_in_typecache(the_target, sharedWanted)) //always eat
		return TRUE

	return ..()	//else, have a nibble, see if it's food


/mob/living/simple_animal/hostile/swarmer/ai/resource/OpenFire(atom/A)
	if(isliving(A)) //don't shoot rocks, sillies.
		..()


/mob/living/simple_animal/hostile/swarmer/ai/resource/AttackingTarget()
	if(target.swarmer_act(src))
		add_type_to_wanted(target.type)
		return TRUE
	else
		add_type_to_ignore(target.type)
		return FALSE


/mob/living/simple_animal/hostile/swarmer/ai/resource/handle_automated_action()
	. = ..()
	if(.)
		if(!stop_automated_movement)
			if(GLOB.AISwarmers.len < GetTotalAISwarmerCap() && resources >= 50)
				StartAction(100) //so they'll actually sit still and use the verbs
				CreateSwarmer()
				return

			if(resources > 5)
				if(prob(5)) //lower odds, as to prioritise reproduction
					StartAction(10) //not a typo
					CreateBarricade()
					return
				if(prob(5))
					CreateTrap()
					return


//So swarmers can learn what is and isn't food
/mob/living/simple_animal/hostile/swarmer/ai/resource/proc/add_type_to_wanted(typepath)
	if(!sharedWanted[typepath])// this and += is faster than |=
		sharedWanted += typecacheof(typepath)


/mob/living/simple_animal/hostile/swarmer/ai/resource/proc/add_type_to_ignore(typepath)
	if(!sharedIgnore[typepath])
		sharedIgnore += typecacheof(typepath)


//RANGED SWARMER
/mob/living/simple_animal/hostile/swarmer/ai/ranged_combat
	icon_state = "swarmer_ranged"
	icon_living = "swarmer_ranged"
	projectiletype = /obj/projectile/beam/laser
	projectilesound = 'sound/weapons/laser.ogg'
	check_friendly_fire = TRUE //you're supposed to protect the resource swarmers, you poop
	retreat_distance = 3
	minimum_distance = 3

/mob/living/simple_animal/hostile/swarmer/ai/ranged_combat/Aggro()
	..()
	summon_backup(15, TRUE) //Exact matching, so that goliaths don't come to aid the swarmers, that'd be silly


//MELEE SWARMER
/mob/living/simple_animal/hostile/swarmer/ai/melee_combat
	icon_state = "swarmer_melee"
	icon_living = "swarmer_melee"
	health = 60
	maxHealth = 60
	ranged = FALSE

/mob/living/simple_animal/hostile/swarmer/ai/melee_combat/Aggro()
	..()
	summon_backup(15, TRUE)


/mob/living/simple_animal/hostile/swarmer/ai/melee_combat/AttackingTarget()
	if(isliving(target))
		if(prob(35))
			StartAction(30)
			DisperseTarget(target)
		else
			var/mob/living/L = target
			L.attack_animal(src)
			L.electrocute_act(10, src, flags = SHOCK_NOGLOVES)
		return TRUE
	else
		return ..()




//SWARMER CATWALKS
//Used so they can survive lavaland better
/obj/structure/lattice/catwalk/swarmer_catwalk
	name = "swarmer catwalk"
	desc = "A catwalk-like mesh, produced by swarmers to allow them to navigate hostile terrain."
	icon = 'icons/obj/smooth_structures/swarmer_catwalk.dmi'
	icon_state = "swarmer_catwalk"
