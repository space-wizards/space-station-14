/obj/item/ammo_casing/energy/wormhole
	projectile_type = /obj/projectile/beam/wormhole
	e_cost = 0
	harmful = FALSE
	fire_sound = 'sound/weapons/pulse3.ogg'
	select_name = "blue"
	var/obj/item/gun/energy/wormhole_projector/gun

/obj/item/ammo_casing/energy/wormhole/orange
	projectile_type = /obj/projectile/beam/wormhole/orange
	select_name = "orange"

/obj/item/ammo_casing/energy/wormhole/Initialize(mapload, obj/item/gun/energy/wormhole_projector/wh)
	. = ..()
	gun = wh

/obj/item/ammo_casing/energy/wormhole/throw_proj()
	. = ..()
	if(istype(BB, /obj/projectile/beam/wormhole))
		var/obj/projectile/beam/wormhole/WH = BB
		WH.gun = gun
