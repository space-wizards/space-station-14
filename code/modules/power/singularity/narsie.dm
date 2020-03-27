/obj/singularity/narsie //Moving narsie to a child object of the singularity so it can be made to function differently. --NEO
	name = "Nar'Sie's Avatar"
	desc = "Your mind begins to bubble and ooze as it tries to comprehend what it sees."
	icon = 'icons/obj/magic_terror.dmi'
	pixel_x = -89
	pixel_y = -85
	density = FALSE
	current_size = 9 //It moves/eats like a max-size singulo, aside from range. --NEO
	contained = 0 //Are we going to move around?
	dissipate = 0 //Do we lose energy over time?
	move_self = 1 //Do we move on our own?
	grav_pull = 5 //How many tiles out do we pull?
	consume_range = 6 //How many tiles out do we eat
	light_power = 0.7
	light_range = 15
	light_color = rgb(255, 0, 0)
	gender = FEMALE

/obj/singularity/narsie/large
	name = "Nar'Sie"
	icon = 'icons/obj/narsie.dmi'
	// Pixel stuff centers Narsie.
	pixel_x = -236
	pixel_y = -256
	current_size = 12
	grav_pull = 10
	consume_range = 12 //How many tiles out do we eat

/obj/singularity/narsie/large/Initialize()
	. = ..()
	send_to_playing_players("<span class='narsie'>NAR'SIE HAS RISEN</span>")
	sound_to_playing_players('sound/creatures/narsie_rises.ogg')

	var/area/A = get_area(src)
	if(A)
		var/mutable_appearance/alert_overlay = mutable_appearance('icons/effects/cult_effects.dmi', "ghostalertsie")
		notify_ghosts("Nar'Sie has risen in \the [A.name]. Reach out to the Geometer to be given a new shell for your soul.", source = src, alert_overlay = alert_overlay, action=NOTIFY_ATTACK)
	INVOKE_ASYNC(src, .proc/narsie_spawn_animation)
	UnregisterSignal(src, COMSIG_ATOM_BSA_BEAM) //set up in /singularity/Initialize()

/obj/singularity/narsie/large/cult  // For the new cult ending, guaranteed to end the round within 3 minutes
	var/list/souls_needed = list()
	var/soul_goal = 0
	var/souls = 0
	var/resolved = FALSE

/obj/singularity/narsie/large/cult/Initialize()
	. = ..()
	GLOB.cult_narsie = src
	var/list/all_cults = list()
	for(var/datum/antagonist/cult/C in GLOB.antagonists)
		if(!C.owner)
			continue
		all_cults |= C.cult_team
	for(var/datum/team/cult/T in all_cults)
		deltimer(T.blood_target_reset_timer)
		T.blood_target = src
		var/datum/objective/eldergod/summon_objective = locate() in T.objectives
		if(summon_objective)
			summon_objective.summoned = TRUE
	for(var/datum/mind/cult_mind in SSticker.mode.cult)
		if(isliving(cult_mind.current))
			var/mob/living/L = cult_mind.current
			L.narsie_act()
	for(var/mob/living/player in GLOB.player_list)
		if(player.stat != DEAD && player.loc && is_station_level(player.loc.z) && !iscultist(player) && !isanimal(player))
			souls_needed[player] = TRUE
	soul_goal = round(1 + LAZYLEN(souls_needed) * 0.75)
	INVOKE_ASYNC(GLOBAL_PROC, .proc/begin_the_end)

/proc/begin_the_end()
	sleep(50)
	if(QDELETED(GLOB.cult_narsie)) // uno
		priority_announce("Status report? We detected a anomaly, but it disappeared almost immediately.","Central Command Higher Dimensional Affairs", 'sound/misc/notice1.ogg')
		GLOB.cult_narsie = null
		sleep(20)
		INVOKE_ASYNC(GLOBAL_PROC, .proc/cult_ending_helper, 2)
		return
	priority_announce("An acausal dimensional event has been detected in your sector. Event has been flagged EXTINCTION-CLASS. Directing all available assets toward simulating solutions. SOLUTION ETA: 60 SECONDS.","Central Command Higher Dimensional Affairs", 'sound/misc/airraid.ogg')
	sleep(500)
	if(QDELETED(GLOB.cult_narsie)) // dos
		priority_announce("Simulations aborted, sensors report that the acasual event is normalizing. Good work, crew.","Central Command Higher Dimensional Affairs", 'sound/misc/notice1.ogg')
		GLOB.cult_narsie = null
		sleep(20)
		INVOKE_ASYNC(GLOBAL_PROC, .proc/cult_ending_helper, 2)
		return
	priority_announce("Simulations on acausal dimensional event complete. Deploying solution package now. Deployment ETA: ONE MINUTE. ","Central Command Higher Dimensional Affairs")
	sleep(50)
	set_security_level("delta")
	SSshuttle.registerHostileEnvironment(GLOB.cult_narsie)
	SSshuttle.lockdown = TRUE
	sleep(600)
	if(QDELETED(GLOB.cult_narsie)) // tres
		priority_announce("Normalization detected! Abort the solution package!","Central Command Higher Dimensional Affairs", 'sound/misc/notice1.ogg')
		GLOB.cult_narsie = null
		sleep(20)
		set_security_level("red")
		SSshuttle.clearHostileEnvironment()
		SSshuttle.lockdown = FALSE
		INVOKE_ASYNC(GLOBAL_PROC, .proc/cult_ending_helper, 2)
		return
	if(GLOB.cult_narsie.resolved == FALSE)
		GLOB.cult_narsie.resolved = TRUE
		sound_to_playing_players('sound/machines/alarm.ogg')
		addtimer(CALLBACK(GLOBAL_PROC, .proc/cult_ending_helper), 120)

