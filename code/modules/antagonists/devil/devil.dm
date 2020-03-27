#define BLOOD_THRESHOLD 3 //How many souls are needed per stage.
#define TRUE_THRESHOLD 7
#define ARCH_THRESHOLD 12

#define BASIC_DEVIL 0
#define BLOOD_LIZARD 1
#define TRUE_DEVIL 2
#define ARCH_DEVIL 3

#define LOSS_PER_DEATH 2

#define SOULVALUE soulsOwned.len-reviveNumber

#define DEVILRESURRECTTIME 600

GLOBAL_LIST_EMPTY(allDevils)
GLOBAL_LIST_INIT(lawlorify, list (
		LORE = list(
			OBLIGATION_FOOD = "This devil seems to always offer its victims food before slaughtering them.",
			OBLIGATION_FIDDLE = "This devil will never turn down a musical challenge.",
			OBLIGATION_DANCEOFF = "This devil will never turn down a dance off.",
			OBLIGATION_GREET = "This devil seems to only be able to converse with people it knows the name of.",
			OBLIGATION_PRESENCEKNOWN = "This devil seems to be unable to attack from stealth.",
			OBLIGATION_SAYNAME = "He will always chant his name upon killing someone.",
			OBLIGATION_ANNOUNCEKILL = "This devil always loudly announces his kills for the world to hear.",
			OBLIGATION_ANSWERTONAME = "This devil always responds to his truename.",
			BANE_SILVER = "Silver seems to gravely injure this devil.",
			BANE_SALT = "Throwing salt at this devil will hinder his ability to use infernal powers temporarily.",
			BANE_LIGHT = "Bright flashes will disorient the devil, likely causing him to flee.",
			BANE_IRON = "Cold iron will slowly injure him, until he can purge it from his system.",
			BANE_WHITECLOTHES = "Wearing clean white clothing will help ward off this devil.",
			BANE_HARVEST = "Presenting the labors of a harvest will disrupt the devil.",
			BANE_TOOLBOX = "That which holds the means of creation also holds the means of the devil's undoing.",
			BAN_HURTWOMAN = "This devil seems to prefer hunting men.",
			BAN_CHAPEL = "This devil avoids holy ground.",
			BAN_HURTPRIEST = "The annointed clergy appear to be immune to his powers.",
			BAN_AVOIDWATER = "The devil seems to have some sort of aversion to water, though it does not appear to harm him.",
			BAN_STRIKEUNCONSCIOUS = "This devil only shows interest in those who are awake.",
			BAN_HURTLIZARD = "This devil will not strike a lizardman first.",
			BAN_HURTANIMAL = "This devil avoids hurting animals.",
			BANISH_WATER = "To banish the devil, you must infuse its body with holy water.",
			BANISH_COFFIN = "This devil will return to life if its remains are not placed within a coffin.",
			BANISH_FORMALDYHIDE = "To banish the devil, you must inject its lifeless body with embalming fluid.",
			BANISH_RUNES = "This devil will resurrect after death, unless its remains are within a rune.",
			BANISH_CANDLES = "A large number of nearby lit candles will prevent it from resurrecting.",
			BANISH_DESTRUCTION = "Its corpse must be utterly destroyed to prevent resurrection.",
			BANISH_FUNERAL_GARB = "If clad in funeral garments, this devil will be unable to resurrect.  Should the clothes not fit, lay them gently on top of the devil's corpse."
		),
		LAW = list(
			OBLIGATION_FOOD = "When not acting in self defense, you must always offer your victim food before harming them.",
			OBLIGATION_FIDDLE = "When not in immediate danger, if you are challenged to a musical duel, you must accept it.  You are not obligated to duel the same person twice.",
			OBLIGATION_DANCEOFF = "When not in immediate danger, if you are challenged to a dance off, you must accept it. You are not obligated to face off with the same person twice.",
			OBLIGATION_GREET = "You must always greet other people by their last name before talking with them.",
			OBLIGATION_PRESENCEKNOWN = "You must always make your presence known before attacking.",
			OBLIGATION_SAYNAME = "You must always say your true name after you kill someone.",
			OBLIGATION_ANNOUNCEKILL = "Upon killing someone, you must make your deed known to all within earshot, over comms if reasonably possible.",
			OBLIGATION_ANSWERTONAME = "If you are not under attack, you must always respond to your true name.",
			BAN_HURTWOMAN = "You must never harm a female outside of self defense.",
			BAN_CHAPEL = "You must never attempt to enter the chapel.",
			BAN_HURTPRIEST = "You must never attack a priest.",
			BAN_AVOIDWATER = "You must never willingly touch a wet surface.",
			BAN_STRIKEUNCONSCIOUS = "You must never strike an unconscious person.",
			BAN_HURTLIZARD = "You must never harm a lizardman outside of self defense.",
			BAN_HURTANIMAL = "You must never harm a non-sentient creature or robot outside of self defense.",
			BANE_SILVER = "Silver, in all of its forms shall be your downfall.",
			BANE_SALT = "Salt will disrupt your magical abilities.",
			BANE_LIGHT = "Blinding lights will prevent you from using offensive powers for a time.",
			BANE_IRON = "Cold wrought iron shall act as poison to you.",
			BANE_WHITECLOTHES = "Those clad in pristine white garments will strike you true.",
			BANE_HARVEST = "The fruits of the harvest shall be your downfall.",
			BANE_TOOLBOX = "Toolboxes are bad news for you, for some reason.",
			BANISH_WATER = "If your corpse is filled with holy water, you will be unable to resurrect.",
			BANISH_COFFIN = "If your corpse is in a coffin, you will be unable to resurrect.",
			BANISH_FORMALDYHIDE = "If your corpse is embalmed, you will be unable to resurrect.",
			BANISH_RUNES = "If your corpse is placed within a rune, you will be unable to resurrect.",
			BANISH_CANDLES = "If your corpse is near lit candles, you will be unable to resurrect.",
			BANISH_DESTRUCTION = "If your corpse is destroyed, you will be unable to resurrect.",
			BANISH_FUNERAL_GARB = "If your corpse is clad in funeral garments, you will be unable to resurrect."
		)
	))

