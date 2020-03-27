/obj/projectile/hallucination
	name = "bullet"
	icon = null
	icon_state = null
	hitsound = ""
	suppressed = TRUE
	ricochets_max = 0
	ricochet_chance = 0
	damage = 0
	nodamage = TRUE
	projectile_type = /obj/projectile/hallucination
	log_override = TRUE
	var/hal_icon_state
	var/image/fake_icon
	var/mob/living/carbon/hal_target
	var/hal_fire_sound
	var/hal_hitsound
	var/hal_hitsound_wall
	var/hal_impact_effect
	var/hal_impact_effect_wall
	var/hit_duration
	var/hit_duration_wall

/obj/projectile/hallucination/fire()
	..()
	fake_icon = image('icons/obj/projectiles.dmi', src, hal_icon_state, ABOVE_MOB_LAYER)
	if(hal_target.client)
		hal_target.client.images += fake_icon

/obj/projectile/hallucination/Destroy()
	if(hal_target.client)
		hal_target.client.images -= fake_icon
	QDEL_NULL(fake_icon)
	return ..()

/obj/projectile/hallucination/Bump(atom/A)
	if(!ismob(A))
		if(hal_hitsound_wall)
			hal_target.playsound_local(loc, hal_hitsound_wall, 40, 1)
		if(hal_impact_effect_wall)
			spawn_hit(A, TRUE)
	else if(A == hal_target)
		if(hal_hitsound)
			hal_target.playsound_local(A, hal_hitsound, 100, 1)
		target_on_hit(A)
	qdel(src)
	return TRUE

/obj/projectile/hallucination/proc/target_on_hit(mob/M)
	if(M == hal_target)
		to_chat(hal_target, "<span class='userdanger'>[M] is hit by \a [src] in the chest!</span>")
		hal_apply_effect()
	else if(M in view(hal_target))
		to_chat(hal_target, "<span class='danger'>[M] is hit by \a [src] in the chest!!</span>")
	if(damage_type == BRUTE)
		var/splatter_dir = dir
		if(starting)
			splatter_dir = get_dir(starting, get_turf(M))
		spawn_blood(M, splatter_dir)
	else if(hal_impact_effect)
		spawn_hit(M, FALSE)

/obj/projectile/hallucination/proc/spawn_blood(mob/M, set_dir)
	set waitfor = 0
	if(!hal_target.client)
		return

	var/splatter_icon_state
	if(set_dir in GLOB.diagonals)
		splatter_icon_state = "splatter[pick(1, 2, 6)]"
	else
		splatter_icon_state = "splatter[pick(3, 4, 5)]"

	var/image/blood = image('icons/effects/blood.dmi', M, splatter_icon_state, ABOVE_MOB_LAYER)
	var/target_pixel_x = 0
	var/target_pixel_y = 0
	switch(set_dir)
		if(NORTH)
			target_pixel_y = 16
		if(SOUTH)
			target_pixel_y = -16
			layer = ABOVE_MOB_LAYER
		if(EAST)
			target_pixel_x = 16
		if(WEST)
			target_pixel_x = -16
		if(NORTHEAST)
			target_pixel_x = 16
			target_pixel_y = 16
		if(NORTHWEST)
			target_pixel_x = -16
			target_pixel_y = 16
		if(SOUTHEAST)
			target_pixel_x = 16
			target_pixel_y = -16
			layer = ABOVE_MOB_LAYER
		if(SOUTHWEST)
			target_pixel_x = -16
			target_pixel_y = -16
			layer = ABOVE_MOB_LAYER
	hal_target.client.images += blood
	animate(blood, pixel_x = target_pixel_x, pixel_y = target_pixel_y, alpha = 0, time = 5)
	addtimer(CALLBACK(src, .proc/cleanup_blood), 5)

/obj/projectile/hallucination/proc/cleanup_blood(image/blood)
	hal_target.client.images -= blood
	qdel(blood)

/obj/projectile/hallucination/proc/spawn_hit(atom/A, is_wall)
	set waitfor = 0
	if(!hal_target.client)
		return

	var/image/hit_effect = image('icons/effects/blood.dmi', A, is_wall ? hal_impact_effect_wall : hal_impact_effect, ABOVE_MOB_LAYER)
	hit_effect.pixel_x = A.pixel_x + rand(-4,4)
	hit_effect.pixel_y = A.pixel_y + rand(-4,4)
	hal_target.client.images += hit_effect
	sleep(is_wall ? hit_duration_wall : hit_duration)
	hal_target.client.images -= hit_effect
	qdel(hit_effect)


/obj/projectile/hallucination/proc/hal_apply_effect()
	return

