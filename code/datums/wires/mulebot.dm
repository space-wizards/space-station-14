/datum/wires/mulebot
	holder_type = /mob/living/simple_animal/bot/mulebot
	randomize = TRUE

/datum/wires/mulebot/New(atom/holder)
	wires = list(
		WIRE_POWER1, WIRE_POWER2,
		WIRE_AVOIDANCE, WIRE_LOADCHECK,
		WIRE_MOTOR1, WIRE_MOTOR2,
		WIRE_RX, WIRE_TX, WIRE_BEACON
	)
	..()

/datum/wires/mulebot/interactable(mob/user)
	var/mob/living/simple_animal/bot/mulebot/M = holder
	if(M.open)
		return TRUE

/datum/wires/mulebot/on_pulse(wire)
	var/mob/living/simple_animal/bot/mulebot/M = holder
	switch(wire)
		if(WIRE_POWER1, WIRE_POWER2)
			holder.visible_message("<span class='notice'>[icon2html(M, viewers(holder))] The charge light flickers.</span>")
		if(WIRE_AVOIDANCE)
			holder.visible_message("<span class='notice'>[icon2html(M, viewers(holder))] The external warning lights flash briefly.</span>")
		if(WIRE_LOADCHECK)
			holder.visible_message("<span class='notice'>[icon2html(M, viewers(holder))] The load platform clunks.</span>")
		if(WIRE_MOTOR1, WIRE_MOTOR2)
			holder.visible_message("<span class='notice'>[icon2html(M, viewers(holder))] The drive motor whines briefly.</span>")
		else
			holder.visible_message("<span class='notice'>[icon2html(M, viewers(holder))] You hear a radio crackle.</span>")
