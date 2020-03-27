#define DEVIL_HANDS_LAYER 1
#define DEVIL_HEAD_LAYER 2
#define DEVIL_TOTAL_LAYERS 2


/mob/living/carbon/true_devil
	name = "True Devil"
	desc = "A pile of infernal energy, taking a vaguely humanoid form."
	icon = 'icons/mob/32x64.dmi'
	icon_state = "true_devil"
	gender = NEUTER
	health = 350
	maxHealth = 350
	ventcrawler = VENTCRAWLER_NONE
	density = TRUE
	pass_flags =  0
	sight = (SEE_TURFS | SEE_OBJS)
	status_flags = CANPUSH
	spacewalk = TRUE
	mob_size = MOB_SIZE_LARGE
	held_items = list(null, null)
	bodyparts = list(/obj/item/bodypart/chest/devil, /obj/item/bodypart/head/devil, /obj/item/bodypart/l_arm/devil,
					 /obj/item/bodypart/r_arm/devil, /obj/item/bodypart/r_leg/devil, /obj/item/bodypart/l_leg/devil)
	hud_type = /datum/hud/devil
	var/ascended = FALSE
	var/mob/living/oldform
	var/list/devil_overlays[DEVIL_TOTAL_LAYERS]

/mob/living/carbon/true_devil/Initialize()
	create_bodyparts() //initialize bodyparts
	create_internal_organs()
	grant_all_languages()
	..()

/mob/living/carbon/true_devil/create_internal_organs()
	internal_organs += new /obj/item/organ/brain
	internal_organs += new /obj/item/organ/tongue
	internal_organs += new /obj/item/organ/eyes
	internal_organs += new /obj/item/organ/ears/invincible //Prevents hearing loss from poorly aimed fireballs.
	..()

/mob/living/carbon/true_devil/proc/convert_to_archdevil()
	maxHealth = 500 // not an IMPOSSIBLE amount, but still near impossible.
	ascended = TRUE
	health = maxHealth
	icon_state = "arch_devil"

/mob/living/carbon/true_devil/proc/set_name()
	var/datum/antagonist/devil/devilinfo = mind.has_antag_datum(/datum/antagonist/devil)
	name = devilinfo.truename
	real_name = name

/mob/living/carbon/true_devil/Login()
	..()
	var/datum/antagonist/devil/devilinfo = mind.has_antag_datum(/datum/antagonist/devil)
	devilinfo.greet()
	mind.announce_objectives()

/mob/living/carbon/true_devil/death(gibbed)
	set_stat(DEAD)
	..(gibbed)
	drop_all_held_items()
	INVOKE_ASYNC(mind.has_antag_datum(/datum/antagonist/devil), /datum/antagonist/devil/proc/beginResurrectionCheck, src)


/mob/living/carbon/true_devil/examine(mob/user)
	. = list("<span class='info'>*---------*\nThis is [icon2html(src, user)] <b>[src]</b>!")

	//Left hand items
	for(var/obj/item/I in held_items)
		if(!(I.item_flags & ABSTRACT))
			. += "It is holding [I.get_examine_string(user)] in its [get_held_index_name(get_held_index_of_item(I))]."

	//Braindead
	if(!client && stat != DEAD)
		. += "The devil seems to be in deep contemplation."

	//Damaged
	if(stat == DEAD)
		. += "<span class='deadsay'>The hellfire seems to have been extinguished, for now at least.</span>"
	else if(health < (maxHealth/10))
		. += "<span class='warning'>You can see hellfire inside its gaping wounds.</span>"
	else if(health < (maxHealth/2))
		. += "<span class='warning'>You can see hellfire inside its wounds.</span>"
	. += "*---------*</span>"

/mob/living/carbon/true_devil/IsAdvancedToolUser()
	return 1

/mob/living/carbon/true_devil/resist_buckle()
	if(buckled)
		buckled.user_unbuckle_mob(src,src)
		visible_message("<span class='warning'>[src] easily breaks out of [p_their()] handcuffs!</span>", \
					"<span class='notice'>With just a thought your handcuffs fall off.</span>")

/mob/living/carbon/true_devil/canUseTopic(atom/movable/M, be_close=FALSE, no_dexterity=FALSE, no_tk=FALSE)
	if(incapacitated())
		to_chat(src, "<span class='warning'>You can't do that right now!</span>")
		return FALSE
	if(be_close && !in_range(M, src))
		to_chat(src, "<span class='warning'>You are too far away!</span>")
		return FALSE
	return TRUE

