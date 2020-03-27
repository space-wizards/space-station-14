/datum/element/selfknockback
	element_flags = ELEMENT_BESPOKE
	id_arg_index = 2
	var/override_throw_val
	var/override_speed_val

/* Adds the ability to put recoil on various things, throwing the user back proportional to the force and speed set.
Important note: Force/Throw_Amount, is how far you want to throw a user. By default, it is calculated based on damage below.
Speed_Amount, is a measure of how long it will take a user to get thrown to the target. By default, it is calculated by
clamping the Knockback_Force value below. */
/datum/element/selfknockback/Attach(datum/target, throw_amount, speed_amount)
	. = ..()
	if(isitem(target))
		RegisterSignal(target, COMSIG_ITEM_AFTERATTACK, .proc/Item_SelfKnockback)
	else if(isprojectile(target))
		RegisterSignal(target, COMSIG_PROJECTILE_FIRE, .proc/Projectile_SelfKnockback)
	else	
		return ELEMENT_INCOMPATIBLE
	
	override_throw_val = throw_amount
	override_speed_val = speed_amount
	
/datum/element/selfknockback/Detach(datum/source, force)
	. = ..()
	UnregisterSignal(source, list(COMSIG_ITEM_AFTERATTACK, COMSIG_PROJECTILE_FIRE))

/datum/element/selfknockback/proc/Get_Knockback_Force(default_force)
	if(override_throw_val)
		return override_throw_val
	else
		return default_force

/datum/element/selfknockback/proc/Get_Knockback_Speed(default_speed)
	if(override_speed_val)
		return override_speed_val
	else
		return default_speed

/datum/element/selfknockback/proc/Item_SelfKnockback(obj/item/I, atom/attacktarget, mob/usertarget, proximity_flag)
	if(isturf(attacktarget) && !attacktarget.density)
		return
	if(proximity_flag || (get_dist(attacktarget, usertarget) <= I.reach))
		var/knockback_force = Get_Knockback_Force(CLAMP(CEILING((I.force / 10), 1), 1, 5))
		var/knockback_speed = Get_Knockback_Speed(CLAMP(knockback_force, 1, 5))

		var/target_angle = Get_Angle(attacktarget, usertarget)
		var/move_target = get_ranged_target_turf(usertarget, angle2dir(target_angle), knockback_force)
		usertarget.throw_at(move_target, knockback_force, knockback_speed)
		usertarget.visible_message("<span class='warning'>[usertarget] gets thrown back by the force of \the [I] impacting \the [attacktarget]!</span>", "<span class='warning'>The force of \the [I] impacting \the [attacktarget] sends you flying!</span>")

/datum/element/selfknockback/proc/Projectile_SelfKnockback(obj/projectile/P)
	if(!P.firer)
		return

	var/knockback_force = Get_Knockback_Force(CLAMP(CEILING((P.damage / 10), 1), 1, 5))
	var/knockback_speed = Get_Knockback_Speed(CLAMP(knockback_force, 1, 5))

	var/atom/movable/knockback_target = P.firer
	var/move_target = get_edge_target_turf(knockback_target, angle2dir(P.original_angle+180))
	knockback_target.throw_at(move_target, knockback_force, knockback_speed)
