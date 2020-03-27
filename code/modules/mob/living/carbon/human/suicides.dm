/mob/living/carbon/human/proc/delayed_suicide()
	suicide_log()
	adjustBruteLoss(max(200 - getToxLoss() - getFireLoss() - getBruteLoss() - getOxyLoss(), 0))
	death(FALSE)
	ghostize(FALSE)	// Disallows reentering body and disassociates mind

/mob/living/carbon/human/proc/disarm_suicide()
	var/suicide_message = "[src] is ripping [p_their()] own arms off! It looks like [p_theyre()] trying to commit suicide." //heheh get it?
	visible_message("<span class='danger'>[suicide_message]</span>", "<span class='userdanger'>[suicide_message]</span>")

	var/timer = 15
	for(var/obj/item/bodypart/thing in bodyparts)
		if(thing.body_part == ARM_LEFT || thing.body_part == ARM_RIGHT)
			addtimer(CALLBACK(thing, /obj/item/bodypart/.proc/dismember), timer)
			addtimer(CALLBACK(GLOBAL_PROC, .proc/playsound, src, 'sound/effects/cartoon_pop.ogg', 70), timer)
			timer += 15
	addtimer(CALLBACK(src, /mob/living/carbon/human/.proc/delayed_suicide, FALSE), timer-10)
