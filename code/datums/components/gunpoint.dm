#define GUNPOINT_SHOOTER_STRAY_RANGE 2
#define GUNPOINT_DELAY_STAGE_2 25
#define GUNPOINT_DELAY_STAGE_3 75 // cumulative with past stages, so 100 deciseconds
#define GUNPOINT_MULT_STAGE_1 1
#define GUNPOINT_MULT_STAGE_2 2
#define GUNPOINT_MULT_STAGE_3 2.5


/datum/component/gunpoint
	dupe_mode = COMPONENT_DUPE_UNIQUE

	var/mob/living/target
	var/obj/item/gun/weapon

	var/stage = 1
	var/damage_mult = GUNPOINT_MULT_STAGE_1

	var/point_of_no_return = FALSE

// *extremely bad russian accent* no!
/datum/component/gunpoint/Initialize(mob/living/targ, obj/item/gun/wep)
	if(!isliving(parent))
		return COMPONENT_INCOMPATIBLE

	var/mob/living/shooter = parent
	target = targ
	weapon = wep
	RegisterSignal(targ, list(COMSIG_MOB_ATTACK_HAND, COMSIG_MOB_ITEM_ATTACK, COMSIG_MOVABLE_MOVED, COMSIG_MOB_FIRED_GUN), .proc/trigger_reaction)

	RegisterSignal(weapon, list(COMSIG_ITEM_DROPPED, COMSIG_ITEM_EQUIPPED), .proc/cancel)

	shooter.visible_message("<span class='danger'>[shooter] aims [weapon] point blank at [target]!</span>", \
		"<span class='danger'>You aim [weapon] point blank at [target]!</span>", target)
	to_chat(target, "<span class='userdanger'>[shooter] aims [weapon] point blank at you!</span>")

	shooter.apply_status_effect(STATUS_EFFECT_HOLDUP)
	target.apply_status_effect(STATUS_EFFECT_HELDUP)

	if(target.job == "Captain" && target.stat == CONSCIOUS && is_nuclear_operative(shooter))
		if(istype(weapon, /obj/item/gun/ballistic/rocketlauncher) && weapon.chambered)
			shooter.client.give_award(/datum/award/achievement/misc/rocket_holdup, shooter)

	target.do_alert_animation(target)
	target.playsound_local(target.loc, 'sound/machines/chime.ogg', 50, TRUE)

	addtimer(CALLBACK(src, .proc/update_stage, 2), GUNPOINT_DELAY_STAGE_2)

/datum/component/gunpoint/Destroy(force, silent)
	var/mob/living/shooter = parent
	shooter.remove_status_effect(STATUS_EFFECT_HOLDUP)
	target.remove_status_effect(STATUS_EFFECT_HELDUP)
	return ..()

/datum/component/gunpoint/RegisterWithParent()
	RegisterSignal(parent, COMSIG_MOVABLE_MOVED, .proc/check_deescalate)
	RegisterSignal(parent, COMSIG_MOB_APPLY_DAMGE, .proc/flinch)
	RegisterSignal(parent, COMSIG_MOB_ATTACK_HAND, .proc/check_shove)
	RegisterSignal(parent, list(COMSIG_LIVING_START_PULL, COMSIG_MOVABLE_BUMP), .proc/check_bump)

/datum/component/gunpoint/UnregisterFromParent()
	UnregisterSignal(parent, COMSIG_MOVABLE_MOVED)
	UnregisterSignal(parent, COMSIG_MOB_APPLY_DAMGE)
	UnregisterSignal(parent, COMSIG_MOB_ATTACK_HAND)
	UnregisterSignal(parent, list(COMSIG_LIVING_START_PULL, COMSIG_MOVABLE_BUMP))

/datum/component/gunpoint/proc/check_bump(atom/B, atom/A)
	var/mob/living/T = A
	if(T && T == target)
		var/mob/living/shooter = parent
		shooter.visible_message("<span class='danger'>[shooter] bumps into [target] and fumbles [shooter.p_their()] aim!</span>", \
			"<span class='danger'>You bump into [target] and fumble your aim!</span>", target)
		to_chat(target, "<span class='userdanger'>[shooter] bumps into you and fumbles [shooter.p_their()] aim!</span>")
		qdel(src)

