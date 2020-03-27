/obj/projectile/ion
	name = "ion bolt"
	icon_state = "ion"
	damage = 0
	damage_type = BURN
	nodamage = TRUE
	flag = "energy"
	impact_effect_type = /obj/effect/temp_visual/impact_effect/ion
	var/emp_radius = 1

/obj/projectile/ion/on_hit(atom/target, blocked = FALSE)
	..()
	empulse(target, emp_radius, emp_radius)
	return BULLET_ACT_HIT

/obj/projectile/ion/weak
	emp_radius = 0
