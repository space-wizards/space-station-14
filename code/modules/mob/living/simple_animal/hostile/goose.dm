#define GOOSE_SATIATED 50
/mob/living/simple_animal/hostile/retaliate/goose
	name = "goose"
	desc = "It's loose"
	icon_state = "goose" // sprites by cogwerks from goonstation, used with permission
	icon_living = "goose"
	icon_dead = "goose_dead"
	mob_biotypes = MOB_ORGANIC|MOB_BEAST
	speak_chance = 0
	turns_per_move = 5
	butcher_results = list(/obj/item/reagent_containers/food/snacks/meat/slab = 2)
	response_help_continuous = "pets"
	response_help_simple = "pet"
	response_disarm_continuous = "gently pushes aside"
	response_disarm_simple = "gently push aside"
	response_harm_continuous = "kicks"
	response_harm_simple = "kick"
	emote_taunt = list("hisses")
	taunt_chance = 30
	speed = 0
	maxHealth = 25
	health = 25
	harm_intent_damage = 5
	melee_damage_lower = 5
	melee_damage_upper = 5
	attack_verb_continuous = "pecks"
	attack_verb_simple = "peck"
	attack_sound = "goose"
	speak_emote = list("honks")
	faction = list("neutral")
	attack_same = TRUE
	gold_core_spawnable = HOSTILE_SPAWN
	var/random_retaliate = TRUE
	var/icon_vomit_start = "vomit_start"
	var/icon_vomit = "vomit"
	var/icon_vomit_end = "vomit_end"
	var/message_cooldown = 0
	var/list/nummies = list()
	var/choking = FALSE

/mob/living/simple_animal/hostile/retaliate/goose/Initialize()
	. = ..()
	RegisterSignal(src, COMSIG_MOVABLE_MOVED, .proc/goosement)

/mob/living/simple_animal/hostile/retaliate/goose/proc/goosement(atom/movable/AM, OldLoc, Dir, Forced)
	if(stat == DEAD)
		return
	nummies.Cut()
	nummies += loc.contents
	if(prob(5) && random_retaliate)
		Retaliate()		

/mob/living/simple_animal/hostile/retaliate/goose/handle_automated_action()
	if(length(nummies))
		var/obj/item/E = locate() in nummies
		if(E && E.loc == loc)
			feed(E)
		nummies -= E

/mob/living/simple_animal/hostile/retaliate/goose/vomit/handle_automated_action()
	if(length(nummies))
		var/obj/item/E = pick(nummies)
		if(!(E.custom_materials && E.custom_materials[getmaterialref(/datum/material/plastic)]))
			nummies -= E // remove non-plastic item from queue
			E = locate(/obj/item/reagent_containers/food) in nummies // find food
		if(E && E.loc == loc)
			feed(E)
		nummies -= E

/mob/living/simple_animal/hostile/retaliate/goose/proc/feed(obj/item/suffocator)
	if(stat == DEAD || choking) // plapatin I swear to god
		return FALSE
	if(suffocator.custom_materials && suffocator.custom_materials[getmaterialref(/datum/material/plastic)]) // dumb goose'll swallow food or drink with plastic in it
		visible_message("<span class='danger'>[src] hungrily gobbles up \the [suffocator]! </span>")
		visible_message("<span class='boldwarning'>[src] is choking on \the [suffocator]! </span>")
		suffocator.forceMove(src)
		choke(suffocator)
		choking = TRUE
		return TRUE

/mob/living/simple_animal/hostile/retaliate/goose/vomit
	name = "Birdboat"
	real_name = "Birdboat"
	desc = "It's a sick-looking goose, probably ate too much maintenance trash. Best not to move it around too much."
	gender = MALE
	response_help_continuous = "pets"
	response_help_simple = "pet"
	response_disarm_continuous = "gently pushes aside"
	response_disarm_simple = "gently push aside"
	response_harm_continuous = "kicks"
	response_harm_simple = "kick"
	gold_core_spawnable = NO_SPAWN
	random_retaliate = FALSE
	var/vomiting = FALSE
	var/vomitCoefficient = 1
	var/vomitTimeBonus = 0
	var/datum/action/cooldown/vomit/goosevomit

/mob/living/simple_animal/hostile/retaliate/goose/vomit/Initialize()
	. = ..()
	goosevomit = new
	goosevomit.Grant(src)
	// 5% chance every round to have anarchy mode deadchat control on birdboat.
	if(prob(5))
		desc = "[initial(desc)] It's waddling more than usual. It seems to be possessed."
		deadchat_plays_goose()

/mob/living/simple_animal/hostile/retaliate/goose/vomit/Destroy()
	UnregisterSignal(src, COMSIG_MOVABLE_MOVED)
	QDEL_NULL(goosevomit)
	return ..()

/mob/living/simple_animal/hostile/retaliate/goose/vomit/examine(user)
	. = ..()
	. += "<span class='notice'>Somehow, it still looks hungry.</span>"

/mob/living/simple_animal/hostile/retaliate/goose/attackby(obj/item/O, mob/user)
	. = ..()
	if(feed(O))
		return TRUE

/mob/living/simple_animal/hostile/retaliate/goose/vomit/feed(obj/item/reagent_containers/food/tasty)
	. = ..()
	if(. || !istype(tasty))
		return FALSE
	if (contents.len > GOOSE_SATIATED)
		if(message_cooldown < world.time)
			visible_message("<span class='notice'>[src] looks too full to eat \the [tasty]!</span>")
			message_cooldown = world.time + 5 SECONDS
		return FALSE
	if (tasty.foodtype & GROSS)
		visible_message("<span class='notice'>[src] hungrily gobbles up \the [tasty]!</span>")
		tasty.forceMove(src)
		playsound(src,'sound/items/eatfood.ogg', 70, TRUE)
		vomitCoefficient += 3
		vomitTimeBonus += 2
		return TRUE
	else
		if(message_cooldown < world.time)
			visible_message("<span class='notice'>[src] refuses to eat \the [tasty].</span>")
			message_cooldown = world.time + 5 SECONDS
			return FALSE