/obj/projectile/hallucination/bullet
	name = "bullet"
	hal_icon_state = "bullet"
	hal_fire_sound = "gunshot"
	hal_hitsound = 'sound/weapons/pierce.ogg'
	hal_hitsound_wall = "ricochet"
	hal_impact_effect = "impact_bullet"
	hal_impact_effect_wall = "impact_bullet"
	hit_duration = 5
	hit_duration_wall = 5

/obj/projectile/hallucination/bullet/hal_apply_effect()
	hal_target.adjustStaminaLoss(60)

/obj/projectile/hallucination/laser
	name = "laser"
	damage_type = BURN
	hal_icon_state = "laser"
	hal_fire_sound = 'sound/weapons/laser.ogg'
	hal_hitsound = 'sound/weapons/sear.ogg'
	hal_hitsound_wall = 'sound/weapons/effects/searwall.ogg'
	hal_impact_effect = "impact_laser"
	hal_impact_effect_wall = "impact_laser_wall"
	hit_duration = 4
	hit_duration_wall = 10
	pass_flags = PASSTABLE | PASSGLASS | PASSGRILLE

/obj/projectile/hallucination/laser/hal_apply_effect()
	hal_target.adjustStaminaLoss(20)
	hal_target.blur_eyes(2)

/obj/projectile/hallucination/taser
	name = "electrode"
	damage_type = BURN
	hal_icon_state = "spark"
	color = "#FFFF00"
	hal_fire_sound = 'sound/weapons/taser.ogg'
	hal_hitsound = 'sound/weapons/taserhit.ogg'
	hal_hitsound_wall = null
	hal_impact_effect = null
	hal_impact_effect_wall = null

/obj/projectile/hallucination/taser/hal_apply_effect()
	hal_target.Paralyze(100)
	hal_target.stuttering += 20
	if(hal_target.dna && hal_target.dna.check_mutation(HULK))
		hal_target.say(pick(";RAAAAAAAARGH!", ";HNNNNNNNNNGGGGGGH!", ";GWAAAAAAAARRRHHH!", "NNNNNNNNGGGGGGGGHH!", ";AAAAAAARRRGH!" ), forced = "hulk")
	else if((hal_target.status_flags & CANKNOCKDOWN) && !HAS_TRAIT(hal_target, TRAIT_STUNIMMUNE))
		addtimer(CALLBACK(hal_target, /mob/living/carbon.proc/do_jitter_animation, 20), 5)

/obj/projectile/hallucination/disabler
	name = "disabler beam"
	damage_type = STAMINA
	hal_icon_state = "omnilaser"
	hal_fire_sound = 'sound/weapons/taser2.ogg'
	hal_hitsound = 'sound/weapons/tap.ogg'
	hal_hitsound_wall = 'sound/weapons/effects/searwall.ogg'
	hal_impact_effect = "impact_laser_blue"
	hal_impact_effect_wall = null
	hit_duration = 4
	pass_flags = PASSTABLE | PASSGLASS | PASSGRILLE

/obj/projectile/hallucination/disabler/hal_apply_effect()
	hal_target.adjustStaminaLoss(25)

/obj/projectile/hallucination/ebow
	name = "bolt"
	damage_type = TOX
	hal_icon_state = "cbbolt"
	hal_fire_sound = 'sound/weapons/genhit.ogg'
	hal_hitsound = null
	hal_hitsound_wall = null
	hal_impact_effect = null
	hal_impact_effect_wall = null

/obj/projectile/hallucination/ebow/hal_apply_effect()
	hal_target.Paralyze(100)
	hal_target.stuttering += 5
	hal_target.adjustStaminaLoss(8)

/obj/projectile/hallucination/change
	name = "bolt of change"
	damage_type = BURN
	hal_icon_state = "ice_1"
	hal_fire_sound = 'sound/magic/staff_change.ogg'
	hal_hitsound = null
	hal_hitsound_wall = null
	hal_impact_effect = null
	hal_impact_effect_wall = null

/obj/projectile/hallucination/change/hal_apply_effect()
	new /datum/hallucination/self_delusion(hal_target, TRUE, wabbajack = FALSE)

/obj/projectile/hallucination/death
	name = "bolt of death"
	damage_type = BURN
	hal_icon_state = "pulse1_bl"
	hal_fire_sound = 'sound/magic/wandodeath.ogg'
	hal_hitsound = null
	hal_hitsound_wall = null
	hal_impact_effect = null
	hal_impact_effect_wall = null

/obj/projectile/hallucination/death/hal_apply_effect()
	new /datum/hallucination/death(hal_target, TRUE)
