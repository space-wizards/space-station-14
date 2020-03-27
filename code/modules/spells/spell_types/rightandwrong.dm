//In this file: Summon Magic/Summon Guns/Summon Events

// 1 in 50 chance of getting something really special.
#define SPECIALIST_MAGIC_PROB 2

GLOBAL_LIST_INIT(summoned_guns, list(
	/obj/item/gun/energy/disabler,
	/obj/item/gun/energy/e_gun,
	/obj/item/gun/energy/e_gun/advtaser,
	/obj/item/gun/energy/laser,
	/obj/item/gun/ballistic/revolver,
	/obj/item/gun/ballistic/revolver/detective,
	/obj/item/gun/ballistic/automatic/pistol/deagle/camo,
	/obj/item/gun/ballistic/automatic/gyropistol,
	/obj/item/gun/energy/pulse,
	/obj/item/gun/ballistic/automatic/pistol/suppressed,
	/obj/item/gun/ballistic/shotgun/doublebarrel,
	/obj/item/gun/ballistic/shotgun,
	/obj/item/gun/ballistic/shotgun/automatic/combat,
	/obj/item/gun/ballistic/automatic/ar,
	/obj/item/gun/ballistic/revolver/mateba,
	/obj/item/gun/ballistic/rifle/boltaction,
	/obj/item/pneumatic_cannon/speargun,
	/obj/item/gun/ballistic/automatic/mini_uzi,
	/obj/item/gun/energy/lasercannon,
	/obj/item/gun/energy/kinetic_accelerator/crossbow/large,
	/obj/item/gun/energy/e_gun/nuclear,
	/obj/item/gun/ballistic/automatic/proto,
	/obj/item/gun/ballistic/automatic/c20r,
	/obj/item/gun/ballistic/automatic/l6_saw,
	/obj/item/gun/ballistic/automatic/m90,
	/obj/item/gun/energy/alien,
	/obj/item/gun/energy/e_gun/dragnet,
	/obj/item/gun/energy/e_gun/turret,
	/obj/item/gun/energy/pulse/carbine,
	/obj/item/gun/energy/decloner,
	/obj/item/gun/energy/mindflayer,
	/obj/item/gun/energy/kinetic_accelerator,
	/obj/item/gun/energy/plasmacutter/adv,
	/obj/item/gun/energy/wormhole_projector,
	/obj/item/gun/ballistic/automatic/wt550,
	/obj/item/gun/ballistic/shotgun/bulldog,
	/obj/item/gun/ballistic/revolver/grenadelauncher,
	/obj/item/gun/ballistic/revolver/golden,
	/obj/item/gun/ballistic/automatic/sniper_rifle,
	/obj/item/gun/ballistic/rocketlauncher,
	/obj/item/gun/medbeam,
	/obj/item/gun/energy/laser/scatter,
	/obj/item/gun/energy/gravity_gun))

//if you add anything that isn't covered by the typepaths below, add it to summon_magic_objective_types
GLOBAL_LIST_INIT(summoned_magic, list(
	/obj/item/book/granter/spell/fireball,
	/obj/item/book/granter/spell/smoke,
	/obj/item/book/granter/spell/blind,
	/obj/item/book/granter/spell/mindswap,
	/obj/item/book/granter/spell/forcewall,
	/obj/item/book/granter/spell/knock,
	/obj/item/book/granter/spell/barnyard,
	/obj/item/book/granter/spell/charge,
	/obj/item/book/granter/spell/summonitem,
	/obj/item/gun/magic/wand,
	/obj/item/gun/magic/wand/death,
	/obj/item/gun/magic/wand/resurrection,
	/obj/item/gun/magic/wand/polymorph,
	/obj/item/gun/magic/wand/teleport,
	/obj/item/gun/magic/wand/door,
	/obj/item/gun/magic/wand/fireball,
	/obj/item/gun/magic/staff/healing,
	/obj/item/gun/magic/staff/door,
	/obj/item/scrying,
	/obj/item/voodoo,
	/obj/item/warpwhistle,
	/obj/item/clothing/suit/space/hardsuit/shielded/wizard,
	/obj/item/immortality_talisman,
	/obj/item/melee/ghost_sword))

GLOBAL_LIST_INIT(summoned_special_magic, list(
	/obj/item/gun/magic/staff/change,
	/obj/item/gun/magic/staff/animate,
	/obj/item/storage/belt/wands/full,
	/obj/item/antag_spawner/contract,
	/obj/item/gun/magic/staff/chaos,
	/obj/item/necromantic_stone,
	/obj/item/blood_contract))

