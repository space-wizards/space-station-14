///////////////////////////////////////
///////////HALLOWEEN CONTENT///////////
///////////////////////////////////////


//spooky recipes

/datum/recipe/sugarcookie/spookyskull
	reagents_list = list(/datum/reagent/consumable/flour = 5, /datum/reagent/consumable/sugar = 5, /datum/reagent/consumable/milk = 5)
	items = list(
		/obj/item/reagent_containers/food/snacks/egg,
	)
	result = /obj/item/reagent_containers/food/snacks/sugarcookie/spookyskull

/datum/recipe/sugarcookie/spookycoffin
	reagents_list = list(/datum/reagent/consumable/flour = 5, /datum/reagent/consumable/sugar = 5, /datum/reagent/consumable/coffee = 5)
	items = list(
		/obj/item/reagent_containers/food/snacks/egg,
	)
	result = /obj/item/reagent_containers/food/snacks/sugarcookie/spookycoffin

//////////////////////////////
//Spookoween trapped closets//
//////////////////////////////

#define SPOOKY_SKELETON 1
#define ANGRY_FAITHLESS 2
#define SCARY_BATS 		3
#define INSANE_CLOWN	4
#define HOWLING_GHOST	5

//Spookoween variables
/obj/structure/closet
	var/trapped = 0
	var/mob/trapped_mob

/obj/structure/closet/Initialize()
	. = ..()
	if(prob(30))
		set_spooky_trap()

/obj/structure/closet/dump_contents()
	..()
	trigger_spooky_trap()

/obj/structure/closet/proc/set_spooky_trap()
	if(prob(0.1))
		trapped = INSANE_CLOWN
		return
	if(prob(1))
		trapped = ANGRY_FAITHLESS
		return
	if(prob(15))
		trapped = SCARY_BATS
		return
	if(prob(20))
		trapped = HOWLING_GHOST
		return
	else
		var/mob/living/carbon/human/H = new(loc)
		H.makeSkeleton()
		H.health = 1e5
		insert(H)
		trapped_mob = H
		trapped = SPOOKY_SKELETON
		return

/obj/structure/closet/proc/trigger_spooky_trap()
	if(!trapped)
		return

	else if(trapped == SPOOKY_SKELETON)
		visible_message("<span class='userdanger'><font size='5'>BOO!</font></span>")
		playsound(loc, 'sound/spookoween/girlscream.ogg', 300, TRUE)
		trapped = 0
		QDEL_IN(trapped_mob, 90)

	else if(trapped == HOWLING_GHOST)
		visible_message("<span class='userdanger'><font size='5'>[pick("OooOOooooOOOoOoOOooooOOOOO", "BooOOooOooooOOOO", "BOO!", "WoOOoOoooOooo")]</font></span>")
		playsound(loc, 'sound/spookoween/ghosty_wind.ogg', 300, TRUE)
		new /mob/living/simple_animal/shade/howling_ghost(loc)
		trapped = 0

	else if(trapped == SCARY_BATS)
		visible_message("<span class='userdanger'><font size='5'>Protect your hair!</font></span>")
		playsound(loc, 'sound/spookoween/bats.ogg', 300, TRUE)
		var/number = rand(1,3)
		for(var/i=0,i < number,i++)
			new /mob/living/simple_animal/hostile/retaliate/bat(loc)
		trapped = 0

	else if(trapped == ANGRY_FAITHLESS)
		visible_message("<span class='userdanger'>The closet bursts open!</span>")
		visible_message("<span class='userdanger'><font size='5'>THIS BEING RADIATES PURE EVIL! YOU BETTER RUN!!!</font></span>")
		playsound(loc, 'sound/hallucinations/wail.ogg', 300, TRUE)
		var/mob/living/simple_animal/hostile/faithless/F = new(loc)
		trapped = 0
		QDEL_IN(F, 120)

	else if(trapped == INSANE_CLOWN)
		visible_message("<span class='userdanger'><font size='5'>...</font></span>")
		playsound(loc, 'sound/spookoween/scary_clown_appear.ogg', 300, TRUE)
		spawn_atom_to_turf(/mob/living/simple_animal/hostile/clown_insane, loc, 1, FALSE)
		trapped = 0

//don't spawn in crates
/obj/structure/closet/crate/trigger_spooky_trap()
	return

/obj/structure/closet/crate/set_spooky_trap()
	return


////////////////////
//Spookoween Ghost//
////////////////////

/mob/living/simple_animal/shade/howling_ghost
	name = "ghost"
	real_name = "ghost"
	icon = 'icons/mob/mob.dmi'
	maxHealth = 1e6
	health = 1e6
	speak_emote = list("howls")
	emote_hear = list("wails","screeches")
	density = FALSE
	anchored = TRUE
	incorporeal_move = 1
	layer = 4
	var/timer = 0

/mob/living/simple_animal/shade/howling_ghost/Initialize()
	. = ..()
	icon_state = pick("ghost","ghostian","ghostian2","ghostking","ghost1","ghost2")
	icon_living = icon_state
	status_flags |= GODMODE
	timer = rand(1,15)

/mob/living/simple_animal/shade/howling_ghost/Life()
	..()
	timer--
	if(prob(20))
		roam()
	if(timer == 0)
		spooky_ghosty()
		timer = rand(1,15)

/mob/living/simple_animal/shade/howling_ghost/proc/EtherealMove(direction)
	forceMove(get_step(src, direction))
	setDir(direction)

