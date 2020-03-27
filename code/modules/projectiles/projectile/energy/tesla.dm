/obj/projectile/energy/tesla
	name = "tesla bolt"
	icon_state = "tesla_projectile"
	impact_effect_type = /obj/effect/temp_visual/impact_effect/blue_laser
	var/chain
	var/zap_flags = ZAP_MOB_DAMAGE | ZAP_OBJ_DAMAGE | ZAP_IS_TESLA
	var/zap_range = 3
	var/power = 10000

/obj/projectile/energy/tesla/fire(setAngle)
	if(firer)
		chain = firer.Beam(src, icon_state = "lightning[rand(1, 12)]", time = INFINITY, maxdistance = INFINITY)
	..()

/obj/projectile/energy/tesla/on_hit(atom/target)
	. = ..()
	tesla_zap(target, zap_range, power, zap_flags)
	qdel(src)

/obj/projectile/energy/tesla/Destroy()
	QDEL_NULL(chain)
	return ..()

/obj/projectile/energy/tesla/revolver
	name = "energy orb"

/obj/projectile/energy/tesla/cannon
	name = "tesla orb"
	power = 20000
