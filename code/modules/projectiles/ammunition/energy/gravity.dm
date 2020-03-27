/obj/item/ammo_casing/energy/gravity
	e_cost = 0
	fire_sound = 'sound/weapons/wave.ogg'
	select_name = "gravity"
	delay = 50
	var/obj/item/gun/energy/gravity_gun/gun

/obj/item/ammo_casing/energy/gravity/Initialize(mapload)
	if(istype(loc,/obj/item/gun/energy/gravity_gun))
		gun = loc
	. = ..()

/obj/item/ammo_casing/energy/gravity/Destroy()
	gun = null
	. = ..()

/obj/item/ammo_casing/energy/gravity/repulse
	projectile_type = /obj/projectile/gravityrepulse
	select_name = "repulse"

/obj/item/ammo_casing/energy/gravity/attract
	projectile_type = /obj/projectile/gravityattract
	select_name = "attract"

/obj/item/ammo_casing/energy/gravity/chaos
	projectile_type = /obj/projectile/gravitychaos
	select_name = "chaos"


