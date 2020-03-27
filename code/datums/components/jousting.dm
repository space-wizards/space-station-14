/datum/component/jousting
	var/current_direction = NONE
	var/max_tile_charge = 5
	var/min_tile_charge = 2				//tiles before this code gets into effect.
	var/current_tile_charge = 0
	var/movement_reset_tolerance = 2			//deciseconds
	var/unmounted_damage_boost_per_tile = 0
	var/unmounted_knockdown_chance_per_tile = 0
	var/unmounted_knockdown_time = 0
	var/mounted_damage_boost_per_tile = 2
	var/mounted_knockdown_chance_per_tile = 20
	var/mounted_knockdown_time = 20
	var/requires_mob_riding = TRUE			//whether this only works if the attacker is riding a mob, rather than anything they can buckle to.
	var/requires_mount = TRUE				//kinda defeats the point of jousting if you're not mounted but whatever.
	var/mob/current_holder
	var/current_timerid

/datum/component/jousting/Initialize()
	if(!isitem(parent))
		return COMPONENT_INCOMPATIBLE
	RegisterSignal(parent, COMSIG_ITEM_EQUIPPED, .proc/on_equip)
	RegisterSignal(parent, COMSIG_ITEM_DROPPED, .proc/on_drop)
	RegisterSignal(parent, COMSIG_ITEM_ATTACK, .proc/on_attack)

/datum/component/jousting/proc/on_equip(datum/source, mob/user, slot)
	RegisterSignal(user, COMSIG_MOVABLE_MOVED, .proc/mob_move, TRUE)
	current_holder = user

/datum/component/jousting/proc/on_drop(datum/source, mob/user)
	UnregisterSignal(user, COMSIG_MOVABLE_MOVED)
	current_holder = null
	current_direction = NONE
	current_tile_charge = 0

/datum/component/jousting/proc/on_attack(datum/source, mob/living/target, mob/user)
	if(user != current_holder)
		return
	var/current = current_tile_charge
	var/obj/item/I = parent
	var/target_buckled = target.buckled ? TRUE : FALSE			//we don't need the reference of what they're buckled to, just whether they are.
	if((requires_mount && ((requires_mob_riding && !ismob(user.buckled)) || (!user.buckled))) || !current_direction || (current_tile_charge < min_tile_charge))
		return
	var/turf/target_turf = get_step(user, current_direction)
	if(target in range(1, target_turf))
		var/knockdown_chance = (target_buckled? mounted_knockdown_chance_per_tile : unmounted_knockdown_chance_per_tile) * current
		var/knockdown_time = (target_buckled? mounted_knockdown_time : unmounted_knockdown_time)
		var/damage = (target_buckled? mounted_damage_boost_per_tile : unmounted_damage_boost_per_tile) * current
		var/sharp = I.get_sharpness()
		var/msg
		if(damage)
			msg += "[user] [sharp? "impales" : "slams into"] [target] [sharp? "on" : "with"] their [parent]"
			target.apply_damage(damage, BRUTE, user.zone_selected, 0)
		if(prob(knockdown_chance))
			msg += " and knocks [target] [target_buckled? "off of [target.buckled]" : "down"]"
			if(target_buckled)
				target.buckled.unbuckle_mob(target)
			target.Paralyze(knockdown_time)
		if(length(msg))
			user.visible_message("<span class='danger'>[msg]!</span>")

/datum/component/jousting/proc/mob_move(datum/source, newloc, dir)
	if(!current_holder || (requires_mount && ((requires_mob_riding && !ismob(current_holder.buckled)) || (!current_holder.buckled))))
		return
	if(dir != current_direction)
		current_tile_charge = 0
		current_direction = dir
	if(current_tile_charge < max_tile_charge)
		current_tile_charge++
	if(current_timerid)
		deltimer(current_timerid)
	current_timerid = addtimer(CALLBACK(src, .proc/reset_charge), movement_reset_tolerance, TIMER_STOPPABLE)

/datum/component/jousting/proc/reset_charge()
	current_tile_charge = 0
