/obj/effect/acid
	gender = PLURAL
	name = "acid"
	desc = "Burbling corrosive stuff."
	icon_state = "acid"
	density = FALSE
	opacity = 0
	anchored = TRUE
	resistance_flags = FIRE_PROOF | UNACIDABLE | ACID_PROOF
	layer = ABOVE_NORMAL_TURF_LAYER
	var/turf/target


/obj/effect/acid/Initialize(mapload, acid_pwr, acid_amt)
	. = ..()

	target = get_turf(src)

	if(acid_amt)
		acid_level = min(acid_amt*acid_pwr, 12000) //capped so the acid effect doesn't last a half hour on the floor.

	//handle APCs and newscasters and stuff nicely
	pixel_x = target.pixel_x + rand(-4,4)
	pixel_y = target.pixel_y + rand(-4,4)

	START_PROCESSING(SSobj, src)


/obj/effect/acid/Destroy()
	STOP_PROCESSING(SSobj, src)
	target = null
	return ..()

/obj/effect/acid/process()
	. = 1
	if(!target)
		qdel(src)
		return 0

	if(prob(5))
		playsound(loc, 'sound/items/welder.ogg', 100, TRUE)

	for(var/obj/O in target)
		if(prob(20) && !(resistance_flags & UNACIDABLE))
			if(O.acid_level < acid_level*0.3)
				var/acid_used = min(acid_level*0.05, 20)
				O.acid_act(10, acid_used)
				acid_level = max(0, acid_level - acid_used*10)

	acid_level = max(acid_level - (5 + 2*round(sqrt(acid_level))), 0)
	if(acid_level <= 0)
		qdel(src)
		return 0

/obj/effect/acid/Crossed(AM as mob|obj)
	if(isliving(AM))
		var/mob/living/L = AM
		if(L.movement_type & FLYING)
			return
		if(L.m_intent != MOVE_INTENT_WALK && prob(40))
			var/acid_used = min(acid_level*0.05, 20)
			if(L.acid_act(10, acid_used, "feet"))
				acid_level = max(0, acid_level - acid_used*10)
				playsound(L, 'sound/weapons/sear.ogg', 50, TRUE)
				to_chat(L, "<span class='userdanger'>[src] burns you!</span>")

//xenomorph corrosive acid
/obj/effect/acid/alien
	var/target_strength = 30


/obj/effect/acid/alien/process()
	. = ..()
	if(.)
		if(prob(45))
			playsound(loc, 'sound/items/welder.ogg', 100, TRUE)
		target_strength--
		if(target_strength <= 0)
			target.visible_message("<span class='warning'>[target] collapses under its own weight into a puddle of goop and undigested debris!</span>")
			target.acid_melt()
			qdel(src)
		else

			switch(target_strength)
				if(24)
					visible_message("<span class='warning'>[target] is holding up against the acid!</span>")
				if(16)
					visible_message("<span class='warning'>[target] is being melted by the acid!</span>")
				if(8)
					visible_message("<span class='warning'>[target] is struggling to withstand the acid!</span>")
				if(4)
					visible_message("<span class='warning'>[target] begins to crumble under the acid!</span>")
