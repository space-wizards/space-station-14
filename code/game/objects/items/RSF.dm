/*
CONTAINS:
RSF

*/
/obj/item/rsf
	name = "\improper Rapid-Service-Fabricator"
	desc = "A device used to rapidly deploy service items."
	icon = 'icons/obj/tools.dmi'
	icon_state = "rsf"
	lefthand_file = 'icons/mob/inhands/equipment/tools_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/tools_righthand.dmi'
	opacity = 0
	density = FALSE
	anchored = FALSE
	item_flags = NOBLUDGEON
	armor = list("melee" = 0, "bullet" = 0, "laser" = 0, "energy" = 0, "bomb" = 0, "bio" = 0, "rad" = 0, "fire" = 0, "acid" = 0)
	var/matter = 0
	var/mode = 1
	w_class = WEIGHT_CLASS_NORMAL

/obj/item/rsf/examine(mob/user)
	. = ..()
	. += "<span class='notice'>It currently holds [matter]/30 fabrication-units.</span>"

/obj/item/rsf/cyborg
	matter = 30

/obj/item/rsf/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/rcd_ammo))
		if((matter + 10) > 30)
			to_chat(user, "<span class='warning'>The RSF can't hold any more matter!</span>")
			return
		qdel(W)
		matter += 10
		playsound(src.loc, 'sound/machines/click.ogg', 10, TRUE)
		to_chat(user, "<span class='notice'>The RSF now holds [matter]/30 fabrication-units.</span>")
		icon_state = "rsf"
	else
		return ..()

/obj/item/rsf/attack_self(mob/user)
	playsound(src.loc, 'sound/effects/pop.ogg', 50, FALSE)
	switch(mode)
		if(5)
			mode = 1
			to_chat(user, "<span class='notice'>Changed dispensing mode to 'Drinking Glass'.</span>")
		if(1)
			mode = 2
			to_chat(user, "<span class='notice'>Changed dispensing mode to 'Paper'.</span>")
		if(2)
			mode = 3
			to_chat(user, "<span class='notice'>Changed dispensing mode to 'Pen'.</span>")
		if(3)
			mode = 4
			to_chat(user, "<span class='notice'>Changed dispensing mode to 'Dice Pack'.</span>")
		if(4)
			mode = 5
			to_chat(user, "<span class='notice'>Changed dispensing mode to 'Cigarette'.</span>")
	// Change mode

/obj/item/rsf/afterattack(atom/A, mob/user, proximity)
	. = ..()
	if(!proximity)
		return
	if (!(istype(A, /obj/structure/table) || isfloorturf(A)))
		return

	if(iscyborg(user))
		var/mob/living/silicon/robot/R = user
		if(!R.cell || R.cell.charge < 200)
			to_chat(user, "<span class='warning'>You do not have enough power to use [src].</span>")
			icon_state = "rsf_empty"
			return
	else if (matter < 1)
		to_chat(user, "<span class='warning'>\The [src] doesn't have enough matter left.</span>")
		icon_state = "rsf_empty"
		return

	var/turf/T = get_turf(A)
	playsound(src.loc, 'sound/machines/click.ogg', 10, TRUE)
	switch(mode)
		if(1)
			to_chat(user, "<span class='notice'>Dispensing Drinking Glass...</span>")
			new /obj/item/reagent_containers/food/drinks/drinkingglass(T)
			use_matter(20, user)
		if(2)
			to_chat(user, "<span class='notice'>Dispensing Paper Sheet...</span>")
			new /obj/item/paper(T)
			use_matter(10, user)
		if(3)
			to_chat(user, "<span class='notice'>Dispensing Pen...</span>")
			new /obj/item/pen(T)
			use_matter(50, user)
		if(4)
			to_chat(user, "<span class='notice'>Dispensing Dice Pack...</span>")
			new /obj/item/storage/pill_bottle/dice(T)
			use_matter(200, user)
		if(5)
			to_chat(user, "<span class='notice'>Dispensing Cigarette...</span>")
			new /obj/item/clothing/mask/cigarette(T)
			use_matter(10, user)

/obj/item/rsf/proc/use_matter(charge, mob/user)
	if (iscyborg(user))
		var/mob/living/silicon/robot/R = user
		R.cell.charge -= charge
	else
		matter--
		to_chat(user, "<span class='notice'>The RSF now holds [matter]/30 fabrication-units.</span>")

/obj/item/cookiesynth
	name = "Cookie Synthesizer"
	desc = "A self-recharging device used to rapidly deploy cookies."
	icon = 'icons/obj/tools.dmi'
	icon_state = "rcd"
	lefthand_file = 'icons/mob/inhands/equipment/tools_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/tools_righthand.dmi'
	var/matter = 10
	var/toxin = 0
	var/cooldown = 0
	var/cooldowndelay = 10
	w_class = WEIGHT_CLASS_NORMAL

/obj/item/cookiesynth/examine(mob/user)
	. = ..()
	. += "<span class='notice'>It currently holds [matter]/10 cookie-units.</span>"

/obj/item/cookiesynth/attackby()
	return

/obj/item/cookiesynth/emag_act(mob/user)
	obj_flags ^= EMAGGED
	if(obj_flags & EMAGGED)
		to_chat(user, "<span class='warning'>You short out [src]'s reagent safety checker!</span>")
	else
		to_chat(user, "<span class='warning'>You reset [src]'s reagent safety checker!</span>")
		toxin = 0

/obj/item/cookiesynth/attack_self(mob/user)
	var/mob/living/silicon/robot/P = null
	if(iscyborg(user))
		P = user
	if((obj_flags & EMAGGED)&&!toxin)
		toxin = 1
		to_chat(user, "<span class='alert'>Cookie Synthesizer hacked.</span>")
	else if(P.emagged&&!toxin)
		toxin = 1
		to_chat(user, "<span class='alert'>Cookie Synthesizer hacked.</span>")
	else
		toxin = 0
		to_chat(user, "<span class='notice'>Cookie Synthesizer reset.</span>")

/obj/item/cookiesynth/process()
	if(matter < 10)
		matter++

/obj/item/cookiesynth/afterattack(atom/A, mob/user, proximity)
	. = ..()
	if(cooldown > world.time)
		return
	if(!proximity)
		return
	if (!(istype(A, /obj/structure/table) || isfloorturf(A)))
		return
	if(matter < 1)
		to_chat(user, "<span class='warning'>[src] doesn't have enough matter left. Wait for it to recharge!</span>")
		return
	if(iscyborg(user))
		var/mob/living/silicon/robot/R = user
		if(!R.cell || R.cell.charge < 400)
			to_chat(user, "<span class='warning'>You do not have enough power to use [src].</span>")
			return
	var/turf/T = get_turf(A)
	playsound(src.loc, 'sound/machines/click.ogg', 10, TRUE)
	to_chat(user, "<span class='notice'>Fabricating Cookie...</span>")
	var/obj/item/reagent_containers/food/snacks/cookie/S = new /obj/item/reagent_containers/food/snacks/cookie(T)
	if(toxin)
		S.reagents.add_reagent(/datum/reagent/toxin/chloralhydrate, 10)
	if (iscyborg(user))
		var/mob/living/silicon/robot/R = user
		R.cell.charge -= 100
	else
		matter--
	cooldown = world.time + cooldowndelay
