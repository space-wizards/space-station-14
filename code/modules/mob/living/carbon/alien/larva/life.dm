

/mob/living/carbon/alien/larva/Life()
	set invisibility = 0
	if (notransform)
		return
	if(..() && !IS_IN_STASIS(src)) //not dead and not in stasis
		// GROW!
		if(amount_grown < max_grown)
			amount_grown++
			update_icons()


/mob/living/carbon/alien/larva/update_stat()
	if(status_flags & GODMODE)
		return
	if(stat != DEAD)
		if(health<= -maxHealth || !getorgan(/obj/item/organ/brain))
			death()
			return
		if(IsUnconscious() || IsSleeping() || getOxyLoss() > 50 || (HAS_TRAIT(src, TRAIT_DEATHCOMA)) || health <= crit_threshold)
			if(stat == CONSCIOUS)
				set_stat(UNCONSCIOUS)
				become_blind(UNCONSCIOUS_BLIND)
				update_mobility()
		else
			if(stat == UNCONSCIOUS)
				set_stat(CONSCIOUS)
				cure_blind(UNCONSCIOUS_BLIND)
				set_resting(FALSE)
	update_damage_hud()
	update_health_hud()
