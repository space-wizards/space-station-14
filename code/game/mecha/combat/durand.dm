/obj/mecha/combat/durand
	desc = "An aging combat exosuit utilized by the Nanotrasen corporation. Originally developed to combat hostile alien lifeforms."
	name = "\improper Durand"
	icon_state = "durand"
	step_in = 4
	dir_in = 1 //Facing North.
	max_integrity = 400
	deflect_chance = 20
	armor = list("melee" = 40, "bullet" = 35, "laser" = 15, "energy" = 10, "bomb" = 20, "bio" = 0, "rad" = 50, "fire" = 100, "acid" = 100)
	max_temperature = 30000
	infra_luminosity = 8
	force = 40
	wreckage = /obj/structure/mecha_wreckage/durand
	var/obj/durand_shield/shield

/obj/mecha/combat/durand/Initialize()
	shield = new/obj/durand_shield
	shield.chassis = src
	shield.layer = layer
	RegisterSignal(src, COMSIG_MECHA_ACTION_ACTIVATE, .proc/relay)
	RegisterSignal(src, COMSIG_PROJECTILE_PREHIT, .proc/prehit)
	. = ..()

/obj/mecha/combat/durand/Destroy()
	if(shield)
		qdel(shield)
	. = ..()

/obj/mecha/combat/durand/GrantActions(mob/living/user, human_occupant = 0)
	..()
	defense_action.Grant(user, src)

/obj/mecha/combat/durand/RemoveActions(mob/living/user, human_occupant = 0)
	..()
	defense_action.Remove(user)

/obj/mecha/combat/durand/process()
	. = ..()
	if(defense_mode && !use_power(100))
		defense_action.Activate(forced_state = TRUE)

/obj/mecha/combat/durand/domove(direction)
	. = ..()
	if(shield)
		shield.forceMove(loc)
		shield.dir = dir

/obj/mecha/combat/durand/forceMove(var/turf/T)
	. = ..()
	shield.forceMove(T)

/obj/mecha/combat/durand/go_out(forced, atom/newloc = loc)
	if(defense_mode)
		defense_action.Activate(forced_state = TRUE)
	. = ..()

///Relays the signal from the action button to the shield, and creates a new shield if the old one is MIA.
/obj/mecha/combat/durand/proc/relay(datum/source, list/signal_args)
	if(!shield) //if the shield somehow got deleted
		shield = new/obj/durand_shield
		shield.chassis = src
		shield.layer = layer
		shield.forceMove(loc)
	shield.dir = dir
	SEND_SIGNAL(shield, COMSIG_MECHA_ACTION_ACTIVATE, source, signal_args)

//Redirects projectiles to the shield if defense_check decides they should be blocked and returns true.
/obj/mecha/combat/durand/proc/prehit(obj/projectile/source, list/signal_args)
	if(defense_check(source.loc) && shield)
		signal_args[2] = shield


/**Checks if defense mode is enabled, and if the attacker is standing in an area covered by the shield.
Expects a turf. Returns true if the attack should be blocked, false if not.*/
/obj/mecha/combat/durand/proc/defense_check(var/turf/aloc)
	if (!defense_mode || !shield || shield.switching)
		return FALSE
	. = FALSE
	switch(dir)
		if (1)
			if(abs(x - aloc.x) <= (y - aloc.y) * -2)
				. = TRUE
		if (2)
			if(abs(x - aloc.x) <= (y - aloc.y) * 2)
				. = TRUE
		if (4)
			if(abs(y - aloc.y) <= (x - aloc.x) * -2)
				. = TRUE
		if (8)
			if(abs(y - aloc.y) <= (x - aloc.x) * 2)
				. = TRUE
	return

obj/mecha/combat/durand/attack_generic(mob/user, damage_amount = 0, damage_type = BRUTE, damage_flag = 0, sound_effect = 1, armor_penetration = 0)
	if(defense_check(user.loc))
		log_message("Attack absorbed by defense field. Attacker - [user].", LOG_MECHA, color="orange")
		shield.attack_generic(user, damage_amount, damage_type, damage_flag, sound_effect, armor_penetration)
	else
		. = ..()

/obj/mecha/combat/durand/blob_act(obj/structure/blob/B)
	if(defense_check(B.loc))
		log_message("Attack by blob. Attacker - [B].", LOG_MECHA, color="red")
		log_message("Attack absorbed by defense field.", LOG_MECHA, color="orange")
		shield.blob_act(B)
	else
		. = ..()

/obj/mecha/combat/durand/attackby(obj/item/W as obj, mob/user as mob, params)
	if(defense_check(user.loc))
		log_message("Attack absorbed by defense field. Attacker - [user], with [W]", LOG_MECHA, color="orange")
		shield.attackby(W, user, params)
	else
		. = ..()

