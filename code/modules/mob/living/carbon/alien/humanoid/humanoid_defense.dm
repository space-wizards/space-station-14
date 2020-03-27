

/mob/living/carbon/alien/humanoid/attack_hulk(mob/living/carbon/human/user)
	. = ..()
	if(!.)
		return
	adjustBruteLoss(15)
	var/hitverb = "hit"
	if(mob_size < MOB_SIZE_LARGE)
		safe_throw_at(get_edge_target_turf(src, get_dir(user, src)), 2, 1, user)
		hitverb = "slam"
	playsound(loc, "punch", 25, TRUE, -1)
	visible_message("<span class='danger'>[user] [hitverb]s [src]!</span>", \
					"<span class='userdanger'>[user] [hitverb]s you!</span>", "<span class='hear'>You hear a sickening sound of flesh hitting flesh!</span>", COMBAT_MESSAGE_RANGE, user)
	to_chat(user, "<span class='danger'>You [hitverb] [src]!</span>")

/mob/living/carbon/alien/humanoid/attack_hand(mob/living/carbon/human/M)
	if(..())
		switch(M.a_intent)
			if ("harm")
				var/damage = rand(1, 9)
				if (prob(90))
					playsound(loc, "punch", 25, TRUE, -1)
					visible_message("<span class='danger'>[M] punches [src]!</span>", \
									"<span class='userdanger'>[M] punches you!</span>", "<span class='hear'>You hear a sickening sound of flesh hitting flesh!</span>", COMBAT_MESSAGE_RANGE, M)
					to_chat(M, "<span class='danger'>You punch [src]!</span>")
					if ((stat != DEAD) && (damage > 9 || prob(5)))//Regular humans have a very small chance of knocking an alien down.
						Unconscious(40)
						visible_message("<span class='danger'>[M] knocks [src] down!</span>", \
										"<span class='userdanger'>[M] knocks you down!</span>", "<span class='hear'>You hear a sickening sound of flesh hitting flesh!</span>", null, M)
						to_chat(M, "<span class='danger'>You knock [src] down!</span>")
					var/obj/item/bodypart/affecting = get_bodypart(ran_zone(M.zone_selected))
					apply_damage(damage, BRUTE, affecting)
					log_combat(M, src, "attacked")
				else
					playsound(loc, 'sound/weapons/punchmiss.ogg', 25, TRUE, -1)
					visible_message("<span class='danger'>[M]'s punch misses [src]!</span>", \
									"<span class='danger'>You avoid [M]'s punch!</span>", "<span class='hear'>You hear a swoosh!</span>", COMBAT_MESSAGE_RANGE, M)
					to_chat(M, "<span class='warning'>Your punch misses [src]!</span>")

			if ("disarm")
				if (!(mobility_flags & MOBILITY_STAND))
					if (prob(5))
						Unconscious(40)
						playsound(loc, 'sound/weapons/thudswoosh.ogg', 50, TRUE, -1)
						log_combat(M, src, "pushed")
						visible_message("<span class='danger'>[M] pushes [src] down!</span>", \
										"<span class='userdanger'>[M] pushes you down!</span>", "<span class='hear'>You hear aggressive shuffling followed by a loud thud!</span>", null, M)
						to_chat(M, "<span class='danger'>You push [src] down!</span>")
					else
						if (prob(50))
							dropItemToGround(get_active_held_item())
							playsound(loc, 'sound/weapons/thudswoosh.ogg', 50, TRUE, -1)
							visible_message("<span class='danger'>[M] disarms [src]!</span>", \
											"<span class='userdanger'>[M] disarms you!</span>", "<span class='hear'>You hear aggressive shuffling!</span>", COMBAT_MESSAGE_RANGE, M)
							to_chat(M, "<span class='danger'>You disarm [src]!</span>")
						else
							playsound(loc, 'sound/weapons/punchmiss.ogg', 25, TRUE, -1)
							visible_message("<span class='danger'>[M] fails to disarm [src]!</span>",\
											"<span class='danger'>[M] fails to disarm you!</span>", "<span class='hear'>You hear a swoosh!</span>", COMBAT_MESSAGE_RANGE, M)
							to_chat(M, "<span class='warning'>You fail to disarm [src]!</span>")



/mob/living/carbon/alien/humanoid/do_attack_animation(atom/A, visual_effect_icon, obj/item/used_item, no_effect)
	if(!no_effect && !visual_effect_icon)
		visual_effect_icon = ATTACK_EFFECT_CLAW
	..()