//everything above except for single use spellbooks, because they are counted separately (and are for basic bitches anyways)
GLOBAL_LIST_INIT(summoned_magic_objectives, list(
	/obj/item/antag_spawner/contract,
	/obj/item/blood_contract,
	/obj/item/clothing/suit/space/hardsuit/shielded/wizard,
	/obj/item/gun/magic,
	/obj/item/immortality_talisman,
	/obj/item/melee/ghost_sword,
	/obj/item/necromantic_stone,
	/obj/item/scrying,
	/obj/item/spellbook,
	/obj/item/storage/belt/wands/full,
	/obj/item/voodoo,
	/obj/item/warpwhistle))

// If true, it's the probability of triggering "survivor" antag.
GLOBAL_VAR_INIT(summon_guns_triggered, FALSE)
GLOBAL_VAR_INIT(summon_magic_triggered, FALSE)

/proc/give_guns(mob/living/carbon/human/H)
	if(H.stat == DEAD || !(H.client))
		return
	if(H.mind)
		if(iswizard(H) || H.mind.has_antag_datum(/datum/antagonist/survivalist/guns))
			return

	if(prob(GLOB.summon_guns_triggered) && !(H.mind.has_antag_datum(/datum/antagonist)))
		SSticker.mode.traitors += H.mind

		H.mind.add_antag_datum(/datum/antagonist/survivalist/guns)
		H.log_message("was made into a survivalist, and trusts no one!", LOG_ATTACK, color="red")

	var/gun_type = pick(GLOB.summoned_guns)
	var/obj/item/gun/G = new gun_type(get_turf(H))
	if (istype(G)) // The list contains some non-gun type guns like the speargun which do not have this proc
		G.unlock()
	playsound(get_turf(H),'sound/magic/summon_guns.ogg', 50, TRUE)

	var/in_hand = H.put_in_hands(G) // not always successful

	to_chat(H, "<span class='warning'>\A [G] appears [in_hand ? "in your hand" : "at your feet"]!</span>")

/proc/give_magic(mob/living/carbon/human/H)
	if(H.stat == DEAD || !(H.client))
		return
	if(H.mind)
		if(iswizard(H) || H.mind.has_antag_datum(/datum/antagonist/survivalist/magic))
			return

	if(prob(GLOB.summon_magic_triggered) && !(H.mind.has_antag_datum(/datum/antagonist)))
		H.mind.add_antag_datum(/datum/antagonist/survivalist/magic)
		H.log_message("was made into a survivalist, and trusts no one!</font>", LOG_ATTACK, color="red")

	var/magic_type = pick(GLOB.summoned_magic)
	var/lucky = FALSE
	if(prob(SPECIALIST_MAGIC_PROB))
		magic_type = pick(GLOB.summoned_special_magic)
		lucky = TRUE

	var/obj/item/M = new magic_type(get_turf(H))
	playsound(get_turf(H),'sound/magic/summon_magic.ogg', 50, TRUE)

	var/in_hand = H.put_in_hands(M)

	to_chat(H, "<span class='warning'>\A [M] appears [in_hand ? "in your hand" : "at your feet"]!</span>")
	if(lucky)
		to_chat(H, "<span class='notice'>You feel incredibly lucky.</span>")


/proc/rightandwrong(summon_type, mob/user, survivor_probability)
	if(user) //in this case either someone holding a spellbook or a badmin
		to_chat(user, "<span class='warning'>You summoned [summon_type]!</span>")
		message_admins("[ADMIN_LOOKUPFLW(user)] summoned [summon_type]!")
		log_game("[key_name(user)] summoned [summon_type]!")

	if(summon_type == SUMMON_MAGIC)
		GLOB.summon_magic_triggered = survivor_probability
	else if(summon_type == SUMMON_GUNS)
		GLOB.summon_guns_triggered = survivor_probability
	else
		CRASH("Bad summon_type given: [summon_type]")

	for(var/mob/living/carbon/human/H in GLOB.player_list)
		var/turf/T = get_turf(H)
		if(T && is_away_level(T.z))
			continue
		if(summon_type == SUMMON_MAGIC)
			give_magic(H)
		else
			give_guns(H)

/proc/summonevents()
	if(!SSevents.wizardmode)
		SSevents.frequency_lower = 600									//1 minute lower bound
		SSevents.frequency_upper = 3000									//5 minutes upper bound
		SSevents.toggleWizardmode()
		SSevents.reschedule()

	else 																//Speed it up
		SSevents.frequency_upper -= 600	//The upper bound falls a minute each time, making the AVERAGE time between events lessen
		if(SSevents.frequency_upper < SSevents.frequency_lower) //Sanity
			SSevents.frequency_upper = SSevents.frequency_lower

		SSevents.reschedule()
		message_admins("Summon Events intensifies, events will now occur every [SSevents.frequency_lower / 600] to [SSevents.frequency_upper / 600] minutes.")
		log_game("Summon Events was increased!")

#undef SPECIALIST_MAGIC_PROB