/mob/living/simple_animal/shade/howling_ghost/proc/roam()
	if(prob(80))
		var/direction = pick(NORTH,SOUTH,EAST,WEST,NORTHEAST,NORTHWEST,SOUTHEAST,SOUTHWEST)
		EtherealMove(direction)

/mob/living/simple_animal/shade/howling_ghost/proc/spooky_ghosty()
	if(prob(20)) //haunt
		playsound(loc, pick('sound/spookoween/ghosty_wind.ogg','sound/spookoween/ghost_whisper.ogg','sound/spookoween/chain_rattling.ogg'), 300, TRUE)
	if(prob(10)) //flickers
		var/obj/machinery/light/L = locate(/obj/machinery/light) in view(5, src)
		if(L)
			L.flicker()
	if(prob(5)) //poltergeist
		var/obj/item/I = locate(/obj/item) in view(3, src)
		if(I)
			var/direction = pick(NORTH,SOUTH,EAST,WEST,NORTHEAST,NORTHWEST,SOUTHEAST,SOUTHWEST)
			step(I,direction)
		return

/mob/living/simple_animal/shade/howling_ghost/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	. = 0

///////////////////////////
//Spookoween Insane Clown//
///////////////////////////

///Insane clown mob. Basically a clown that haunts you.
/mob/living/simple_animal/hostile/clown_insane
	name = "insane clown"
	desc = "Some clowns do not manage to be accepted, and go insane. This is one of them."
	icon = 'icons/mob/clown_mobs.dmi'
	icon_state = "scary_clown"
	icon_living = "scary_clown"
	icon_dead = "scary_clown"
	icon_gib = "scary_clown"
	speak = list("...", ". . .")
	maxHealth = 1e6
	health = 1e6
	emote_see = list("silently stares")
	unsuitable_atmos_damage = 0
	var/timer

/mob/living/simple_animal/hostile/clown_insane/Initialize()
	. = ..()
	status_flags |= GODMODE //Slightly easier to maintain.

/mob/living/simple_animal/hostile/clown_insane/Destroy()
	timer = null
	return ..()

/mob/living/simple_animal/hostile/clown_insane/ex_act()
	return

///Adds a timer to call stalk() on Aggro
/mob/living/simple_animal/hostile/clown_insane/Aggro()
	. = ..()
	timer = addtimer(CALLBACK(src, .proc/stalk), 30, TIMER_STOPPABLE|TIMER_UNIQUE)

/mob/living/simple_animal/hostile/clown_insane/LoseAggro()
	. = ..()
	if(timer)
		deltimer(timer)
		timer = null

///Plays scary noises and adds some timers.
/mob/living/simple_animal/hostile/clown_insane/proc/stalk()
	var/mob/living/M = target
	if(!istype(M))
		LoseAggro()
		return
	if(M.stat == DEAD)
		playsound(M.loc, 'sound/spookoween/insane_low_laugh.ogg', 100, TRUE)
		qdel(src)
		return
	playsound(M, pick('sound/spookoween/scary_horn.ogg','sound/spookoween/scary_horn2.ogg', 'sound/spookoween/scary_horn3.ogg'), 100, TRUE)
	timer = addtimer(CALLBACK(src, .proc/stalk), 30, TIMER_STOPPABLE|TIMER_UNIQUE)
	addtimer(CALLBACK(src, .proc/teleport_to_target), 12, TIMER_STOPPABLE|TIMER_UNIQUE)

///Does what's in the name. Teleports to target.loc. Called from a timer.
/mob/living/simple_animal/hostile/clown_insane/proc/teleport_to_target()
	if(target && isturf(target.loc)) //Hiding in lockers works to get rid of this thing.
		forceMove(target.loc)

/mob/living/simple_animal/hostile/clown_insane/MoveToTarget()
	return

/mob/living/simple_animal/hostile/clown_insane/AttackingTarget()
	return

/mob/living/simple_animal/hostile/clown_insane/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	. = 0
	if(prob(5))
		playsound(loc, 'sound/spookoween/insane_low_laugh.ogg', 300, TRUE)

/mob/living/simple_animal/hostile/clown_insane/attackby(obj/item/O, mob/user)
	if(istype(O, /obj/item/nullrod))
		if(prob(5))
			visible_message("<span class='notice'>[src] finally found the peace it deserves. <i>You hear honks echoing off into the distance.</i></span>")
			playsound(loc, 'sound/spookoween/insane_low_laugh.ogg', 300, TRUE)
			qdel(src)
		else
			visible_message("<span class='danger'>[src] seems to be resisting the effect!</span>")
		return
	return ..()

/mob/living/simple_animal/hostile/clown_insane/handle_temperature_damage()
	return

/////////////////////////
// Spooky Uplink Items //
/////////////////////////

/datum/uplink_item/dangerous/crossbow/candy
	name = "Candy Corn Crossbow"
	desc = "A standard miniature energy crossbow that uses a hard-light projector to transform bolts into candy corn. Happy Halloween!"
	category = "Holiday"
	item = /obj/item/gun/energy/kinetic_accelerator/crossbow/halloween
	surplus = 0

/datum/uplink_item/device_tools/emag/hack_o_lantern
	name = "Hack-o'-Lantern"
	desc = "An emag fitted to support the Halloween season. Candle not included."
	category = "Holiday"
	item = /obj/item/card/emag/halloween
	surplus = 0
