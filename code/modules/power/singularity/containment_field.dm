

/obj/machinery/field/containment
	name = "containment field"
	desc = "An energy field."
	icon = 'icons/obj/singularity.dmi'
	icon_state = "Contain_F"
	density = FALSE
	move_resist = INFINITY
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF
	use_power = NO_POWER_USE
	interaction_flags_atom = NONE
	interaction_flags_machine = NONE
	CanAtmosPass = ATMOS_PASS_NO
	light_range = 4
	layer = ABOVE_OBJ_LAYER
	var/obj/machinery/field/generator/FG1 = null
	var/obj/machinery/field/generator/FG2 = null

/obj/machinery/field/containment/Initialize()
	. = ..()
	air_update_turf(TRUE)

/obj/machinery/field/containment/Destroy()
	FG1.fields -= src
	FG2.fields -= src
	CanAtmosPass = ATMOS_PASS_YES
	air_update_turf(TRUE)
	return ..()

//ATTACK HAND IGNORING PARENT RETURN VALUE
/obj/machinery/field/containment/attack_hand(mob/user)
	if(get_dist(src, user) > 1)
		return FALSE
	else
		shock(user)
		return TRUE

/obj/machinery/field/containment/attackby(obj/item/W, mob/user, params)
	shock(user)
	return TRUE

/obj/machinery/field/containment/play_attack_sound(damage_amount, damage_type = BRUTE, damage_flag = 0)
	switch(damage_type)
		if(BURN)
			playsound(loc, 'sound/effects/empulse.ogg', 75, TRUE)
		if(BRUTE)
			playsound(loc, 'sound/effects/empulse.ogg', 75, TRUE)

/obj/machinery/field/containment/blob_act(obj/structure/blob/B)
	return FALSE

/obj/machinery/field/containment/ex_act(severity, target)
	return FALSE

/obj/machinery/field/containment/attack_animal(mob/living/simple_animal/M)
	if(!FG1 || !FG2)
		qdel(src)
		return
	if(ismegafauna(M))
		M.visible_message("<span class='warning'>[M] glows fiercely as the containment field flickers out!</span>")
		FG1.calc_power(INFINITY) //rip that 'containment' field
		M.adjustHealth(-M.obj_damage)
	else
		..()

/obj/machinery/field/containment/Crossed(mob/mover)
	if(isliving(mover))
		shock(mover)

	if(ismachinery(mover) || isstructure(mover) || ismecha(mover))
		bump_field(mover)

/obj/machinery/field/containment/proc/set_master(master1,master2)
	if(!master1 || !master2)
		return FALSE
	FG1 = master1
	FG2 = master2
	return TRUE

/obj/machinery/field/containment/shock(mob/living/user)
	if(!FG1 || !FG2)
		qdel(src)
		return FALSE
	..()

/obj/machinery/field/containment/Move()
	qdel(src)
	return FALSE


// Abstract Field Class
// Used for overriding certain procs

/obj/machinery/field
	var/hasShocked = FALSE //Used to add a delay between shocks. In some cases this used to crash servers by spawning hundreds of sparks every second.

/obj/machinery/field/Bumped(atom/movable/mover)
	if(hasShocked)
		return
	if(isliving(mover))
		shock(mover)
		return
	if(ismachinery(mover) || isstructure(mover) || ismecha(mover))
		bump_field(mover)
		return


/obj/machinery/field/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	if(hasShocked || isliving(mover) || ismachinery(mover) || isstructure(mover) || ismecha(mover))
		return FALSE

/obj/machinery/field/proc/shock(mob/living/user)
	var/shock_damage = min(rand(30,40),rand(30,40))

	if(iscarbon(user))
		user.Paralyze(300)
		user.electrocute_act(shock_damage, src, 1)

	else if(issilicon(user))
		if(prob(20))
			user.Stun(40)
		user.take_overall_damage(0, shock_damage)
		user.visible_message("<span class='danger'>[user.name] was shocked by the [src.name]!</span>", \
		"<span class='userdanger'>Energy pulse detected, system damaged!</span>", \
		"<span class='hear'>You hear an electrical crack.</span>")

	user.updatehealth()
	bump_field(user)

/obj/machinery/field/proc/clear_shock()
	hasShocked = FALSE

/obj/machinery/field/proc/bump_field(atom/movable/AM as mob|obj)
	if(hasShocked)
		return FALSE
	hasShocked = TRUE
	do_sparks(5, TRUE, AM.loc)
	var/atom/target = get_edge_target_turf(AM, get_dir(src, get_step_away(AM, src)))
	AM.throw_at(target, 200, 4)
	addtimer(CALLBACK(src, .proc/clear_shock), 5)
