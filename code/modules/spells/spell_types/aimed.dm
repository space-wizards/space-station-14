
/obj/effect/proc_holder/spell/aimed
	name = "aimed projectile spell"
	var/projectile_type = /obj/projectile/magic/teleport
	var/deactive_msg = "You discharge your projectile..."
	var/active_msg = "You charge your projectile!"
	var/base_icon_state = "projectile"
	var/active_icon_state = "projectile"
	var/list/projectile_var_overrides = list()
	var/projectile_amount = 1	//Projectiles per cast.
	var/current_amount = 0	//How many projectiles left.
	var/projectiles_per_fire = 1		//Projectiles per fire. Probably not a good thing to use unless you override ready_projectile().

/obj/effect/proc_holder/spell/aimed/Click()
	var/mob/living/user = usr
	if(!istype(user))
		return
	var/msg
	if(!can_cast(user))
		msg = "<span class='warning'>You can no longer cast [name]!</span>"
		remove_ranged_ability(msg)
		return
	if(active)
		msg = "<span class='notice'>[deactive_msg]</span>"
		if(charge_type == "recharge")
			var/refund_percent = current_amount/projectile_amount
			charge_counter = charge_max * refund_percent
			start_recharge()
		remove_ranged_ability(msg)
		on_deactivation(user)
	else
		msg = "<span class='notice'>[active_msg] <B>Left-click to shoot it at a target!</B></span>"
		current_amount = projectile_amount
		add_ranged_ability(user, msg, TRUE)
		on_activation(user)

/obj/effect/proc_holder/spell/aimed/proc/on_activation(mob/user)
	return

/obj/effect/proc_holder/spell/aimed/proc/on_deactivation(mob/user)
	return

/obj/effect/proc_holder/spell/aimed/update_icon()
	if(!action)
		return
	action.button_icon_state = "[base_icon_state][active]"
	action.UpdateButtonIcon()

/obj/effect/proc_holder/spell/aimed/InterceptClickOn(mob/living/caller, params, atom/target)
	if(..())
		return FALSE
	var/ran_out = (current_amount <= 0)
	if(!cast_check(!ran_out, ranged_ability_user))
		remove_ranged_ability()
		return FALSE
	var/list/targets = list(target)
	perform(targets, ran_out, user = ranged_ability_user)
	return TRUE

/obj/effect/proc_holder/spell/aimed/cast(list/targets, mob/living/user)
	var/target = targets[1]
	var/turf/T = user.loc
	var/turf/U = get_step(user, user.dir) // Get the tile infront of the move, based on their direction
	if(!isturf(U) || !isturf(T))
		return FALSE
	fire_projectile(user, target)
	user.newtonian_move(get_dir(U, T))
	if(current_amount <= 0)
		remove_ranged_ability() //Auto-disable the ability once you run out of bullets.
		charge_counter = 0
		start_recharge()
		on_deactivation(user)
	return TRUE

/obj/effect/proc_holder/spell/aimed/proc/fire_projectile(mob/living/user, atom/target)
	current_amount--
	for(var/i in 1 to projectiles_per_fire)
		var/obj/projectile/P = new projectile_type(user.loc)
		P.firer = user
		P.preparePixelProjectile(target, user)
		for(var/V in projectile_var_overrides)
			if(P.vars[V])
				P.vv_edit_var(V, projectile_var_overrides[V])
		ready_projectile(P, target, user, i)
		P.fire()
	return TRUE

/obj/effect/proc_holder/spell/aimed/proc/ready_projectile(obj/projectile/P, atom/target, mob/user, iteration)
	return

/obj/effect/proc_holder/spell/aimed/lightningbolt
	name = "Lightning Bolt"
	desc = "Fire a lightning bolt at your foes! It will jump between targets, but can't knock them down."
	school = "evocation"
	charge_max = 200
	clothes_req = TRUE
	invocation = "UN'LTD P'WAH"
	invocation_type = "shout"
	cooldown_min = 30
	base_icon_state = "lightning"
	action_icon_state = "lightning0"
	sound = 'sound/magic/lightningbolt.ogg'
	active = FALSE
	projectile_var_overrides = list("zap_range" = 15, "zap_power" = 20000, "zap_flags" = ZAP_MOB_DAMAGE | ZAP_IS_TESLA)
	active_msg = "You energize your hand with arcane lightning!"
	deactive_msg = "You let the energy flow out of your hands back into yourself..."
	projectile_type = /obj/projectile/magic/aoe/lightning

