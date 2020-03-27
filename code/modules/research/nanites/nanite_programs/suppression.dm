//Programs that are generally useful for population control and non-harmful suppression.

/datum/nanite_program/sleepy
	name = "Sleep Induction"
	desc = "The nanites cause rapid narcolepsy when triggered."
	can_trigger = TRUE
	trigger_cost = 15
	trigger_cooldown = 1200
	rogue_types = list(/datum/nanite_program/brain_misfire, /datum/nanite_program/brain_decay)

/datum/nanite_program/sleepy/on_trigger(comm_message)
	to_chat(host_mob, "<span class='warning'>You start to feel very sleepy...</span>")
	host_mob.drowsyness += 20
	addtimer(CALLBACK(host_mob, /mob/living.proc/Sleeping, 200), rand(60,200))

/datum/nanite_program/paralyzing
	name = "Paralysis"
	desc = "The nanites force muscle contraction, effectively paralyzing the host."
	use_rate = 3
	rogue_types = list(/datum/nanite_program/nerve_decay)

/datum/nanite_program/paralyzing/active_effect()
	host_mob.Stun(40)

/datum/nanite_program/paralyzing/enable_passive_effect()
	. = ..()
	to_chat(host_mob, "<span class='warning'>Your muscles seize! You can't move!</span>")

/datum/nanite_program/paralyzing/disable_passive_effect()
	. = ..()
	to_chat(host_mob, "<span class='notice'>Your muscles relax, and you can move again.</span>")

/datum/nanite_program/shocking
	name = "Electric Shock"
	desc = "The nanites shock the host when triggered. Destroys a large amount of nanites!"
	can_trigger = TRUE
	trigger_cost = 10
	trigger_cooldown = 300
	program_flags = NANITE_SHOCK_IMMUNE
	rogue_types = list(/datum/nanite_program/toxic)

/datum/nanite_program/shocking/on_trigger(comm_message)
	host_mob.electrocute_act(rand(5,10), "shock nanites", 1, SHOCK_NOGLOVES)

/datum/nanite_program/stun
	name = "Neural Shock"
	desc = "The nanites pulse the host's nerves when triggered, inapacitating them for a short period."
	can_trigger = TRUE
	trigger_cost = 4
	trigger_cooldown = 300
	rogue_types = list(/datum/nanite_program/shocking, /datum/nanite_program/nerve_decay)

/datum/nanite_program/stun/on_trigger(comm_message)
	playsound(host_mob, "sparks", 75, TRUE, -1)
	host_mob.Paralyze(80)

/datum/nanite_program/pacifying
	name = "Pacification"
	desc = "The nanites suppress the aggression center of the brain, preventing the host from causing direct harm to others."
	use_rate = 1
	rogue_types = list(/datum/nanite_program/brain_misfire, /datum/nanite_program/brain_decay)

/datum/nanite_program/pacifying/enable_passive_effect()
	. = ..()
	ADD_TRAIT(host_mob, TRAIT_PACIFISM, "nanites")

/datum/nanite_program/pacifying/disable_passive_effect()
	. = ..()
	REMOVE_TRAIT(host_mob, TRAIT_PACIFISM, "nanites")

/datum/nanite_program/blinding
	name = "Blindness"
	desc = "The nanites suppress the host's ocular nerves, blinding them while they're active."
	use_rate = 1.5
	rogue_types = list(/datum/nanite_program/nerve_decay)

/datum/nanite_program/blinding/enable_passive_effect()
	. = ..()
	host_mob.become_blind("nanites")

/datum/nanite_program/blinding/disable_passive_effect()
	. = ..()
	host_mob.cure_blind("nanites")

/datum/nanite_program/mute
	name = "Mute"
	desc = "The nanites suppress the host's speech, making them mute while they're active."
	use_rate = 0.75
	rogue_types = list(/datum/nanite_program/brain_decay, /datum/nanite_program/brain_misfire)

/datum/nanite_program/mute/enable_passive_effect()
	. = ..()
	ADD_TRAIT(host_mob, TRAIT_MUTE, "nanites")

/datum/nanite_program/mute/disable_passive_effect()
	. = ..()
	REMOVE_TRAIT(host_mob, TRAIT_MUTE, "nanites")

/datum/nanite_program/fake_death
	name = "Death Simulation"
	desc = "The nanites induce a death-like coma into the host, able to fool most medical scans."
	use_rate = 3.5
	rogue_types = list(/datum/nanite_program/nerve_decay, /datum/nanite_program/necrotic, /datum/nanite_program/brain_decay)

