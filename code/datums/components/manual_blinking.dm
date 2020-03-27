/datum/component/manual_blinking
	dupe_mode = COMPONENT_DUPE_UNIQUE

	var/obj/item/organ/eyes/E
	var/warn_grace = FALSE
	var/warn_dying = FALSE
	var/last_blink
	var/check_every = 20 SECONDS
	var/grace_period = 6 SECONDS
	var/damage_rate = 1 // organ damage taken per tick
	var/list/valid_emotes = list(/datum/emote/living/carbon/blink, /datum/emote/living/carbon/blink_r)

/datum/component/manual_blinking/Initialize()
	if(!iscarbon(parent))
		return COMPONENT_INCOMPATIBLE

	var/mob/living/carbon/C = parent
	E = C.getorganslot(ORGAN_SLOT_EYES)

	if(E)
		START_PROCESSING(SSdcs, src)
		last_blink = world.time
		to_chat(C, "<span class='notice'>You suddenly realize you're blinking manually.</span>")

/datum/component/manual_blinking/Destroy(force, silent)
	E = null
	STOP_PROCESSING(SSdcs, src)
	to_chat(parent, "<span class='notice'>You revert back to automatic blinking.</span>")
	return ..()

/datum/component/manual_blinking/RegisterWithParent()
	RegisterSignal(parent, COMSIG_MOB_EMOTE, .proc/check_emote)
	RegisterSignal(parent, COMSIG_CARBON_GAIN_ORGAN, .proc/check_added_organ)
	RegisterSignal(parent, COMSIG_CARBON_LOSE_ORGAN, .proc/check_removed_organ)
	RegisterSignal(parent, COMSIG_LIVING_REVIVE, .proc/restart)
	RegisterSignal(parent, COMSIG_MOB_DEATH, .proc/pause)

/datum/component/manual_blinking/UnregisterFromParent()
	UnregisterSignal(parent, COMSIG_MOB_EMOTE)
	UnregisterSignal(parent, COMSIG_CARBON_GAIN_ORGAN)
	UnregisterSignal(parent, COMSIG_CARBON_LOSE_ORGAN)
	UnregisterSignal(parent, COMSIG_LIVING_REVIVE)
	UnregisterSignal(parent, COMSIG_MOB_DEATH)

/datum/component/manual_blinking/proc/restart()
	START_PROCESSING(SSdcs, src)

/datum/component/manual_blinking/proc/pause()
	STOP_PROCESSING(SSdcs, src)

/datum/component/manual_blinking/process()
	var/mob/living/carbon/C = parent

	if(world.time > (last_blink + check_every + grace_period))
		if(!warn_dying)
			to_chat(C, "<span class='userdanger'>Your eyes begin to wither, you need to blink!</span>")
			warn_dying = TRUE

		E.applyOrganDamage(damage_rate)
	else if(world.time > (last_blink + check_every))
		if(!warn_grace)
			to_chat(C, "<span class='danger'>You feel a need to blink!</span>")
			warn_grace = TRUE

/datum/component/manual_blinking/proc/check_added_organ(mob/who_cares, obj/item/organ/O)
	var/obj/item/organ/eyes/new_eyes = O

	if(istype(new_eyes,/obj/item/organ/eyes))
		E = new_eyes
		START_PROCESSING(SSdcs, src)

/datum/component/manual_blinking/proc/check_removed_organ(mob/who_cares, obj/item/organ/O)
	var/obj/item/organ/eyes/bye_beyes = O // oh come on, that's pretty good

	if(istype(bye_beyes, /obj/item/organ/eyes))
		E = null
		STOP_PROCESSING(SSdcs, src)

/datum/component/manual_blinking/proc/check_emote(mob/living/carbon/user, datum/emote/emote)
	if(emote.type in valid_emotes)
		warn_grace = FALSE
		warn_dying = FALSE
		last_blink = world.time