/obj/effect/proc_holder/spell/aimed/fireball
	name = "Fireball"
	desc = "This spell fires an explosive fireball at a target."
	school = "evocation"
	charge_max = 60
	clothes_req = FALSE
	invocation = "ONI SOMA"
	invocation_type = "shout"
	range = 20
	cooldown_min = 20 //10 deciseconds reduction per rank
	projectile_type = /obj/projectile/magic/aoe/fireball
	base_icon_state = "fireball"
	action_icon_state = "fireball0"
	sound = 'sound/magic/fireball.ogg'
	active_msg = "You prepare to cast your fireball spell!"
	deactive_msg = "You extinguish your fireball... for now."
	active = FALSE

/obj/effect/proc_holder/spell/aimed/fireball/fire_projectile(list/targets, mob/living/user)
	var/range = 6 + 2*spell_level
	projectile_var_overrides = list("range" = range)
	return ..()

/obj/effect/proc_holder/spell/aimed/spell_cards
	name = "Spell Cards"
	desc = "Blazing hot rapid-fire homing cards. Send your foes to the shadow realm with their mystical power!"
	school = "evocation"
	charge_max = 50
	clothes_req = FALSE
	invocation = "Sigi'lu M'Fan 'Tasia"
	invocation_type = "shout"
	range = 40
	cooldown_min = 10
	projectile_amount = 5
	projectiles_per_fire = 7
	projectile_type = /obj/projectile/spellcard
	base_icon_state = "spellcard"
	action_icon_state = "spellcard0"
	var/datum/weakref/current_target_weakref
	var/projectile_turnrate = 10
	var/projectile_pixel_homing_spread = 32
	var/projectile_initial_spread_amount = 30
	var/projectile_location_spread_amount = 12
	var/datum/component/lockon_aiming/lockon_component
	ranged_clickcd_override = TRUE

/obj/effect/proc_holder/spell/aimed/spell_cards/on_activation(mob/M)
	QDEL_NULL(lockon_component)
	lockon_component = M.AddComponent(/datum/component/lockon_aiming, 5, typecacheof(list(/mob/living)), 1, null, CALLBACK(src, .proc/on_lockon_component))

/obj/effect/proc_holder/spell/aimed/spell_cards/proc/on_lockon_component(list/locked_weakrefs)
	if(!length(locked_weakrefs))
		current_target_weakref = null
		return
	current_target_weakref = locked_weakrefs[1]
	var/atom/A = current_target_weakref.resolve()
	if(A)
		var/mob/M = lockon_component.parent
		M.face_atom(A)

/obj/effect/proc_holder/spell/aimed/spell_cards/on_deactivation(mob/M)
	QDEL_NULL(lockon_component)

/obj/effect/proc_holder/spell/aimed/spell_cards/ready_projectile(obj/projectile/P, atom/target, mob/user, iteration)
	if(current_target_weakref)
		var/atom/A = current_target_weakref.resolve()
		if(A && get_dist(A, user) < 7)
			P.homing_turn_speed = projectile_turnrate
			P.homing_inaccuracy_min = projectile_pixel_homing_spread
			P.homing_inaccuracy_max = projectile_pixel_homing_spread
			P.set_homing_target(current_target_weakref.resolve())
	var/rand_spr = rand()
	var/total_angle = projectile_initial_spread_amount * 2
	var/adjusted_angle = total_angle - ((projectile_initial_spread_amount / projectiles_per_fire) * 0.5)
	var/one_fire_angle = adjusted_angle / projectiles_per_fire
	var/current_angle = iteration * one_fire_angle * rand_spr - (projectile_initial_spread_amount / 2)
	P.pixel_x = rand(-projectile_location_spread_amount, projectile_location_spread_amount)
	P.pixel_y = rand(-projectile_location_spread_amount, projectile_location_spread_amount)
	P.preparePixelProjectile(target, user, null, current_angle)
