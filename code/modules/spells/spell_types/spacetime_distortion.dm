/obj/effect/proc_holder/spell/spacetime_dist
	name = "Spacetime Distortion"
	desc = "Entangle the strings of space-time in an area around you, randomizing the layout and making proper movement impossible. The strings vibrate..."
	charge_max = 300
	var/duration = 150
	range = 7
	var/list/effects
	var/ready = TRUE
	centcom_cancast = FALSE
	sound = 'sound/effects/magic.ogg'
	cooldown_min = 300
	level_max = 0
	action_icon_state = "spacetime"

/obj/effect/proc_holder/spell/spacetime_dist/can_cast(mob/user = usr)
	if(ready)
		return ..()
	return FALSE

/obj/effect/proc_holder/spell/spacetime_dist/choose_targets(mob/user = usr)
	var/list/turfs = spiral_range_turfs(range, user)
	if(!turfs.len)
		revert_cast()
		return

	ready = FALSE
	var/list/turf_steps = list()
	var/length = round(turfs.len * 0.5)
	for(var/i in 1 to length)
		turf_steps[pick_n_take(turfs)] = pick_n_take(turfs)
	if(turfs.len > 0)
		var/turf/loner = pick(turfs)
		var/area/A = get_area(user)
		turf_steps[loner] = get_turf(pick(A.contents))

	perform(turf_steps,user=user)

/obj/effect/proc_holder/spell/spacetime_dist/after_cast(list/targets)
	addtimer(CALLBACK(src, .proc/clean_turfs), duration)

/obj/effect/proc_holder/spell/spacetime_dist/cast(list/targets, mob/user = usr)
	effects = list()
	for(var/V in targets)
		var/turf/T0 = V
		var/turf/T1 = targets[V]
		var/obj/effect/cross_action/spacetime_dist/STD0 = new /obj/effect/cross_action/spacetime_dist(T0)
		var/obj/effect/cross_action/spacetime_dist/STD1 = new /obj/effect/cross_action/spacetime_dist(T1)
		STD0.linked_dist = STD1
		STD0.add_overlay(T1.photograph())
		STD1.linked_dist = STD0
		STD1.add_overlay(T0.photograph())
		STD1.set_light(4, 30, "#c9fff5")
		effects += STD0
		effects += STD1

/obj/effect/proc_holder/spell/spacetime_dist/proc/clean_turfs()
	for(var/effect in effects)
		qdel(effect)
	effects.Cut()
	effects = null
	ready = TRUE

/obj/effect/cross_action
	name = "cross me"
	desc = "for crossing"
	anchored = TRUE

/obj/effect/cross_action/spacetime_dist
	name = "spacetime distortion"
	desc = "A distortion in spacetime. You can hear faint music..."
	icon_state = ""
	var/obj/effect/cross_action/spacetime_dist/linked_dist
	var/busy = FALSE
	var/sound
	var/walks_left = 50 //prevents the game from hanging in extreme cases (such as minigun fire)

/obj/effect/cross_action/singularity_act()
	return

/obj/effect/cross_action/singularity_pull()
	return

/obj/effect/cross_action/spacetime_dist/Initialize(mapload)
	. = ..()
	setDir(pick(GLOB.cardinals))

/obj/effect/cross_action/spacetime_dist/proc/walk_link(atom/movable/AM)
	if(ismob(AM))
		var/mob/M = AM
		if(M.anti_magic_check(chargecost = 0))
			return
	if(linked_dist && walks_left > 0)
		flick("purplesparkles", src)
		linked_dist.get_walker(AM)
		walks_left--

/obj/effect/cross_action/spacetime_dist/proc/get_walker(atom/movable/AM)
	busy = TRUE
	flick("purplesparkles", src)
	AM.forceMove(get_turf(src))
	playsound(get_turf(src),sound,70,FALSE)
	busy = FALSE

/obj/effect/cross_action/spacetime_dist/Crossed(atom/movable/AM)
	if(!busy)
		walk_link(AM)

/obj/effect/cross_action/spacetime_dist/attackby(obj/item/W, mob/user, params)
	if(user.temporarilyRemoveItemFromInventory(W))
		walk_link(W)
	else
		walk_link(user)

//ATTACK HAND IGNORING PARENT RETURN VALUE
/obj/effect/cross_action/spacetime_dist/attack_hand(mob/user)
	walk_link(user)

/obj/effect/cross_action/spacetime_dist/attack_paw(mob/user)
	walk_link(user)

/obj/effect/cross_action/spacetime_dist/Destroy()
	busy = TRUE
	linked_dist = null
	return ..()