//These are also used in the codex gigas, so let's declare them globally.
GLOBAL_LIST_INIT(devil_pre_title, list("Dark ", "Hellish ", "Fallen ", "Fiery ", "Sinful ", "Blood ", "Fluffy "))
GLOBAL_LIST_INIT(devil_title, list("Lord ", "Prelate ", "Count ", "Viscount ", "Vizier ", "Elder ", "Adept "))
GLOBAL_LIST_INIT(devil_syllable, list("hal", "ve", "odr", "neit", "ci", "quon", "mya", "folth", "wren", "geyr", "hil", "niet", "twou", "phi", "coa"))
GLOBAL_LIST_INIT(devil_suffix, list(" the Red", " the Soulless", " the Master", ", the Lord of all things", ", Jr."))
/datum/antagonist/devil
	name = "Devil"
	roundend_category = "devils"
	antagpanel_category = "Devil"
	job_rank = ROLE_DEVIL
	antag_hud_type = ANTAG_HUD_DEVIL
	antag_hud_name = "devil"
	var/obligation
	var/ban
	var/bane
	var/banish
	var/truename
	var/list/datum/mind/soulsOwned = new
	var/reviveNumber = 0
	var/form = BASIC_DEVIL
	var/static/list/devil_spells = typecacheof(list(
		/obj/effect/proc_holder/spell/aimed/fireball/hellish,
		/obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork,
		/obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork/greater,
		/obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork/ascended,
		/obj/effect/proc_holder/spell/targeted/infernal_jaunt,
		/obj/effect/proc_holder/spell/targeted/sintouch,
		/obj/effect/proc_holder/spell/targeted/sintouch/ascended,
		/obj/effect/proc_holder/spell/targeted/summon_contract,
		/obj/effect/proc_holder/spell/targeted/conjure_item/violin,
		/obj/effect/proc_holder/spell/targeted/summon_dancefloor))
	var/ascendable = FALSE

/datum/antagonist/devil/can_be_owned(datum/mind/new_owner)
	. = ..()
	return . && (ishuman(new_owner.current) || iscyborg(new_owner.current))

/datum/antagonist/devil/get_admin_commands()
	. = ..()
	.["Toggle ascendable"] = CALLBACK(src,.proc/admin_toggle_ascendable)


/datum/antagonist/devil/proc/admin_toggle_ascendable(mob/admin)
	ascendable = !ascendable
	message_admins("[key_name_admin(admin)] set [key_name_admin(owner)] devil ascendable to [ascendable]")
	log_admin("[key_name_admin(admin)] set [key_name(owner)] devil ascendable to [ascendable])")

