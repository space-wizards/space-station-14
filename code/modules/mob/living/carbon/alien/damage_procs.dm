
/mob/living/carbon/alien/getToxLoss()
	return 0

/mob/living/carbon/alien/adjustToxLoss(amount, updating_health = TRUE, forced = FALSE) //alien immune to tox damage
	return FALSE

//aliens are immune to stamina damage.
/mob/living/carbon/alien/adjustStaminaLoss(amount, updating_health = 1, forced = FALSE)
	return

/mob/living/carbon/alien/setStaminaLoss(amount, updating_health = 1)
	return
