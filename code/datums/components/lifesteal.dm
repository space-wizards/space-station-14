/datum/component/lifesteal
	var/flat_heal // heals a constant amount every time a hit occurs
	var/static/list/damage_heal_order = list(BRUTE, BURN, OXY)

/datum/component/lifesteal/Initialize(flat_heal=0)
	if(!isitem(parent) && !ishostile(parent) && !isgun(parent))
		return COMPONENT_INCOMPATIBLE

	src.flat_heal = flat_heal

/datum/component/lifesteal/RegisterWithParent()
	if(isgun(parent))
		RegisterSignal(parent, COMSIG_PROJECTILE_ON_HIT, .proc/projectile_hit)
	else if(isitem(parent))
		RegisterSignal(parent, COMSIG_ITEM_AFTERATTACK, .proc/item_afterattack)
	else if(ishostile(parent))
		RegisterSignal(parent, COMSIG_HOSTILE_ATTACKINGTARGET, .proc/hostile_attackingtarget)

/datum/component/lifesteal/UnregisterFromParent()
	UnregisterSignal(parent, list(COMSIG_ITEM_AFTERATTACK, COMSIG_HOSTILE_ATTACKINGTARGET, COMSIG_PROJECTILE_ON_HIT))

/datum/component/lifesteal/proc/item_afterattack(obj/item/source, atom/target, mob/user, proximity_flag, click_parameters)
	if(!proximity_flag)
		return
	do_lifesteal(user, target)

/datum/component/lifesteal/proc/hostile_attackingtarget(mob/living/simple_animal/hostile/attacker, atom/target)
	do_lifesteal(attacker, target)

/datum/component/lifesteal/proc/projectile_hit(atom/fired_from, atom/movable/firer, atom/target, Angle)
	do_lifesteal(firer, target)

/datum/component/lifesteal/proc/do_lifesteal(atom/heal_target, atom/damage_target)
	if(isliving(heal_target) && isliving(damage_target))
		var/mob/living/healing = heal_target
		var/mob/living/damaging = damage_target
		if(damaging.stat != DEAD)
			healing.heal_ordered_damage(flat_heal, damage_heal_order)