/datum/antagonist/devil/admin_add(datum/mind/new_owner,mob/admin)
	switch(alert(admin,"Should the devil be able to ascend",,"Yes","No","Cancel"))
		if("Yes")
			ascendable = TRUE
		if("No")
			ascendable = FALSE
		else
			return
	new_owner.add_antag_datum(src)
	message_admins("[key_name_admin(admin)] has devil'ed [key_name_admin(new_owner)]. [ascendable ? "(Ascendable)":""]")
	log_admin("[key_name(admin)] has devil'ed [key_name(new_owner)]. [ascendable ? "(Ascendable)":""]")

/datum/antagonist/devil/antag_listing_name()
	return ..() + "([truename])"

/proc/devilInfo(name)
	if(GLOB.allDevils[lowertext(name)])
		return GLOB.allDevils[lowertext(name)]
	else
		var/datum/fakeDevil/devil = new /datum/fakeDevil(name)
		GLOB.allDevils[lowertext(name)] = devil
		return devil

/proc/randomDevilName()
	var/name = ""
	if(prob(65))
		if(prob(35))
			name = pick(GLOB.devil_pre_title)
		name += pick(GLOB.devil_title)
	var/probability = 100
	name += pick(GLOB.devil_syllable)
	while(prob(probability))
		name += pick(GLOB.devil_syllable)
		probability -= 20
	if(prob(40))
		name += pick(GLOB.devil_suffix)
	return name

/proc/randomdevilobligation()
	return pick(OBLIGATION_FOOD, OBLIGATION_FIDDLE, OBLIGATION_DANCEOFF, OBLIGATION_GREET, OBLIGATION_PRESENCEKNOWN, OBLIGATION_SAYNAME, OBLIGATION_ANNOUNCEKILL, OBLIGATION_ANSWERTONAME)

/proc/randomdevilban()
	return pick(BAN_HURTWOMAN, BAN_CHAPEL, BAN_HURTPRIEST, BAN_AVOIDWATER, BAN_STRIKEUNCONSCIOUS, BAN_HURTLIZARD, BAN_HURTANIMAL)

/proc/randomdevilbane()
	return pick(BANE_SALT, BANE_LIGHT, BANE_IRON, BANE_WHITECLOTHES, BANE_SILVER, BANE_HARVEST, BANE_TOOLBOX)

/proc/randomdevilbanish()
	return pick(BANISH_WATER, BANISH_COFFIN, BANISH_FORMALDYHIDE, BANISH_RUNES, BANISH_CANDLES, BANISH_DESTRUCTION, BANISH_FUNERAL_GARB)

/datum/antagonist/devil/proc/add_soul(datum/mind/soul)
	if(soulsOwned.Find(soul))
		return
	soulsOwned += soul
	owner.current.set_nutrition(NUTRITION_LEVEL_FULL)
	to_chat(owner.current, "<span class='warning'>You feel satiated as you received a new soul.</span>")
	update_hud()
	switch(SOULVALUE)
		if(0)
			to_chat(owner.current, "<span class='warning'>Your hellish powers have been restored.</span>")
			give_appropriate_spells()
		if(BLOOD_THRESHOLD)
			increase_blood_lizard()
		if(TRUE_THRESHOLD)
			increase_true_devil()
		if(ARCH_THRESHOLD)
			increase_arch_devil()

/datum/antagonist/devil/proc/remove_soul(datum/mind/soul)
	if(soulsOwned.Remove(soul))
		check_regression()
		to_chat(owner.current, "<span class='warning'>You feel as though a soul has slipped from your grasp.</span>")
		update_hud()

/datum/antagonist/devil/proc/check_regression()
	if(form == ARCH_DEVIL)
		return //arch devil can't regress
	//Yes, fallthrough behavior is intended, so I can't use a switch statement.
	if(form == TRUE_DEVIL && SOULVALUE < TRUE_THRESHOLD)
		regress_blood_lizard()
	if(form == BLOOD_LIZARD && SOULVALUE < BLOOD_THRESHOLD)
		regress_humanoid()
	if(SOULVALUE < 0)
		give_appropriate_spells()
		to_chat(owner.current, "<span class='warning'>As punishment for your failures, all of your powers except contract creation have been revoked.</span>")

