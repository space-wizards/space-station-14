/datum/martial_art/mushpunch
	name = "Mushroom Punch"
	id = MARTIALART_MUSHPUNCH

/datum/martial_art/mushpunch/harm_act(mob/living/carbon/human/A, mob/living/carbon/human/D)
	var/atk_verb
	to_chat(A, "<span class='spider'>You begin to wind up an attack...</span>")
	if(!do_after(A, 25, target = D))
		to_chat(A, "<span class='spider'><b>Your attack was interrupted!</b></span>")
		return TRUE //martial art code was a mistake
	A.do_attack_animation(D, ATTACK_EFFECT_PUNCH)
	atk_verb = pick("punch", "smash", "crack")
	D.visible_message("<span class='danger'>[A] [atk_verb]ed [D] with such inhuman strength that it sends [D.p_them()] flying backwards!</span>", \
					"<span class='userdanger'>You're [atk_verb]ed by [A] with such inhuman strength that it sends you flying backwards!</span>", "<span class='hear'>You hear a sickening sound of flesh hitting flesh!</span>", null, A)
	to_chat(A, "<span class='danger'>You [atk_verb] [D] with such inhuman strength that it sends [D.p_them()] flying backwards!</span>")
	D.apply_damage(rand(15,30), A.dna.species.attack_type)
	playsound(D, 'sound/effects/meteorimpact.ogg', 25, TRUE, -1)
	var/throwtarget = get_edge_target_turf(A, get_dir(A, get_step_away(D, A)))
	D.throw_at(throwtarget, 4, 2, A)//So stuff gets tossed around at the same time.
	D.Paralyze(20)
	if(atk_verb)
		log_combat(A, D, "[atk_verb] (Mushroom Punch)")
	return TRUE

/obj/item/mushpunch
	name = "odd mushroom"
	desc = "<I>Sapienza Ophioglossoides</I>:An odd mushroom from the flesh of a mushroom person. It has apparently retained some innate power of its owner, as it quivers with barely-contained POWER!"
	icon = 'icons/obj/hydroponics/seeds.dmi'
	icon_state = "mycelium-angel"

/obj/item/mushpunch/attack_self(mob/living/carbon/human/user)
	if(!istype(user) || !user)
		return
	var/message = "<span class='spider'>You devour [src], and a confluence of skill and power from the mushroom enhances your punches! You do need a short moment to charge these powerful punches.</span>"
	to_chat(user, message)
	var/datum/martial_art/mushpunch/mush = new(null)
	mush.teach(user)
	qdel(src)
	visible_message("<span class='warning'>[user] devours [src].</span>", \
					"<span class='notice'>You devour [src].</span>")