/datum/nanite_program/fake_death/enable_passive_effect()
	. = ..()
	host_mob.emote("deathgasp")
	host_mob.fakedeath("nanites")

/datum/nanite_program/fake_death/disable_passive_effect()
	. = ..()
	host_mob.cure_fakedeath("nanites")

//Can receive transmissions from a nanite communication remote for customized messages
/datum/nanite_program/comm
	can_trigger = TRUE
	var/comm_code = 0
	var/comm_message = ""

/datum/nanite_program/comm/register_extra_settings()
	extra_settings[NES_COMM_CODE] = new /datum/nanite_extra_setting/number(0, 0, 9999)

/datum/nanite_program/comm/proc/receive_comm_signal(signal_comm_code, comm_message, comm_source)
	if(!activated || !comm_code)
		return
	if(signal_comm_code == comm_code)
		host_mob.investigate_log("'s [name] nanite program was messaged by [comm_source] with comm code [signal_comm_code] and message '[comm_message]'.", INVESTIGATE_NANITES)
		trigger(comm_message)

/datum/nanite_program/comm/speech
	name = "Forced Speech"
	desc = "The nanites force the host to say a pre-programmed sentence when triggered."
	unique = FALSE
	trigger_cost = 3
	trigger_cooldown = 20
	rogue_types = list(/datum/nanite_program/brain_misfire, /datum/nanite_program/brain_decay)
	var/static/list/blacklist = list(
		"*surrender",
		"*collapse"
	)

/datum/nanite_program/comm/speech/register_extra_settings()
	. = ..()
	extra_settings[NES_SENTENCE] = new /datum/nanite_extra_setting/text("")

/datum/nanite_program/comm/speech/on_trigger(comm_message)
	var/sent_message = comm_message
	if(!comm_message)
		var/datum/nanite_extra_setting/sentence = extra_settings[NES_SENTENCE]
		sent_message = sentence.get_value()
	if(sent_message in blacklist)
		return
	if(host_mob.stat == DEAD)
		return
	to_chat(host_mob, "<span class='warning'>You feel compelled to speak...</span>")
	host_mob.say(sent_message, forced = "nanite speech")

/datum/nanite_program/comm/voice
	name = "Skull Echo"
	desc = "The nanites echo a synthesized message inside the host's skull."
	unique = FALSE
	trigger_cost = 1
	trigger_cooldown = 20
	rogue_types = list(/datum/nanite_program/brain_misfire, /datum/nanite_program/brain_decay)

/datum/nanite_program/comm/voice/register_extra_settings()
	. = ..()
	extra_settings[NES_MESSAGE] = new /datum/nanite_extra_setting/text("")

/datum/nanite_program/comm/voice/on_trigger(comm_message)
	var/sent_message = comm_message
	if(!comm_message)
		var/datum/nanite_extra_setting/message_setting = extra_settings[NES_MESSAGE]
		sent_message = message_setting.get_value()
	if(host_mob.stat == DEAD)
		return
	to_chat(host_mob, "<i>You hear a strange, robotic voice in your head...</i> \"<span class='robot'>[sent_message]</span>\"")

/datum/nanite_program/comm/hallucination
	name = "Hallucination"
	desc = "The nanites make the host hallucinate something when triggered."
	trigger_cost = 4
	trigger_cooldown = 80
	unique = FALSE
	rogue_types = list(/datum/nanite_program/brain_misfire)

/datum/nanite_program/comm/hallucination/register_extra_settings()
	. = ..()
	var/list/options = list(
		"Message",
		"Battle",
		"Sound",
		"Weird Sound",
		"Station Message",
		"Health",
		"Alert",
		"Fire",
		"Shock",
		"Plasma Flood",
		"Random"
	)
	extra_settings[NES_HALLUCINATION_TYPE] = new /datum/nanite_extra_setting/type("Message", options)
	extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/text("")

