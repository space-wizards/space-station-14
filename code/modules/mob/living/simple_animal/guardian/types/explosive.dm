#define UNREGISTER_BOMB_SIGNALS(A) \
	do { \
		UnregisterSignal(A, boom_signals); \
		UnregisterSignal(A, COMSIG_PARENT_EXAMINE); \
	} while (0)

//Bomb
/mob/living/simple_animal/hostile/guardian/bomb
	melee_damage_lower = 15
	melee_damage_upper = 15
	damage_coeff = list(BRUTE = 0.6, BURN = 0.6, TOX = 0.6, CLONE = 0.6, STAMINA = 0, OXY = 0.6)
	range = 13
	playstyle_string = "<span class='holoparasite'>As an <b>explosive</b> type, you have moderate close combat abilities, may explosively teleport targets on attack, and are capable of converting nearby items and objects into disguised bombs via alt click.</span>"
	magic_fluff_string = "<span class='holoparasite'>..And draw the Scientist, master of explosive death.</span>"
	tech_fluff_string = "<span class='holoparasite'>Boot sequence complete. Explosive modules active. Holoparasite swarm online.</span>"
	carp_fluff_string = "<span class='holoparasite'>CARP CARP CARP! Caught one! It's an explosive carp! Boom goes the fishy.</span>"
	var/bomb_cooldown = 0
	var/static/list/boom_signals = list(COMSIG_PARENT_ATTACKBY, COMSIG_ATOM_BUMPED, COMSIG_ATOM_ATTACK_HAND)

/mob/living/simple_animal/hostile/guardian/bomb/Stat()
	..()
	if(statpanel("Status"))
		if(bomb_cooldown >= world.time)
			stat(null, "Bomb Cooldown Remaining: [DisplayTimeText(bomb_cooldown - world.time)]")

/mob/living/simple_animal/hostile/guardian/bomb/AttackingTarget()
	. = ..()
	if(. && prob(40) && isliving(target))
		var/mob/living/M = target
		if(!M.anchored && M != summoner && !hasmatchingsummoner(M))
			new /obj/effect/temp_visual/guardian/phase/out(get_turf(M))
			do_teleport(M, M, 10, channel = TELEPORT_CHANNEL_BLUESPACE)
			for(var/mob/living/L in range(1, M))
				if(hasmatchingsummoner(L)) //if the summoner matches don't hurt them
					continue
				if(L != src && L != summoner)
					L.apply_damage(15, BRUTE)
			new /obj/effect/temp_visual/explosion(get_turf(M))

/mob/living/simple_animal/hostile/guardian/bomb/AltClickOn(atom/movable/A)
	if(!istype(A))
		return
	if(loc == summoner)
		to_chat(src, "<span class='danger'><B>You must be manifested to create bombs!</B></span>")
		return
	if(isobj(A) && Adjacent(A))
		if(bomb_cooldown <= world.time && !stat)
			to_chat(src, "<span class='danger'><B>Success! Bomb armed!</B></span>")
			bomb_cooldown = world.time + 200
			RegisterSignal(A, COMSIG_PARENT_EXAMINE, .proc/display_examine)
			RegisterSignal(A, boom_signals, .proc/kaboom)
			addtimer(CALLBACK(src, .proc/disable, A), 600, TIMER_UNIQUE|TIMER_OVERRIDE)
		else
			to_chat(src, "<span class='danger'><B>Your powers are on cooldown! You must wait 20 seconds between bombs.</B></span>")

/mob/living/simple_animal/hostile/guardian/bomb/proc/kaboom(atom/source, mob/living/explodee)
	if(!istype(explodee))
		return
	if(explodee == src || explodee == summoner || hasmatchingsummoner(explodee))
		return
	to_chat(explodee, "<span class='danger'><B>[source] was boobytrapped!</B></span>")
	to_chat(src, "<span class='danger'><B>Success! Your trap caught [explodee]</B></span>")
	var/turf/T = get_turf(source)
	playsound(T,'sound/effects/explosion2.ogg', 200, TRUE)
	new /obj/effect/temp_visual/explosion(T)
	explodee.ex_act(EXPLODE_HEAVY)
	UNREGISTER_BOMB_SIGNALS(source)

/mob/living/simple_animal/hostile/guardian/bomb/proc/disable(atom/A)
	to_chat(src, "<span class='danger'><B>Failure! Your trap didn't catch anyone this time.</B></span>")
	UNREGISTER_BOMB_SIGNALS(A)

/mob/living/simple_animal/hostile/guardian/bomb/proc/display_examine(datum/source, mob/user, text)
	text += "<span class='holoparasite'>It glows with a strange <font color=\"[namedatum.colour]\">light</font>!</span>"

#undef UNREGISTER_BOMB_SIGNALS