/datum/antagonist/devil/proc/regress_humanoid()
	to_chat(owner.current, "<span class='warning'>Your powers weaken, have more contracts be signed to regain power.</span>")
	if(ishuman(owner.current))
		var/mob/living/carbon/human/H = owner.current
		H.set_species(/datum/species/human, 1)
		H.regenerate_icons()
	give_appropriate_spells()
	if(istype(owner.current.loc, /obj/effect/dummy/phased_mob/slaughter/))
		owner.current.forceMove(get_turf(owner.current))//Fixes dying while jaunted leaving you permajaunted.
	form = BASIC_DEVIL

/datum/antagonist/devil/proc/regress_blood_lizard()
	var/mob/living/carbon/true_devil/D = owner.current
	to_chat(D, "<span class='warning'>Your powers weaken, have more contracts be signed to regain power.</span>")
	D.oldform.forceMove(D.drop_location())
	owner.transfer_to(D.oldform)
	give_appropriate_spells()
	qdel(D)
	form = BLOOD_LIZARD
	update_hud()


/datum/antagonist/devil/proc/increase_blood_lizard()
	to_chat(owner.current, "<span class='warning'>You feel as though your humanoid form is about to shed. You will soon turn into a blood lizard.</span>")
	sleep(50)
	if(ishuman(owner.current))
		var/mob/living/carbon/human/H = owner.current
		H.set_species(/datum/species/lizard, 1)
		H.underwear = "Nude"
		H.undershirt = "Nude"
		H.socks = "Nude"
		H.dna.features["mcolor"] = "511" //A deep red
		H.regenerate_icons()
	else //Did the devil get hit by a staff of transmutation?
		owner.current.color = "#501010"
	give_appropriate_spells()
	form = BLOOD_LIZARD



/datum/antagonist/devil/proc/increase_true_devil()
	to_chat(owner.current, "<span class='warning'>You feel as though your current form is about to shed. You will soon turn into a true devil.</span>")
	sleep(50)
	var/mob/living/carbon/true_devil/A = new /mob/living/carbon/true_devil(owner.current.loc)
	A.faction |= "hell"
	owner.current.forceMove(A)
	A.oldform = owner.current
	owner.transfer_to(A)
	A.set_name()
	give_appropriate_spells()
	form = TRUE_DEVIL
	update_hud()

/datum/antagonist/devil/proc/increase_arch_devil()
	if(!ascendable)
		return
	var/mob/living/carbon/true_devil/D = owner.current
	to_chat(D, "<span class='warning'>You feel as though your form is about to ascend.</span>")
	sleep(50)
	if(!D)
		return
	D.visible_message("<span class='warning'>[D]'s skin begins to erupt with spikes.</span>", \
		"<span class='warning'>Your flesh begins creating a shield around yourself.</span>")
	sleep(100)
	if(!D)
		return
	D.visible_message("<span class='warning'>The horns on [D]'s head slowly grow and elongate.</span>", \
		"<span class='warning'>Your body continues to mutate. Your telepathic abilities grow.</span>")
	sleep(90)
	if(!D)
		return
	D.visible_message("<span class='warning'>[D]'s body begins to violently stretch and contort.</span>", \
		"<span class='warning'>You begin to rend apart the final barriers to ultimate power.</span>")
	sleep(40)
	if(!D)
		return
	to_chat(D, "<i><b>Yes!</b></i>")
	sleep(10)
	if(!D)
		return
	to_chat(D, "<i><b><span class='big'>YES!!</span></b></i>")
	sleep(10)
	if(!D)
		return
	to_chat(D, "<i><b><span class='reallybig'>YE--</span></b></i>")
	sleep(1)
	if(!D)
		return
	send_to_playing_players("<font size=5><span class='danger'><b>\"SLOTH, WRATH, GLUTTONY, ACEDIA, ENVY, GREED, PRIDE! FIRES OF HELL AWAKEN!!\"</font></span>")
	sound_to_playing_players('sound/hallucinations/veryfar_noise.ogg')
	give_appropriate_spells()
	D.convert_to_archdevil()
	if(istype(D.loc, /obj/effect/dummy/phased_mob/slaughter/))
		D.forceMove(get_turf(D))//Fixes dying while jaunted leaving you permajaunted.
	var/area/A = get_area(owner.current)
	if(A)
		notify_ghosts("An arch devil has ascended in \the [A.name]. Reach out to the devil to be given a new shell for your soul.", source = owner.current, action=NOTIFY_ATTACK)
	sleep(50)
	if(!SSticker.mode.devil_ascended)
		SSshuttle.emergency.request(null, set_coefficient = 0.3)
	SSticker.mode.devil_ascended++
	form = ARCH_DEVIL

