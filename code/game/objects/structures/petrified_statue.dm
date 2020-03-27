/obj/structure/statue/petrified
	name = "statue"
	desc = "An incredibly lifelike marble carving."
	icon_state = "human_male"
	density = TRUE
	anchored = TRUE
	max_integrity = 200
	var/timer = 240 //eventually the person will be freed
	var/mob/living/petrified_mob

/obj/structure/statue/petrified/New(loc, mob/living/L, statue_timer)
	if(statue_timer)
		timer = statue_timer
	if(L)
		petrified_mob = L
		if(L.buckled)
			L.buckled.unbuckle_mob(L,force=1)
		L.visible_message("<span class='warning'>[L]'s skin rapidly turns to marble!</span>", "<span class='userdanger'>Your body freezes up! Can't... move... can't... think...</span>")
		L.forceMove(src)
		ADD_TRAIT(L, TRAIT_MUTE, STATUE_MUTE)
		L.faction += "mimic" //Stops mimics from instaqdeling people in statues
		L.status_flags |= GODMODE
		obj_integrity = L.health + 100 //stoning damaged mobs will result in easier to shatter statues
		max_integrity = obj_integrity
		START_PROCESSING(SSobj, src)
	..()

/obj/structure/statue/petrified/process()
	if(!petrified_mob)
		STOP_PROCESSING(SSobj, src)
	timer--
	petrified_mob.Stun(40) //So they can't do anything while petrified
	if(timer <= 0)
		STOP_PROCESSING(SSobj, src)
		qdel(src)

/obj/structure/statue/petrified/contents_explosion(severity, target)
	return

/obj/structure/statue/petrified/handle_atom_del(atom/A)
	if(A == petrified_mob)
		petrified_mob = null

/obj/structure/statue/petrified/Destroy()

	if(istype(src.loc, /mob/living/simple_animal/hostile/statue))
		var/mob/living/simple_animal/hostile/statue/S = src.loc
		forceMove(S.loc)
		if(S.mind)
			if(petrified_mob)
				S.mind.transfer_to(petrified_mob)
				petrified_mob.Paralyze(100)
				to_chat(petrified_mob, "<span class='notice'>You slowly come back to your senses. You are in control of yourself again!</span>")
		qdel(S)

	for(var/obj/O in src)
		O.forceMove(loc)

	if(petrified_mob)
		petrified_mob.status_flags &= ~GODMODE
		petrified_mob.forceMove(loc)
		REMOVE_TRAIT(petrified_mob, TRAIT_MUTE, STATUE_MUTE)
		petrified_mob.take_overall_damage((petrified_mob.health - obj_integrity + 100)) //any new damage the statue incurred is transfered to the mob
		petrified_mob.faction -= "mimic"
		petrified_mob = null
	return ..()

/obj/structure/statue/petrified/deconstruct(disassembled = TRUE)
	if(!disassembled)
		if(petrified_mob)
			petrified_mob.dust()
	visible_message("<span class='danger'>[src] shatters!.</span>")
	qdel(src)


/mob/proc/petrify(statue_timer)

/mob/living/carbon/human/petrify(statue_timer)
	if(!isturf(loc))
		return 0
	var/obj/structure/statue/petrified/S = new(loc, src, statue_timer)
	S.name = "statue of [name]"
	bleedsuppress = 1
	S.copy_overlays(src)
	var/newcolor = list(rgb(77,77,77), rgb(150,150,150), rgb(28,28,28), rgb(0,0,0))
	S.add_atom_colour(newcolor, FIXED_COLOUR_PRIORITY)
	return 1

/mob/living/carbon/monkey/petrify(statue_timer)
	if(!isturf(loc))
		return 0
	var/obj/structure/statue/petrified/S = new(loc, src, statue_timer)
	S.name = "statue of a monkey"
	S.icon_state = "monkey"
	return 1

/mob/living/simple_animal/pet/dog/corgi/petrify(statue_timer)
	if(!isturf(loc))
		return 0
	var/obj/structure/statue/petrified/S = new (loc, src, statue_timer)
	S.name = "statue of a corgi"
	S.icon_state = "corgi"
	S.desc = "If it takes forever, I will wait for you..."
	return 1