/mob/living/simple_animal/hostile/retaliate/goose/proc/choke(obj/item/reagent_containers/food/plastic)
	if(stat == DEAD || choking)
		return
	addtimer(CALLBACK(src, .proc/suffocate), 300)

/mob/living/simple_animal/hostile/retaliate/goose/vomit/choke(obj/item/reagent_containers/food/plastic)
	if(stat == DEAD || choking)
		return
	if(prob(25))
		visible_message("<span class='warning'>[src] is gagging on \the [plastic]!</span>")
		emote("me", 1, "gags!")
		addtimer(CALLBACK(src, .proc/vomit), 300)
	else
		addtimer(CALLBACK(src, .proc/suffocate), 300)

/mob/living/simple_animal/hostile/retaliate/goose/Life()
	. = ..()
	if(choking && !stat)
		do_jitter_animation(50)
		if(prob(20))
			emote("gasp")

/mob/living/simple_animal/hostile/retaliate/goose/proc/suffocate()
	if(!choking)
		return
	deathmessage = "lets out one final oxygen-deprived honk before they go limp and lifeless.."
	death()

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/vomit()
	if (stat == DEAD)
		return
	var/turf/T = get_turf(src)
	var/obj/item/reagent_containers/food/consumed = locate() in contents //Barf out a single food item from our guts
	choking = FALSE // assume birdboat is vomiting out whatever he was choking on
	if (prob(50) && consumed)
		barf_food(consumed)
	else
		playsound(T, 'sound/effects/splat.ogg', 50, TRUE)
		T.add_vomit_floor(src)

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/barf_food(atom/A, hard = FALSE)
	if (stat == DEAD)
		return
	if(!istype(A, /obj/item/reagent_containers/food))
		return
	var/turf/currentTurf = get_turf(src)
	var/obj/item/reagent_containers/food/consumed = A
	consumed.forceMove(currentTurf)
	var/destination = get_edge_target_turf(currentTurf, pick(GLOB.alldirs)) //Pick a random direction to toss them in
	var/throwRange = hard ? rand(2,8) : 1
	consumed.safe_throw_at(destination, throwRange, 2) //Thow the food at a random tile 1 spot away
	sleep(2)
	if (QDELETED(src) || QDELETED(consumed))
		return
	currentTurf = get_turf(consumed)
	currentTurf.add_vomit_floor(src)
	playsound(currentTurf, 'sound/effects/splat.ogg', 50, TRUE)

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/vomit_prestart(duration)
	flick("vomit_start",src)
	addtimer(CALLBACK(src, .proc/vomit_start, duration), 13) //13 is the length of the vomit_start animation in gooseloose.dmi

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/vomit_start(duration)
	vomiting = TRUE
	icon_state = "vomit"
	vomit()
	addtimer(CALLBACK(src, .proc/vomit_preend), duration)

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/vomit_preend()
	for (var/obj/item/consumed in contents) //Get rid of any food left in the poor thing
		barf_food(consumed, TRUE)
		sleep(1)
		if (QDELETED(src))
			return
	vomit_end()

/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/vomit_end()
	flick("vomit_end",src)
	vomiting = FALSE
	icon_state = initial(icon_state)

/mob/living/simple_animal/hostile/retaliate/goose/vomit/goosement(atom/movable/AM, OldLoc, Dir, Forced)
	. = ..()
	if(vomiting)
		vomit() // its supposed to keep vomiting if you move
		return
	if(prob(vomitCoefficient * 0.2))
		vomit_prestart(vomitTimeBonus + 25)
		vomitCoefficient = 1
		vomitTimeBonus = 0

/// A proc to make it easier for admins to make the goose playable by deadchat.
/mob/living/simple_animal/hostile/retaliate/goose/vomit/proc/deadchat_plays_goose()
	stop_automated_movement = TRUE
	AddComponent(/datum/component/deadchat_control, ANARCHY_MODE, list(
	 "up" = CALLBACK(GLOBAL_PROC, .proc/_step, src, NORTH),
	 "down" = CALLBACK(GLOBAL_PROC, .proc/_step, src, SOUTH),
	 "left" = CALLBACK(GLOBAL_PROC, .proc/_step, src, WEST),
	 "right" = CALLBACK(GLOBAL_PROC, .proc/_step, src, EAST),
	 "vomit" = CALLBACK(src, .proc/vomit_prestart, 25)), 12 SECONDS, 4 SECONDS)

/datum/action/cooldown/vomit
	name = "Vomit"
	check_flags = AB_CHECK_CONSCIOUS
	button_icon_state = "vomit"
	icon_icon = 'icons/mob/animal.dmi'
	cooldown_time = 250

/datum/action/cooldown/vomit/Trigger()
	if(!..())
		return FALSE
	if(!istype(owner, /mob/living/simple_animal/hostile/retaliate/goose/vomit))
		return FALSE
	var/mob/living/simple_animal/hostile/retaliate/goose/vomit/vomit = owner
	if(!vomit.vomiting)
		vomit.vomit_prestart(vomit.vomitTimeBonus + 25)
		vomit.vomitCoefficient = 1
		vomit.vomitTimeBonus = 0
	return TRUE
