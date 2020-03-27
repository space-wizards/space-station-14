/obj/projectile/beam/wormhole
	name = "bluespace beam"
	icon_state = "spark"
	hitsound = "sparks"
	damage = 0
	nodamage = TRUE
	pass_flags = PASSGLASS | PASSTABLE | PASSGRILLE | PASSMOB
	var/obj/item/gun/energy/wormhole_projector/gun
	color = "#33CCFF"
	tracer_type = /obj/effect/projectile/tracer/wormhole
	impact_type = /obj/effect/projectile/impact/wormhole
	muzzle_type = /obj/effect/projectile/muzzle/wormhole
	hitscan = TRUE

/obj/projectile/beam/wormhole/orange
	name = "orange bluespace beam"
	color = "#FF6600"

/obj/projectile/beam/wormhole/Initialize(mapload, obj/item/ammo_casing/energy/wormhole/casing)
	. = ..()
	if(casing)
		gun = casing.gun


/obj/projectile/beam/wormhole/on_hit(atom/target)
	if(!gun)
		qdel(src)
		return
	gun.create_portal(src, get_turf(src))
