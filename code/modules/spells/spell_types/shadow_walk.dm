/obj/effect/proc_holder/spell/targeted/shadowwalk
	name = "Shadow Walk"
	desc = "Grants unlimited movement in darkness."
	charge_max = 0
	clothes_req = FALSE
	antimagic_allowed = TRUE
	phase_allowed = TRUE
	selection_type = "range"
	range = -1
	include_user = TRUE
	cooldown_min = 0
	overlay = null
	action_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	action_icon_state = "ninja_cloak"
	action_background_icon_state = "bg_alien"

/obj/effect/proc_holder/spell/targeted/shadowwalk/cast(list/targets,mob/living/user = usr)
	var/L = user.loc
	if(istype(user.loc, /obj/effect/dummy/phased_mob/shadow))
		var/obj/effect/dummy/phased_mob/shadow/S = L
		S.end_jaunt(FALSE)
		return
	else
		var/turf/T = get_turf(user)
		var/light_amount = T.get_lumcount()
		if(light_amount < SHADOW_SPECIES_LIGHT_THRESHOLD)
			playsound(get_turf(user), 'sound/magic/ethereal_enter.ogg', 50, TRUE, -1)
			visible_message("<span class='boldwarning'>[user] melts into the shadows!</span>")
			user.SetAllImmobility(0)
			user.setStaminaLoss(0, 0)
			var/obj/effect/dummy/phased_mob/shadow/S2 = new(get_turf(user.loc))
			user.forceMove(S2)
			S2.jaunter = user
		else
			to_chat(user, "<span class='warning'>It isn't dark enough here!</span>")

/obj/effect/dummy/phased_mob/shadow
	name = "darkness"
	icon = 'icons/effects/effects.dmi'
	icon_state = "nothing"
	var/canmove = TRUE
	var/mob/living/jaunter
	density = FALSE
	anchored = TRUE
	invisibility = 60
	resistance_flags = LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF

/obj/effect/dummy/phased_mob/shadow/relaymove(mob/user, direction)
	var/turf/newLoc = get_step(src,direction)
	if(isspaceturf(newLoc))
		to_chat(user, "<span class='warning'>It really would not be wise to go into space.</span>")
		return
	forceMove(newLoc)
	check_light_level()

/obj/effect/dummy/phased_mob/shadow/proc/check_light_level()
	var/turf/T = get_turf(src)
	var/light_amount = T.get_lumcount()
	if(light_amount > 0.2) // jaunt ends
		end_jaunt(TRUE)
	else if (light_amount < 0.2 && (!QDELETED(jaunter))) //heal in the dark
		jaunter.heal_overall_damage(1,1, 0, BODYPART_ORGANIC)

/obj/effect/dummy/phased_mob/shadow/proc/end_jaunt(forced = FALSE)
	if(jaunter)
		if(forced)
			visible_message("<span class='boldwarning'>[jaunter] is revealed by the light!</span>")
		else
			visible_message("<span class='boldwarning'>[jaunter] emerges from the darkness!</span>")
		jaunter.forceMove(get_turf(src))
		playsound(get_turf(jaunter), 'sound/magic/ethereal_exit.ogg', 50, TRUE, -1)
		jaunter = null
	qdel(src)

/obj/effect/dummy/phased_mob/shadow/Initialize(mapload)
	. = ..()
	START_PROCESSING(SSobj, src)

/obj/effect/dummy/phased_mob/shadow/Destroy()
	STOP_PROCESSING(SSobj, src)
	. = ..()

/obj/effect/dummy/phased_mob/shadow/process()
	if(!jaunter)
		qdel(src)
	if(jaunter.loc != src)
		qdel(src)
	check_light_level()

/obj/effect/dummy/phased_mob/shadow/ex_act()
	return

/obj/effect/dummy/phased_mob/shadow/bullet_act()
	return BULLET_ACT_FORCE_PIERCE

/obj/effect/dummy/phased_mob/shadow/singularity_act()
	return