/datum/antagonist/devil/proc/remove_spells()
	for(var/X in owner.spell_list)
		var/obj/effect/proc_holder/spell/S = X
		if(is_type_in_typecache(S, devil_spells))
			owner.RemoveSpell(S)

/datum/antagonist/devil/proc/give_summon_contract()
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/summon_contract(null))
	if(obligation == OBLIGATION_FIDDLE)
		owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/conjure_item/violin(null))
	else if(obligation == OBLIGATION_DANCEOFF)
		owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/summon_dancefloor(null))

/datum/antagonist/devil/proc/give_appropriate_spells()
	remove_spells()
	give_summon_contract()
	if(SOULVALUE >= ARCH_THRESHOLD && ascendable)
		give_arch_spells()
	else if(SOULVALUE >= TRUE_THRESHOLD)
		give_true_spells()
	else if(SOULVALUE >= BLOOD_THRESHOLD)
		give_blood_spells()
	else if(SOULVALUE >= 0)
		give_base_spells()

/datum/antagonist/devil/proc/give_base_spells()
	owner.AddSpell(new /obj/effect/proc_holder/spell/aimed/fireball/hellish(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork(null))

/datum/antagonist/devil/proc/give_blood_spells()
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/aimed/fireball/hellish(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/infernal_jaunt(null))

/datum/antagonist/devil/proc/give_true_spells()
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork/greater(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/aimed/fireball/hellish(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/infernal_jaunt(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/sintouch(null))

/datum/antagonist/devil/proc/give_arch_spells()
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/conjure_item/summon_pitchfork/ascended(null))
	owner.AddSpell(new /obj/effect/proc_holder/spell/targeted/sintouch/ascended(null))

/datum/antagonist/devil/proc/beginResurrectionCheck(mob/living/body)
	if(SOULVALUE>0)
		to_chat(owner.current, "<span class='userdanger'>Your body has been damaged to the point that you may no longer use it. At the cost of some of your power, you will return to life soon. Remain in your body.</span>")
		sleep(DEVILRESURRECTTIME)
		if (!body ||  body.stat == DEAD)
			if(SOULVALUE>0)
				if(check_banishment(body))
					to_chat(owner.current, "<span class='userdanger'>Unfortunately, the mortals have finished a ritual that prevents your resurrection.</span>")
					return -1
				else
					to_chat(owner.current, "<span class='userdanger'>WE LIVE AGAIN!</span>")
					return hellish_resurrection(body)
			else
				to_chat(owner.current, "<span class='userdanger'>Unfortunately, the power that stemmed from your contracts has been extinguished. You no longer have enough power to resurrect.</span>")
				return -1
		else
			to_chat(owner.current, "<span class='danger'>You seem to have resurrected without your hellish powers.</span>")
	else
		to_chat(owner.current, "<span class='userdanger'>Your hellish powers are too weak to resurrect yourself.</span>")

/datum/antagonist/devil/proc/check_banishment(mob/living/body)
	switch(banish)
		if(BANISH_WATER)
			if(iscarbon(body))
				var/mob/living/carbon/H = body
				return H.reagents.has_reagent(/datum/reagent/water/holywater)
			return 0
		if(BANISH_COFFIN)
			return (body && istype(body.loc, /obj/structure/closet/crate/coffin))
		if(BANISH_FORMALDYHIDE)
			if(iscarbon(body))
				var/mob/living/carbon/H = body
				return H.reagents.has_reagent(/datum/reagent/toxin/formaldehyde)
			return 0
		if(BANISH_RUNES)
			if(body)
				for(var/obj/effect/decal/cleanable/crayon/R in range(0,body))
					if (R.name == "rune")
						return 1
			return 0
		if(BANISH_CANDLES)
			if(body)
				var/count = 0
				for(var/obj/item/candle/C in range(1,body))
					count += C.lit
				if(count>=4)
					return 1
			return 0
		if(BANISH_DESTRUCTION)
			if(body)
				return 0
			return 1
		if(BANISH_FUNERAL_GARB)
			if(ishuman(body))
				var/mob/living/carbon/human/H = body
				if(H.w_uniform && istype(H.w_uniform, /obj/item/clothing/under/misc/burial))
					return 1
				return 0
			else
				for(var/obj/item/clothing/under/misc/burial/B in range(0,body))
					if(B.loc == get_turf(B)) //Make sure it's not in someone's inventory or something.
						return 1
				return 0

/datum/antagonist/devil/proc/hellish_resurrection(mob/living/body)
	message_admins("[key_name_admin(owner)] (true name is: [truename]) is resurrecting using hellish energy.</a>")
	if(SOULVALUE < ARCH_THRESHOLD || !ascendable) // once ascended, arch devils do not go down in power by any means.
		reviveNumber += LOSS_PER_DEATH
		update_hud()
	if(body)
		body.revive(full_heal = TRUE, admin_revive = TRUE) //Adminrevive also recovers organs, preventing someone from resurrecting without a heart.
		if(istype(body.loc, /obj/effect/dummy/phased_mob/slaughter/))
			body.forceMove(get_turf(body))//Fixes dying while jaunted leaving you permajaunted.
		if(istype(body, /mob/living/carbon/true_devil))
			var/mob/living/carbon/true_devil/D = body
			if(D.oldform)
				D.oldform.revive(full_heal = TRUE, admin_revive = FALSE) // Heal the old body too, so the devil doesn't resurrect, then immediately regress into a dead body.
		if(body.stat == DEAD)
			create_new_body()
	else
		create_new_body()
	check_regression()

/datum/antagonist/devil/proc/create_new_body()
	if(GLOB.blobstart.len > 0)
		var/turf/targetturf = get_turf(pick(GLOB.blobstart))
		var/mob/currentMob = owner.current
		if(!currentMob)
			currentMob = owner.get_ghost()
			if(!currentMob)
				message_admins("[key_name_admin(owner)]'s devil resurrection failed due to client logoff.  Aborting.")
				return -1
		if(currentMob.mind != owner)
			message_admins("[key_name_admin(owner)]'s devil resurrection failed due to becoming a new mob.  Aborting.")
			return -1
		currentMob.change_mob_type( /mob/living/carbon/human, targetturf, null, 1)
		var/mob/living/carbon/human/H = owner.current
		H.equip_to_slot_or_del(new /obj/item/clothing/under/rank/civilian/lawyer/black(H), ITEM_SLOT_ICLOTHING)
		H.equip_to_slot_or_del(new /obj/item/clothing/shoes/laceup(H), ITEM_SLOT_FEET)
		H.equip_to_slot_or_del(new /obj/item/storage/briefcase(H), ITEM_SLOT_HANDS)
		H.equip_to_slot_or_del(new /obj/item/pen(H), ITEM_SLOT_LPOCKET)
		if(SOULVALUE >= BLOOD_THRESHOLD)
			H.set_species(/datum/species/lizard, 1)
			H.underwear = "Nude"
			H.undershirt = "Nude"
			H.socks = "Nude"
			H.dna.features["mcolor"] = "511"
			H.regenerate_icons()
			if(SOULVALUE >= TRUE_THRESHOLD) //Yes, BOTH this and the above if statement are to run if soulpower is high enough.
				var/mob/living/carbon/true_devil/A = new /mob/living/carbon/true_devil(targetturf)
				A.faction |= "hell"
				H.forceMove(A)
				A.oldform = H
				owner.transfer_to(A, TRUE)
				A.set_name()
				if(SOULVALUE >= ARCH_THRESHOLD && ascendable)
					A.convert_to_archdevil()
	else
		CRASH("Unable to find a blobstart landmark for hellish resurrection")


/datum/antagonist/devil/proc/update_hud()
	if(iscarbon(owner.current))
		var/mob/living/C = owner.current
		if(C.hud_used && C.hud_used.devilsouldisplay)
			C.hud_used.devilsouldisplay.update_counter(SOULVALUE)

/datum/antagonist/devil/greet()
	to_chat(owner.current, "<span class='warning'><b>You remember your link to the infernal. You are [truename], an agent of hell, a devil. And you were sent to the plane of creation for a reason. A greater purpose. Convince the crew to sin, and embroiden Hell's grasp.</b></span>")
	to_chat(owner.current, "<span class='warning'><b>However, your infernal form is not without weaknesses.</b></span>")
	to_chat(owner.current, "You may not use violence to coerce someone into selling their soul.")
	to_chat(owner.current, "You may not directly and knowingly physically harm a devil, other than yourself.")
	to_chat(owner.current, GLOB.lawlorify[LAW][bane])
	to_chat(owner.current, GLOB.lawlorify[LAW][ban])
	to_chat(owner.current, GLOB.lawlorify[LAW][obligation])
	to_chat(owner.current, GLOB.lawlorify[LAW][banish])
	to_chat(owner.current, "<span class='warning'>Remember, the crew can research your weaknesses if they find out your devil name.</span><br>")
	.=..()

/datum/antagonist/devil/on_gain()
	truename = randomDevilName()
	ban = randomdevilban()
	bane = randomdevilbane()
	obligation = randomdevilobligation()
	banish = randomdevilbanish()
	GLOB.allDevils[lowertext(truename)] = src

	antag_memory += "Your devilic true name is [truename]<br>[GLOB.lawlorify[LAW][ban]]<br>You may not use violence to coerce someone into selling their soul.<br>You may not directly and knowingly physically harm a devil, other than yourself.<br>[GLOB.lawlorify[LAW][bane]]<br>[GLOB.lawlorify[LAW][obligation]]<br>[GLOB.lawlorify[LAW][banish]]<br>"
	if(issilicon(owner.current))
		var/mob/living/silicon/robot_devil = owner.current
		var/laws = list("You may not use violence to coerce someone into selling their soul.", "You may not directly and knowingly physically harm a devil, other than yourself.", GLOB.lawlorify[LAW][ban], GLOB.lawlorify[LAW][obligation], "Accomplish your objectives at all costs.")
		robot_devil.set_law_sixsixsix(laws)
	sleep(10)
	.=..()

/datum/antagonist/devil/on_removal()
	to_chat(owner.current, "<span class='userdanger'>Your infernal link has been severed! You are no longer a devil!</span>")
	.=..()

/datum/antagonist/devil/apply_innate_effects(mob/living/mob_override)
	give_appropriate_spells()
	var/mob/living/M = mob_override || owner.current
	add_antag_hud(antag_hud_type, antag_hud_name, M)
	handle_clown_mutation(M, mob_override ? null : "Your infernal nature has allowed you to overcome your clownishness.")
	owner.current.grant_all_languages(TRUE, TRUE, TRUE, LANGUAGE_DEVIL)
	update_hud()
	.=..()

/datum/antagonist/devil/remove_innate_effects(mob/living/mob_override)
	for(var/X in owner.spell_list)
		var/obj/effect/proc_holder/spell/S = X
		if(is_type_in_typecache(S, devil_spells))
			owner.RemoveSpell(S)
	var/mob/living/M = mob_override || owner.current
	remove_antag_hud(antag_hud_type, M)
	handle_clown_mutation(M, removing = FALSE)
	owner.current.remove_all_languages(LANGUAGE_DEVIL)
	.=..()

/datum/antagonist/devil/proc/printdevilinfo()
	var/list/parts = list()
	parts += "The devil's true name is: [truename]"
	parts += "The devil's bans were:"
	parts += "[FOURSPACES][GLOB.lawlorify[LORE][ban]]"
	parts += "[FOURSPACES][GLOB.lawlorify[LORE][bane]]"
	parts += "[FOURSPACES][GLOB.lawlorify[LORE][obligation]]"
	parts += "[FOURSPACES][GLOB.lawlorify[LORE][banish]]"
	return parts.Join("<br>")

/datum/antagonist/devil/roundend_report()
	var/list/parts = list()
	parts += printplayer(owner)
	parts += printdevilinfo()
	parts += printobjectives(objectives)
	return parts.Join("<br>")

//A simple super light weight datum for the codex gigas.
/datum/fakeDevil
	var/truename
	var/bane
	var/obligation
	var/ban
	var/banish
	var/ascendable

/datum/fakeDevil/New(name = randomDevilName())
	truename = name
	bane = randomdevilbane()
	obligation = randomdevilobligation()
	ban = randomdevilban()
	banish = randomdevilbanish()
	ascendable = prob(25)