/obj/singularity/narsie/large/cult/Destroy()
	send_to_playing_players("<span class='narsie'>\"<b>[pick("Nooooo...", "Not die. How-", "Die. Mort-", "Sas tyen re-")]\"</b></span>")
	sound_to_playing_players('sound/magic/demon_dies.ogg', 50)
	var/list/all_cults = list()
	for(var/datum/antagonist/cult/C in GLOB.antagonists)
		if(!C.owner)
			continue
		all_cults |= C.cult_team
	for(var/datum/team/cult/T in all_cults)
		var/datum/objective/eldergod/summon_objective = locate() in T.objectives
		if(summon_objective)
			summon_objective.summoned = FALSE
			summon_objective.killed = TRUE
	return ..()

/proc/ending_helper()
	SSticker.force_ending = 1

/proc/cult_ending_helper(var/ending_type = 0)
	if(ending_type == 2) //narsie fukkin died
		Cinematic(CINEMATIC_CULT_FAIL,world,CALLBACK(GLOBAL_PROC,/proc/ending_helper))
	else if(ending_type) //no explosion
		Cinematic(CINEMATIC_CULT,world,CALLBACK(GLOBAL_PROC,/proc/ending_helper))
	else // explosion
		Cinematic(CINEMATIC_CULT_NUKE,world,CALLBACK(GLOBAL_PROC,/proc/ending_helper))

//ATTACK GHOST IGNORING PARENT RETURN VALUE
/obj/singularity/narsie/large/attack_ghost(mob/dead/observer/user as mob)
	makeNewConstruct(/mob/living/simple_animal/hostile/construct/harvester, user, cultoverride = TRUE, loc_override = src.loc)

/obj/singularity/narsie/process()
	eat()
	if(!target || prob(5))
		pickcultist()
	move()
	if(prob(25))
		mezzer()


/obj/singularity/narsie/Bump(atom/A)
	var/turf/T = get_turf(A)
	if(T == loc)
		T = get_step(A, A.dir) //please don't slam into a window like a bird, Nar'Sie
	forceMove(T)


/obj/singularity/narsie/mezzer()
	for(var/mob/living/carbon/M in viewers(consume_range, src))
		if(M.stat == CONSCIOUS)
			if(!iscultist(M))
				to_chat(M, "<span class='cultsmall'>You feel conscious thought crumble away in an instant as you gaze upon [src.name]...</span>")
				M.apply_effect(60, EFFECT_STUN)


/obj/singularity/narsie/consume(atom/A)
	if(isturf(A))
		A.narsie_act()


/obj/singularity/narsie/ex_act() //No throwing bombs at her either.
	return


/obj/singularity/narsie/proc/pickcultist() //Narsie rewards her cultists with being devoured first, then picks a ghost to follow.
	var/list/cultists = list()
	var/list/noncultists = list()

	for(var/mob/living/carbon/food in GLOB.alive_mob_list) //we don't care about constructs or cult-Ians or whatever. cult-monkeys are fair game i guess
		var/turf/pos = get_turf(food)
		if(!pos || (pos.z != z))
			continue

		if(iscultist(food))
			cultists += food
		else
			noncultists += food

		if(cultists.len) //cultists get higher priority
			acquire(pick(cultists))
			return

		if(noncultists.len)
			acquire(pick(noncultists))
			return

	//no living humans, follow a ghost instead.
	for(var/mob/dead/observer/ghost in GLOB.player_list)
		var/turf/pos = get_turf(ghost)
		if(!pos || (pos.z != z))
			continue
		cultists += ghost
	if(cultists.len)
		acquire(pick(cultists))
		return


/obj/singularity/narsie/proc/acquire(atom/food)
	if(food == target)
		return
	to_chat(target, "<span class='cultsmall'>NAR'SIE HAS LOST INTEREST IN YOU.</span>")
	target = food
	if(ishuman(target))
		to_chat(target, "<span class='cult'>NAR'SIE HUNGERS FOR YOUR SOUL.</span>")
	else
		to_chat(target, "<span class='cult'>NAR'SIE HAS CHOSEN YOU TO LEAD HER TO HER NEXT MEAL.</span>")

//Wizard narsie
/obj/singularity/narsie/wizard
	grav_pull = 0

/obj/singularity/narsie/wizard/eat()
//	if(defer_powernet_rebuild != 2)
//		defer_powernet_rebuild = 1
	for(var/atom/X in urange(consume_range,src,1))
		if(isturf(X) || ismovableatom(X))
			consume(X)
//	if(defer_powernet_rebuild != 2)
//		defer_powernet_rebuild = 0
	return


/obj/singularity/narsie/proc/narsie_spawn_animation()
	icon = 'icons/obj/narsie_spawn_anim.dmi'
	setDir(SOUTH)
	move_self = 0
	flick("narsie_spawn_anim",src)
	sleep(11)
	move_self = 1
	icon = initial(icon)



