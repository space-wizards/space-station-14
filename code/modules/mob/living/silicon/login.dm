/mob/living/silicon/Login()
	if(mind && SSticker.mode)
		SSticker.mode.remove_cultist(mind, 0, 0)
		var/datum/antagonist/rev/rev = mind.has_antag_datum(/datum/antagonist/rev)
		if(rev)
			rev.remove_revolutionary(TRUE)
	..()


/mob/living/silicon/auto_deadmin_on_login()
	if(!client?.holder)
		return TRUE
	if(CONFIG_GET(flag/auto_deadmin_silicons) || (client.prefs?.toggles & DEADMIN_POSITION_SILICON))
		return client.holder.auto_deadmin()
	return ..()
