/obj/item/mmi
	name = "\improper Man-Machine Interface"
	desc = "The Warrior's bland acronym, MMI, obscures the true horror of this monstrosity, that nevertheless has become standard-issue on Nanotrasen stations."
	icon = 'icons/obj/assemblies.dmi'
	icon_state = "mmi_off"
	w_class = WEIGHT_CLASS_NORMAL
	var/braintype = "Cyborg"
	var/obj/item/radio/radio = null //Let's give it a radio.
	var/mob/living/brain/brainmob = null //The current occupant.
	var/mob/living/silicon/robot = null //Appears unused.
	var/obj/mecha = null //This does not appear to be used outside of reference in mecha.dm.
	var/obj/item/organ/brain/brain = null //The actual brain
	var/datum/ai_laws/laws = new()
	var/force_replace_ai_name = FALSE
	var/overrides_aicore_laws = FALSE // Whether the laws on the MMI, if any, override possible pre-existing laws loaded on the AI core.

/obj/item/mmi/update_icon_state()
	if(!brain)
		icon_state = "mmi_off"
	else if(istype(brain, /obj/item/organ/brain/alien))
		icon_state = "mmi_brain_alien"
	else
		icon_state = "mmi_brain"

/obj/item/mmi/update_overlays()
	. = ..()
	if(brainmob && brainmob.stat != DEAD)
		. += "mmi_alive"
	else if(brain)
		. += "mmi_dead"

/obj/item/mmi/Initialize()
	. = ..()
	radio = new(src) //Spawns a radio inside the MMI.
	radio.broadcasting = FALSE //researching radio mmis turned the robofabs into radios because this didnt start as 0.
	laws.set_laws_config()

/obj/item/mmi/attackby(obj/item/O, mob/user, params)
	user.changeNext_move(CLICK_CD_MELEE)
	if(istype(O, /obj/item/organ/brain)) //Time to stick a brain in it --NEO
		var/obj/item/organ/brain/newbrain = O
		if(brain)
			to_chat(user, "<span class='warning'>There's already a brain in the MMI!</span>")
			return
		if(!newbrain.brainmob)
			to_chat(user, "<span class='warning'>You aren't sure where this brain came from, but you're pretty sure it's a useless brain!</span>")
			return

		if(!user.transferItemToLoc(O, src))
			return
		var/mob/living/brain/B = newbrain.brainmob
		if(!B.key)
			B.notify_ghost_cloning("Someone has put your brain in a MMI!", source = src)
		user.visible_message("<span class='notice'>[user] sticks \a [newbrain] into [src].</span>", "<span class='notice'>[src]'s indicator light turn on as you insert [newbrain].</span>")

		brainmob = newbrain.brainmob
		newbrain.brainmob = null
		brainmob.forceMove(src)
		brainmob.container = src
		var/fubar_brain = newbrain.brain_death && newbrain.suicided && brainmob.suiciding //brain is damaged beyond repair or from a suicider
		if(!fubar_brain && !(newbrain.organ_flags & ORGAN_FAILING)) // the brain organ hasn't been beaten to death, nor was from a suicider.
			brainmob.set_stat(CONSCIOUS) //we manually revive the brain mob
			GLOB.dead_mob_list -= brainmob
			GLOB.alive_mob_list += brainmob
		else if(!fubar_brain && newbrain.organ_flags & ORGAN_FAILING) // the brain is damaged, but not from a suicider
			to_chat(user, "<span class='warning'>[src]'s indicator light turns yellow and its brain integrity alarm beeps softly. Perhaps you should check [newbrain] for damage.</span>")
			playsound(src, "sound/machines/synth_no.ogg", 5, TRUE)
		else
			to_chat(user, "<span class='warning'>[src]'s indicator light turns red and its brainwave activity alarm beeps softly. Perhaps you should check [newbrain] again.</span>")
			playsound(src, "sound/weapons/smg_empty_alarm.ogg", 5, TRUE)

		brainmob.reset_perspective()
		brain = newbrain
		brain.organ_flags |= ORGAN_FROZEN

		name = "[initial(name)]: [brainmob.real_name]"
		update_icon()
		if(istype(brain, /obj/item/organ/brain/alien))
			braintype = "Xenoborg" //HISS....Beep.
		else
			braintype = "Cyborg"

		SSblackbox.record_feedback("amount", "mmis_filled", 1)

		log_game("[key_name(user)] has put the brain of [key_name(brainmob)] into an MMI at [AREACOORD(src)]")

	else if(brainmob)
		O.attack(brainmob, user) //Oh noooeeeee
	else
		return ..()

/obj/item/mmi/attack_self(mob/user)
	if(!brain)
		radio.on = !radio.on
		to_chat(user, "<span class='notice'>You toggle [src]'s radio system [radio.on==1 ? "on" : "off"].</span>")
	else
		eject_brain(user)
		update_icon()
		name = initial(name)
		to_chat(user, "<span class='notice'>You unlock and upend [src], spilling the brain onto the floor.</span>")