/datum/component/gunpoint/proc/check_shove(mob/living/carbon/shooter, mob/shooter_again, mob/living/T)
	if(T == target && (shooter.a_intent == INTENT_DISARM || shooter.a_intent == INTENT_GRAB))
		shooter.visible_message("<span class='danger'>[shooter] bumps into [target] and fumbles [shooter.p_their()] aim!</span>", \
			"<span class='danger'>You bump into [target] and fumble your aim!</span>", target)
		to_chat(target, "<span class='userdanger'>[shooter] bumps into you and fumbles [shooter.p_their()] aim!</span>")
		qdel(src)

// if you're gonna try to break away from a holdup, better to do it right away
/datum/component/gunpoint/proc/update_stage(new_stage)
	stage = new_stage
	if(stage == 2)
		to_chat(parent, "<span class='danger'>You steady [weapon] on [target].</span>")
		to_chat(target, "<span class='userdanger'>[parent] has steadied [weapon] on you!</span>")
		damage_mult = GUNPOINT_MULT_STAGE_2
		addtimer(CALLBACK(src, .proc/update_stage, 3), GUNPOINT_DELAY_STAGE_3)
	else if(stage == 3)
		to_chat(parent, "<span class='danger'>You have fully steadied [weapon] on [target].</span>")
		to_chat(target, "<span class='userdanger'>[parent] has fully steadied [weapon] on you!</span>")
		damage_mult = GUNPOINT_MULT_STAGE_3

/datum/component/gunpoint/proc/check_deescalate()
	if(!can_see(parent, target, GUNPOINT_SHOOTER_STRAY_RANGE - 1))
		cancel()

/datum/component/gunpoint/proc/trigger_reaction()
	if(point_of_no_return)
		return
	point_of_no_return = TRUE

	var/mob/living/shooter = parent

	if(!weapon.can_shoot() || !weapon.can_trigger_gun(shooter) || (weapon.weapon_weight == WEAPON_HEAVY && shooter.get_inactive_held_item()))
		shooter.visible_message("<span class='danger'>[shooter] fumbles [weapon]!</span>", \
			"<span class='danger'>You fumble [weapon] and fail to fire at [target]!</span>", target)
		to_chat(target, "<span class='userdanger'>[shooter] fumbles [weapon] and fails to fire at you!</span>")
		qdel(src)
		return

	if(weapon.chambered && weapon.chambered.BB)
		weapon.chambered.BB.damage *= damage_mult

	if(weapon.check_botched(shooter))
		return

	weapon.process_fire(target, shooter)
	qdel(src)

/datum/component/gunpoint/proc/cancel()
	var/mob/living/shooter = parent
	shooter.visible_message("<span class='danger'>[shooter] breaks [shooter.p_their()] aim on [target]!</span>", \
		"<span class='danger'>You are no longer aiming [weapon] at [target].</span>", target)
	to_chat(target, "<span class='userdanger'>[shooter] breaks [shooter.p_their()] aim on you!</span>")
	qdel(src)

/datum/component/gunpoint/proc/flinch(attacker, damage, damagetype, def_zone)
	var/mob/living/shooter = parent

	var/flinch_chance = 50
	var/gun_hand = LEFT_HANDS

	if(shooter.held_items[RIGHT_HANDS] == weapon)
		gun_hand = RIGHT_HANDS

	if((def_zone == BODY_ZONE_L_ARM && gun_hand == LEFT_HANDS) || (def_zone == BODY_ZONE_R_ARM && gun_hand == RIGHT_HANDS))
		flinch_chance = 80

	if(prob(flinch_chance))
		shooter.visible_message("<span class='danger'>[shooter] flinches!</span>", \
			"<span class='danger'>You flinch!</span>")
		trigger_reaction()

#undef GUNPOINT_SHOOTER_STRAY_RANGE
#undef GUNPOINT_DELAY_STAGE_2
#undef GUNPOINT_DELAY_STAGE_3
#undef GUNPOINT_MULT_STAGE_1
#undef GUNPOINT_MULT_STAGE_2
#undef GUNPOINT_MULT_STAGE_3
