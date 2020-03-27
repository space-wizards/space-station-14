/mob/living/proc/robot_talk(message)
	log_talk(message, LOG_SAY, tag="binary")
	var/desig = "Default Cyborg" //ezmode for taters
	if(issilicon(src))
		var/mob/living/silicon/S = src
		desig = trim_left(S.designation + " " + S.job)
	var/message_a = say_quote(message)
	var/rendered = "Robotic Talk, <span class='name'>[name]</span> <span class='message'>[message_a]</span>"
	for(var/mob/M in GLOB.player_list)
		if(M.binarycheck())
			if(isAI(M))
				var/renderedAI = "<span class='binarysay'>Robotic Talk, <a href='?src=[REF(M)];track=[html_encode(name)]'><span class='name'>[name] ([desig])</span></a> <span class='message'>[message_a]</span></span>"
				to_chat(M, renderedAI)
			else
				to_chat(M, "<span class='binarysay'>[rendered]</span>")
		if(isobserver(M))
			var/following = src
			// If the AI talks on binary chat, we still want to follow
			// it's camera eye, like if it talked on the radio
			if(isAI(src))
				var/mob/living/silicon/ai/ai = src
				following = ai.eyeobj
			var/link = FOLLOW_LINK(M, following)
			to_chat(M, "<span class='binarysay'>[link] [rendered]</span>")

/mob/living/silicon/binarycheck()
	return 1

/mob/living/silicon/lingcheck()
	return 0 //Borged or AI'd lings can't speak on the ling channel.

/mob/living/silicon/radio(message, message_mode, list/spans, language)
	. = ..()
	if(. != 0)
		return .

	if(message_mode == "robot")
		if (radio)
			radio.talk_into(src, message, , spans, language)
		return REDUCE_RANGE

	else if(message_mode in GLOB.radiochannels)
		if(radio)
			radio.talk_into(src, message, message_mode, spans, language)
			return ITALICS | REDUCE_RANGE

	return 0

/mob/living/silicon/get_message_mode(message)
	. = ..()
	if(..() == MODE_HEADSET)
		return MODE_ROBOT
	else
		return .
