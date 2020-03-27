/obj/structure/target_stake
	name = "target stake"
	desc = "A thin platform with negatively-magnetized wheels."
	icon = 'icons/obj/objects.dmi'
	icon_state = "target_stake"
	density = FALSE
	flags_1 = CONDUCT_1
	can_buckle = TRUE
	max_buckled_mobs = 1
	buckle_lying = FALSE
	var/obj/item/target/pinned_target

/obj/structure/target_stake/Destroy()
	if(pinned_target)
		pinned_target.nullPinnedLoc()
	return ..()

/obj/structure/target_stake/proc/handle_density()
	if(length(buckled_mobs) || pinned_target)
		density = TRUE
	else
		density = FALSE

/obj/structure/target_stake/post_buckle_mob()
	handle_density()
	return ..()

/obj/structure/target_stake/post_unbuckle_mob()
	handle_density()
	return ..()

/obj/structure/target_stake/proc/nullPinnedTarget()
	pinned_target = null

/obj/structure/target_stake/Move()
	. = ..()
	if(pinned_target)
		pinned_target.forceMove(loc)

/obj/structure/target_stake/attackby(obj/item/target/T, mob/user)
	if(pinned_target)
		return
	if(istype(T) && user.transferItemToLoc(T, drop_location()))
		pinned_target = T
		T.pinnedLoc = src
		T.density = TRUE
		T.layer = OBJ_LAYER + 0.01
		handle_density()
		to_chat(user, "<span class='notice'>You slide the target into the stake.</span>")

/obj/structure/target_stake/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(pinned_target)
		removeTarget(user)

/obj/structure/target_stake/proc/removeTarget(mob/user)
	pinned_target.layer = OBJ_LAYER
	pinned_target.forceMove(user.loc)
	pinned_target.nullPinnedLoc()
	nullPinnedTarget()
	handle_density()
	if(ishuman(user))
		if(!user.get_active_held_item())
			user.put_in_hands(pinned_target)
			to_chat(user, "<span class='notice'>You take the target out of the stake.</span>")
	else
		pinned_target.forceMove(user.drop_location())
		to_chat(user, "<span class='notice'>You take the target out of the stake.</span>")

/obj/structure/target_stake/bullet_act(obj/projectile/P)
	if(pinned_target)
		pinned_target.bullet_act(P)
	else
		. = ..()