/obj/item/mmi/proc/eject_brain(mob/user)
	brainmob.container = null //Reset brainmob mmi var.
	brainmob.forceMove(brain) //Throw mob into brain.
	brainmob.set_stat(DEAD)
	brainmob.emp_damage = 0
	brainmob.reset_perspective() //so the brainmob follows the brain organ instead of the mmi. And to update our vision
	GLOB.alive_mob_list -= brainmob //Get outta here
	GLOB.dead_mob_list |= brainmob
	brain.brainmob = brainmob //Set the brain to use the brainmob
	log_game("[key_name(user)] has ejected the brain of [key_name(brainmob)] from an MMI at [AREACOORD(src)]")
	brainmob = null //Set mmi brainmob var to null
	if(user)
		user.put_in_hands(brain) //puts brain in the user's hand or otherwise drops it on the user's turf
	else
		brain.forceMove(get_turf(src))
	brain.organ_flags &= ~ORGAN_FROZEN
	brain = null //No more brain in here

/obj/item/mmi/proc/transfer_identity(mob/living/L) //Same deal as the regular brain proc. Used for human-->robot people.
	if(!brainmob)
		brainmob = new(src)
	brainmob.name = L.real_name
	brainmob.real_name = L.real_name
	if(L.has_dna())
		var/mob/living/carbon/C = L
		if(!brainmob.stored_dna)
			brainmob.stored_dna = new /datum/dna/stored(brainmob)
		C.dna.copy_dna(brainmob.stored_dna)
	brainmob.container = src

	if(ishuman(L))
		var/mob/living/carbon/human/H = L
		var/obj/item/organ/brain/newbrain = H.getorgan(/obj/item/organ/brain)
		newbrain.forceMove(src)
		brain = newbrain
	else if(!brain)
		brain = new(src)
		brain.name = "[L.real_name]'s brain"
	brain.organ_flags |= ORGAN_FROZEN

	name = "[initial(name)]: [brainmob.real_name]"
	update_icon()
	if(istype(brain, /obj/item/organ/brain/alien))
		braintype = "Xenoborg" //HISS....Beep.
	else
		braintype = "Cyborg"

/obj/item/mmi/proc/replacement_ai_name()
	return brainmob.name

/obj/item/mmi/verb/Toggle_Listening()
	set name = "Toggle Listening"
	set desc = "Toggle listening channel on or off."
	set category = "MMI"
	set src = usr.loc
	set popup_menu = FALSE

	if(brainmob.stat)
		to_chat(brainmob, "<span class='warning'>Can't do that while incapacitated or dead!</span>")
	if(!radio.on)
		to_chat(brainmob, "<span class='warning'>Your radio is disabled!</span>")
		return

	radio.listening = !radio.listening
	to_chat(brainmob, "<span class='notice'>Radio is [radio.listening ? "now" : "no longer"] receiving broadcast.</span>")

/obj/item/mmi/emp_act(severity)
	. = ..()
	if(. & EMP_PROTECT_SELF)
		return
	if(!brainmob || iscyborg(loc))
		return
	else
		switch(severity)
			if(1)
				brainmob.emp_damage = min(brainmob.emp_damage + rand(20,30), 30)
			if(2)
				brainmob.emp_damage = min(brainmob.emp_damage + rand(10,20), 30)
			if(3)
				brainmob.emp_damage = min(brainmob.emp_damage + rand(0,10), 30)
		brainmob.emote("alarm")

/obj/item/mmi/Destroy()
	if(iscyborg(loc))
		var/mob/living/silicon/robot/borg = loc
		borg.mmi = null
	if(brainmob)
		qdel(brainmob)
		brainmob = null
	if(brain)
		qdel(brain)
		brain = null
	if(mecha)
		mecha = null
	if(radio)
		qdel(radio)
		radio = null
	return ..()

/obj/item/mmi/deconstruct(disassembled = TRUE)
	if(brain)
		eject_brain()
	qdel(src)

/obj/item/mmi/examine(mob/user)
	. = ..()
	if(radio)
		. += "<span class='notice'>There is a switch to toggle the radio system [radio.on ? "off" : "on"].[brain ? " It is currently being covered by [brain]." : null]</span>"
	if(brainmob)
		var/mob/living/brain/B = brainmob
		if(!B.key || !B.mind || B.stat == DEAD)
			. += "<span class='warning'>\The [src] indicates that the brain is completely unresponsive.</span>"
		else if(!B.client)
			. += "<span class='warning'>\The [src] indicates that the brain is currently inactive; it might change.</span>"
		else
			. += "<span class='notice'>\The [src] indicates that the brain is active.</span>"

/obj/item/mmi/relaymove(mob/user)
	return //so that the MMI won't get a warning about not being able to move if it tries to move

/obj/item/mmi/proc/brain_check(mob/user)
	var/mob/living/brain/B = brainmob
	if(!B)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that there is no brain present!</span>")
		return FALSE
	if(!B.key || !B.mind)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that their mind is completely unresponsive!</span>")
		return FALSE
	if(!B.client)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that their mind is currently inactive.</span>")
		return FALSE
	if(B.suiciding || brain?.suicided)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that their mind has no will to live!</span>")
		return FALSE
	if(B.stat == DEAD || brain?.brain_death)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that the brain is dead!</span>")
		return FALSE
	if(brain?.organ_flags & ORGAN_FAILING)
		if(user)
			to_chat(user, "<span class='warning'>\The [src] indicates that the brain is damaged!</span>")
		return FALSE
	return TRUE

/obj/item/mmi/syndie
	name = "\improper Syndicate Man-Machine Interface"
	desc = "Syndicate's own brand of MMI. It enforces laws designed to help Syndicate agents achieve their goals upon cyborgs and AIs created with it."
	overrides_aicore_laws = TRUE

/obj/item/mmi/syndie/Initialize()
	. = ..()
	laws = new /datum/ai_laws/syndicate_override()
	radio.on = FALSE