/mob/living/carbon/true_devil/assess_threat(judgement_criteria, lasercolor = "", datum/callback/weaponcheck=null)
	return 666

/mob/living/carbon/true_devil/flash_act(intensity = 1, override_blindness_check = 0, affect_silicon = 0, visual = 0)
	if(mind && has_bane(BANE_LIGHT))
		mind.disrupt_spells(-500)
		return ..() //flashes don't stop devils UNLESS it's their bane.

/mob/living/carbon/true_devil/soundbang_act()
	return 0

/mob/living/carbon/true_devil/get_ear_protection()
	return 2


/mob/living/carbon/true_devil/attacked_by(obj/item/I, mob/living/user, def_zone)
	var/weakness = check_weakness(I, user)
	apply_damage(I.force * weakness, I.damtype, def_zone)
	var/message_verb = ""
	if(I.attack_verb && I.attack_verb.len)
		message_verb = "[pick(I.attack_verb)]"
	else if(I.force)
		message_verb = "attacked"

	var/attack_message = "[src] has been [message_verb] with [I]."
	if(user)
		user.do_attack_animation(src)
		if(user in viewers(src, null))
			attack_message = "[user] has [message_verb] [src] with [I]!"
	if(message_verb)
		visible_message("<span class='danger'>[attack_message]</span>",
		"<span class='userdanger'>[attack_message]</span>", null, COMBAT_MESSAGE_RANGE)
	return TRUE

/mob/living/carbon/true_devil/singularity_act()
	if(ascended)
		return 0
	return ..()

//ATTACK GHOST IGNORING PARENT RETURN VALUE
/mob/living/carbon/true_devil/attack_ghost(mob/dead/observer/user as mob)
	if(ascended || user.mind.soulOwner == src.mind)
		var/mob/living/simple_animal/imp/S = new(get_turf(loc))
		S.key = user.key
		var/datum/antagonist/imp/A = new()
		S.mind.add_antag_datum(A)
		to_chat(S, S.playstyle_string)
	else
		return ..()

/mob/living/carbon/true_devil/can_be_revived()
	return 1

/mob/living/carbon/true_devil/resist_fire()
	//They're immune to fire.

/mob/living/carbon/true_devil/attack_hand(mob/living/carbon/human/M)
	. = ..()
	if(.)
		switch(M.a_intent)
			if ("harm")
				var/damage = rand(1, 5)
				playsound(loc, "punch", 25, TRUE, -1)
				visible_message("<span class='danger'>[M] punches [src]!</span>", \
						"<span class='userdanger'>[M] punches you!</span>")
				adjustBruteLoss(damage)
				log_combat(M, src, "attacked")
				updatehealth()
			if ("disarm")
				if (!(mobility_flags & MOBILITY_STAND) && !ascended) //No stealing the arch devil's pitchfork.
					if (prob(5))
						Unconscious(40)
						playsound(loc, 'sound/weapons/thudswoosh.ogg', 50, TRUE, -1)
						log_combat(M, src, "pushed")
						visible_message("<span class='danger'>[M] pushes [src] down!</span>", \
							"<span class='userdanger'>[M] pushes you down!</span>")
					else
						if (prob(25))
							dropItemToGround(get_active_held_item())
							playsound(loc, 'sound/weapons/thudswoosh.ogg', 50, TRUE, -1)
							visible_message("<span class='danger'>[M] disarms [src]!</span>", \
							"<span class='userdanger'>[M] disarms you!</span>")
						else
							playsound(loc, 'sound/weapons/punchmiss.ogg', 25, TRUE, -1)
							visible_message("<span class='danger'>[M] fails to disarm [src]!</span>", \
							"<span class='userdanger'>[M] fails to disarm you!</span>")

/mob/living/carbon/true_devil/handle_breathing()
	// devils do not need to breathe

/mob/living/carbon/true_devil/is_literate()
	return TRUE

/mob/living/carbon/true_devil/ex_act(severity, ex_target)
	if(!ascended)
		var/b_loss
		switch (severity)
			if (EXPLODE_DEVASTATE)
				b_loss = 500
			if (EXPLODE_HEAVY)
				b_loss = 150
			if (EXPLODE_LIGHT)
				b_loss = 30
		if(has_bane(BANE_LIGHT))
			b_loss *=2
		adjustBruteLoss(b_loss)
	return ..()


/mob/living/carbon/true_devil/update_body() //we don't use the bodyparts layer for devils.
	return

/mob/living/carbon/true_devil/update_body_parts()
	return

/mob/living/carbon/true_devil/update_damage_overlays() //devils don't have damage overlays.
	return