/obj/mecha/combat/durand/hitby(atom/movable/AM, skipcatch, hitpush, blocked, datum/thrownthing/throwingdatum)
	if(defense_check(AM.loc))
		log_message("Impact with [AM] absorbed by defense field.", LOG_MECHA, color="orange")
		shield.hitby(AM, skipcatch, hitpush, blocked, throwingdatum)
	else
		. = ..()

////////////////////////////
///// Shield processing ////
////////////////////////////

/**An object to take the hit for us when using the Durand's defense mode.
It is spawned in during the durand's initilization, and always stays on the same tile.
Normally invisible, until defense mode is actvated. When the durand detects an attack that should be blocked, the
attack is passed to the shield. The shield takes the damage, uses it to calculate charge cost, and then sets its
own integrity back to max. Shield is automatically dropped if we run out of power or the user gets out.*/

/obj/durand_shield //projectiles get passed to this when defense mode is enabled
	name = "defense grid"
	icon = 'icons/mecha/durand_shield.dmi'
	icon_state = "shield_null"
	invisibility = INVISIBILITY_MAXIMUM //no showing on right-click
	pixel_y = 4
	max_integrity = 10000
	obj_integrity = 10000
	anchored = TRUE
	var/obj/mecha/combat/durand/chassis ///Our link back to the durand
	var/switching = FALSE ///To keep track of things during the animation

/obj/durand_shield/Initialize()
	. = ..()
	RegisterSignal(src, COMSIG_MECHA_ACTION_ACTIVATE, .proc/activate)

/obj/durand_shield/Destroy()
	if(chassis)
		chassis.shield = null
	. = ..()

/**Handles activating and deactivating the shield. This proc is called by a signal sent from the mech's action button
and relayed by the mech itself. The "forced" variabe, signal_args[1], will skip the to-pilot text and is meant for when
the shield is disabled by means other than the action button (like running out of power)*/

/obj/durand_shield/proc/activate(datum/source, datum/action/innate/mecha/mech_defense_mode/button, list/signal_args)
	if(!chassis || !chassis.occupant)
		return
	if(switching && !signal_args[1])
		return
	if(!chassis.defense_mode && (!chassis.cell || chassis.cell.charge < 100)) //If it's off, and we have less than 100 units of power
		chassis.occupant_message("<span class='warn'>Insufficient power; cannot activate defense mode.</span>")
		return
	switching = TRUE
	chassis.defense_mode = !chassis.defense_mode
	chassis.defense_action.button_icon_state = "mech_defense_mode_[chassis.defense_mode ? "on" : "off"]" //This is backwards because we haven't changed the var yet
	if(!signal_args[1])
		chassis.occupant_message("<span class='notice'>Defense mode [chassis.defense_mode?"enabled":"disabled"].</span>")
		chassis.log_message("User has toggled defense mode -- now [chassis.defense_mode?"enabled":"disabled"].", LOG_MECHA)
	else
		chassis.log_message("defense mode state changed -- now [chassis.defense_mode?"enabled":"disabled"].", LOG_MECHA)
	chassis.defense_action.UpdateButtonIcon()

	if(chassis.defense_mode)
		invisibility = 0
		flick("shield_raise", src)
		playsound(src, 'sound/mecha/mech_shield_raise.ogg', 50, FALSE)
		set_light(l_range = MINIMUM_USEFUL_LIGHT_RANGE	, l_power = 5, l_color = "#00FFFF")
		sleep(3)
		icon_state = "shield"
	else
		flick("shield_drop", src)
		playsound(src, 'sound/mecha/mech_shield_drop.ogg', 50, FALSE)
		sleep(5)
		set_light(0)
		icon_state = "shield_null"
		invisibility = INVISIBILITY_MAXIMUM //no showing on right-click
	switching = FALSE

/obj/durand_shield/take_damage()
	if(!chassis)
		qdel(src)
		return
	if(!chassis.defense_mode) //if defense mode is disabled, we're taking damage that we shouldn't be taking
		return
	. = ..()
	flick("shield_impact", src)
	if(!chassis.use_power((max_integrity - obj_integrity) * 100))
		chassis.cell?.charge = 0
		chassis.defense_action.Activate(forced_state = TRUE)
	obj_integrity = 10000

/obj/durand_shield/play_attack_sound()
	playsound(src, 'sound/mecha/mech_shield_deflect.ogg', 100, TRUE)

/obj/durand_shield/bullet_act()
	play_attack_sound()
	. = ..()
