/obj/item/target
	name = "shooting target"
	desc = "A shooting target."
	icon = 'icons/obj/objects.dmi'
	icon_state = "target_h"
	density = FALSE
	var/hp = 1800
	var/obj/structure/target_stake/pinnedLoc

/obj/item/target/Destroy()
	removeOverlays()
	if(pinnedLoc)
		pinnedLoc.nullPinnedTarget()
	return ..()

/obj/item/target/proc/nullPinnedLoc()
	pinnedLoc = null
	density = FALSE

/obj/item/target/proc/removeOverlays()
	cut_overlays()

/obj/item/target/Move()
	. = ..()
	if(pinnedLoc)
		pinnedLoc.forceMove(loc)

/obj/item/target/welder_act(mob/living/user, obj/item/I)
	..()
	if(I.use_tool(src, user, 0, volume=40))
		removeOverlays()
		to_chat(user, "<span class='notice'>You slice off [src]'s uneven chunks of aluminium and scorch marks.</span>")
	return TRUE

/obj/item/target/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(pinnedLoc)
		pinnedLoc.removeTarget(user)

/obj/item/target/syndicate
	icon_state = "target_s"
	desc = "A shooting target that looks like syndicate scum."
	hp = 2600

/obj/item/target/alien
	icon_state = "target_q"
	desc = "A shooting target that looks like a xenomorphic alien."
	hp = 2350

/obj/item/target/alien/anchored
	anchored = TRUE

/obj/item/target/clown
	icon_state = "target_c"
	desc = "A shooting target that looks like a useless clown."
	hp = 2000

#define DECALTYPE_SCORCH 1
#define DECALTYPE_BULLET 2

/obj/item/target/clown/bullet_act(obj/projectile/P)
	. = ..()
	playsound(src.loc, 'sound/items/bikehorn.ogg', 50, TRUE)

/obj/item/target/bullet_act(obj/projectile/P)
	if(istype(P, /obj/projectile/bullet/reusable)) // If it's a foam dart, don't bother with any of this other shit
		return P.on_hit(src, 0)
	var/p_x = P.p_x + pick(0,0,0,0,0,-1,1) // really ugly way of coding "sometimes offset P.p_x!"
	var/p_y = P.p_y + pick(0,0,0,0,0,-1,1)
	var/decaltype = DECALTYPE_SCORCH
	if(istype(P, /obj/projectile/bullet))
		decaltype = DECALTYPE_BULLET
	var/icon/C = icon(icon,icon_state)
	if(C.GetPixel(p_x, p_y) && P.original == src && overlays.len <= 35) // if the located pixel isn't blank (null)
		hp -= P.damage
		if(hp <= 0)
			visible_message("<span class='danger'>[src] breaks into tiny pieces and collapses!</span>")
			qdel(src)
		var/image/bullet_hole = image('icons/effects/effects.dmi', "scorch", OBJ_LAYER + 0.5)
		bullet_hole.pixel_x = p_x - 1 //offset correction
		bullet_hole.pixel_y = p_y - 1
		if(decaltype == DECALTYPE_SCORCH)
			bullet_hole.setDir(pick(NORTH,SOUTH,EAST,WEST))// random scorch design
			if(P.damage >= 20 || istype(P, /obj/projectile/beam/practice))
				bullet_hole.setDir(pick(NORTH,SOUTH,EAST,WEST))
			else
				bullet_hole.icon_state = "light_scorch"
		else
			bullet_hole.icon_state = "dent"
		add_overlay(bullet_hole)
		return BULLET_ACT_HIT
	return BULLET_ACT_FORCE_PIERCE

#undef DECALTYPE_SCORCH
#undef DECALTYPE_BULLET
