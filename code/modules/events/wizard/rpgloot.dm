/datum/round_event_control/wizard/rpgloot //its time to minmax your shit
	name = "RPG Loot"
	weight = 3
	typepath = /datum/round_event/wizard/rpgloot
	max_occurrences = 1
	earliest_start = 0 MINUTES

/datum/round_event/wizard/rpgloot/start()
	var/upgrade_scroll_chance = 0
	for(var/obj/item/I in world)
		CHECK_TICK

		if(!(I.flags_1 & INITIALIZED_1))
			continue

		I.AddComponent(/datum/component/fantasy)

		if(istype(I, /obj/item/storage))
			var/obj/item/storage/S = I
			var/datum/component/storage/STR = S.GetComponent(/datum/component/storage)
			if(prob(upgrade_scroll_chance) && S.contents.len < STR.max_items && !S.invisibility)
				var/obj/item/upgradescroll/scroll = new(get_turf(S))
				SEND_SIGNAL(S, COMSIG_TRY_STORAGE_INSERT, scroll, null, TRUE, TRUE)
				upgrade_scroll_chance = max(0,upgrade_scroll_chance-100)
				if(isturf(scroll.loc))
					qdel(scroll)

			upgrade_scroll_chance += 25

	GLOB.rpg_loot_items = TRUE

/obj/item/upgradescroll
	name = "item fortification scroll"
	desc = "Somehow, this piece of paper can be applied to items to make them \"better\". Apparently there's a risk of losing the item if it's already \"too good\". <i>This all feels so arbitrary...</i>"
	icon = 'icons/obj/wizard.dmi'
	icon_state = "scroll"
	w_class = WEIGHT_CLASS_TINY

	var/upgrade_amount = 1
	var/can_backfire = TRUE
	var/uses = 1

/obj/item/upgradescroll/afterattack(obj/item/target, mob/user , proximity)
	. = ..()
	if(!proximity || !istype(target))
		return

	target.AddComponent(/datum/component/fantasy, upgrade_amount, null, null, can_backfire, TRUE)

	if(--uses <= 0)
		qdel(src)

/obj/item/upgradescroll/unlimited
	name = "unlimited foolproof item fortification scroll"
	desc = "Somehow, this piece of paper can be applied to items to make them \"better\". This scroll is made from the tongues of dead paper wizards, and can be used an unlimited number of times, with no drawbacks."
	uses = INFINITY
	can_backfire = FALSE