/datum/nanite_program/comm/hallucination/on_trigger(comm_message)
	var/datum/nanite_extra_setting/hal_setting = extra_settings[NES_HALLUCINATION_TYPE]
	var/hal_type = hal_setting.get_value()
	var/datum/nanite_extra_setting/hal_detail_setting = extra_settings[NES_HALLUCINATION_DETAIL]
	var/hal_details = hal_detail_setting.get_value()
	if(comm_message && (hal_type != "Message")) //Triggered via comm remote, but not set to a message hallucination
		return
	var/sent_message = comm_message //Comm remotes can send custom hallucination messages for the chat hallucination
	if(!sent_message)
		sent_message = hal_details

	if(!iscarbon(host_mob))
		return
	var/mob/living/carbon/C = host_mob
	if(hal_details == "random")
		hal_details = null
	if(hal_type == "Random")
		C.hallucination += 15
	else
		switch(hal_type)
			if("Message")
				new /datum/hallucination/chat(C, TRUE, null, sent_message)
			if("Battle")
				new /datum/hallucination/battle(C, TRUE, hal_details)
			if("Sound")
				new /datum/hallucination/sounds(C, TRUE, hal_details)
			if("Weird Sound")
				new /datum/hallucination/weird_sounds(C, TRUE, hal_details)
			if("Station Message")
				new /datum/hallucination/stationmessage(C, TRUE, hal_details)
			if("Health")
				switch(hal_details)
					if("critical")
						hal_details = SCREWYHUD_CRIT
					if("dead")
						hal_details = SCREWYHUD_DEAD
					if("healthy")
						hal_details = SCREWYHUD_HEALTHY
				new /datum/hallucination/hudscrew(C, TRUE, hal_details)
			if("Alert")
				new /datum/hallucination/fake_alert(C, TRUE, hal_details)
			if("Fire")
				new /datum/hallucination/fire(C, TRUE)
			if("Shock")
				new /datum/hallucination/shock(C, TRUE)
			if("Plasma Flood")
				new /datum/hallucination/fake_flood(C, TRUE)

/datum/nanite_program/comm/hallucination/set_extra_setting(setting, value)
	. = ..()
	if(setting == NES_HALLUCINATION_TYPE)
		switch(value)
			if("Message")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/text("")
			if("Battle")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","laser","disabler","esword","gun","stunprod","harmbaton","bomb"))
			if("Sound")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","airlock","airlock pry","console","explosion","far explosion","mech","glass","alarm","beepsky","mech","wall decon","door hack"))
			if("Weird Sound")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","phone","hallelujah","highlander","laughter","hyperspace","game over","creepy","tesla"))
			if("Station Message")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","ratvar","shuttle dock","blob alert","malf ai","meteors","supermatter"))
			if("Health")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","critical","dead","healthy"))
			if("Alert")
				extra_settings[NES_HALLUCINATION_DETAIL] = new /datum/nanite_extra_setting/type("random", list("random","not_enough_oxy","not_enough_tox","not_enough_co2","too_much_oxy","too_much_co2","too_much_tox","newlaw","nutrition","charge","gravity","fire","locked","hacked","temphot","tempcold","pressure"))
			else
				extra_settings.Remove(NES_HALLUCINATION_DETAIL)

/datum/nanite_program/good_mood
	name = "Happiness Enhancer"
	desc = "The nanites synthesize serotonin inside the host's brain, creating an artificial sense of happiness."
	use_rate = 0.1
	rogue_types = list(/datum/nanite_program/brain_decay)

/datum/nanite_program/good_mood/register_extra_settings()
	. = ..()
	extra_settings[NES_MOOD_MESSAGE] = new /datum/nanite_extra_setting/text("HAPPINESS ENHANCEMENT")

/datum/nanite_program/good_mood/enable_passive_effect()
	. = ..()
	SEND_SIGNAL(host_mob, COMSIG_ADD_MOOD_EVENT, "nanite_happy", /datum/mood_event/nanite_happiness, get_extra_setting_value(NES_MOOD_MESSAGE))

/datum/nanite_program/good_mood/disable_passive_effect()
	. = ..()
	SEND_SIGNAL(host_mob, COMSIG_CLEAR_MOOD_EVENT, "nanite_happy")

/datum/nanite_program/bad_mood
	name = "Happiness Suppressor"
	desc = "The nanites suppress the production of serotonin inside the host's brain, creating an artificial state of depression."
	use_rate = 0.1
	rogue_types = list(/datum/nanite_program/brain_decay)

/datum/nanite_program/bad_mood/register_extra_settings()
	. = ..()
	extra_settings[NES_MOOD_MESSAGE] = new /datum/nanite_extra_setting/text("HAPPINESS SUPPRESSION")

/datum/nanite_program/bad_mood/enable_passive_effect()
	. = ..()
	SEND_SIGNAL(host_mob, COMSIG_ADD_MOOD_EVENT, "nanite_sadness", /datum/mood_event/nanite_sadness, get_extra_setting_value(NES_MOOD_MESSAGE))

/datum/nanite_program/bad_mood/disable_passive_effect()
	. = ..()
	SEND_SIGNAL(host_mob, COMSIG_CLEAR_MOOD_EVENT, "nanite_sadness")
