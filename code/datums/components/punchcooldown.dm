///Your favourite Jojoke. Used for the gloves of the north star.
/datum/component/wearertargeting/punchcooldown
	signals = list(COMSIG_HUMAN_MELEE_UNARMED_ATTACK)
	mobtype = /mob/living/carbon
	proctype = .proc/reducecooldown
	valid_slots = list(ITEM_SLOT_GLOVES)
	///The warcry this generates
	var/warcry = "AT"

/datum/component/wearertargeting/punchcooldown/Initialize()
	. = ..()
	if(. == COMPONENT_INCOMPATIBLE)
		return
	RegisterSignal(parent, COMSIG_ITEM_ATTACK_SELF, .proc/changewarcry)

///Called on COMSIG_HUMAN_MELEE_UNARMED_ATTACK. Yells the warcry and and reduces punch cooldown.
/datum/component/wearertargeting/punchcooldown/proc/reducecooldown(mob/living/carbon/M, atom/target)
	if(M.a_intent == INTENT_HARM && isliving(target))
		M.changeNext_move(CLICK_CD_RAPID)
		if(warcry)
			M.say(warcry, ignore_spam = TRUE, forced = "north star warcry")

///Called on COMSIG_ITEM_ATTACK_SELF. Allows you to change the warcry.
/datum/component/wearertargeting/punchcooldown/proc/changewarcry(datum/source, mob/user)
	var/input = stripped_input(user,"What do you want your battlecry to be? Max length of 6 characters.", ,"", 7)
	if(!QDELETED(src) && !QDELETED(user) && !user.Adjacent(parent))
		return
	if(input)
		warcry = input
