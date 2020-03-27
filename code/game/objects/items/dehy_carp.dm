/*
 *	Dehydrated Carp
 *	Instant carp, just add water
 */

//Child of carpplushie because this should do everything the toy does and more
/obj/item/toy/plush/carpplushie/dehy_carp
	var/mob/owner = null	//Carp doesn't attack owner, set when using in hand
	var/owned = 0	//Boolean, no owner to begin with
	var/mobtype = /mob/living/simple_animal/hostile/carp //So admins can change what mob spawns via var fuckery

//Attack self
/obj/item/toy/plush/carpplushie/dehy_carp/attack_self(mob/user)
	src.add_fingerprint(user)	//Anyone can add their fingerprints to it with this
	if(!owned)
		to_chat(user, "<span class='notice'>You pet [src]. You swear it looks up at you.</span>")
		owner = user
		owned = 1
	else
		return ..()

/obj/item/toy/plush/carpplushie/dehy_carp/plop(obj/item/toy/plush/Daddy)
	return FALSE

/obj/item/toy/plush/carpplushie/dehy_carp/proc/Swell()
	desc = "It's growing!"
	visible_message("<span class='notice'>[src] swells up!</span>")

	//Animation
	icon = 'icons/mob/carp.dmi'
	flick("carp_swell", src)
	//Wait for animation to end
	sleep(6)
	if(!src || QDELETED(src))//we got toasted while animating
		return
	//Make space carp
	var/mob/living/M = new mobtype(get_turf(src))
	//Make carp non-hostile to user, and their allies
	if(owner)
		var/list/factions = owner.faction.Copy()
		for(var/F in factions)
			if(F == "neutral")
				factions -= F
		M.faction = factions
	if (!owner || owner.faction != M.faction)
		visible_message("<span class='warning'>You have a bad feeling about this.</span>") //welcome to the hostile carp enjoy your die
	else
		visible_message("<span class='notice'>The newly grown [M.name] looks up at you with friendly eyes.</span>")
	qdel(src)
	
/obj/item/toy/plush/carpplushie/dehy_carp/suicide_act(mob/user)
	var/mob/living/carbon/human/H = user
	user.visible_message("<span class='suicide'>[user] starts eating [src]. It looks like [user.p_theyre()] trying to commit suicide!</span>")
	playsound(src, 'sound/items/eatfood.ogg', 50, TRUE)
	if(istype(H))
		H.Paralyze(30)
		forceMove(H) //we move it AWAAAYY
		sleep(20)
		
		if(QDELETED(src))
			return SHAME
		if(!QDELETED(H))
			H.spawn_gibs()
			H.apply_damage(200, def_zone = BODY_ZONE_CHEST)
			forceMove(get_turf(H)) //we move it back
		icon = 'icons/mob/carp.dmi'
		flick("carp_swell", src)
		sleep(6) //let the animation play out
	
		if(!QDELETED(src))
			var/mob/living/M = new mobtype(get_turf(src))
			M.faction = list("neutral")
			qdel(src)
	return BRUTELOSS
