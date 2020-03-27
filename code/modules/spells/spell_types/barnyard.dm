/obj/effect/proc_holder/spell/targeted/barnyardcurse
	name = "Curse of the Barnyard"
	desc = "This spell dooms an unlucky soul to possess the speech and facial attributes of a barnyard animal."
	school = "transmutation"
	charge_type = "recharge"
	charge_max	= 150
	charge_counter = 0
	clothes_req = FALSE
	stat_allowed = FALSE
	invocation = "KN'A FTAGHU, PUCK 'BTHNK!"
	invocation_type = "shout"
	range = 7
	cooldown_min = 30
	selection_type = "range"
	var/static/list/compatible_mobs_typecache = typecacheof(list(/mob/living/carbon/human, /mob/living/carbon/monkey))

	action_icon_state = "barn"

/obj/effect/proc_holder/spell/targeted/barnyardcurse/cast(list/targets, mob/user = usr)
	if(!targets.len)
		to_chat(user, "<span class='warning'>No target found in range!</span>")
		return

	var/mob/living/carbon/target = targets[1]


	if(!is_type_in_typecache(target, compatible_mobs_typecache))
		to_chat(user, "<span class='warning'>You are unable to curse [target]'s head!</span>")
		return

	if(!(target in oview(range)))
		to_chat(user, "<span class='warning'>[target.p_theyre(TRUE)] too far away!</span>")
		return

	if(target.anti_magic_check())
		to_chat(user, "<span class='warning'>The spell had no effect!</span>")
		target.visible_message("<span class='danger'>[target]'s face bursts into flames, which instantly burst outward, leaving [target] unharmed!</span>", \
						   "<span class='danger'>Your face starts burning up, but the flames are repulsed by your anti-magic protection!</span>")
		return

	var/list/masks = list(/obj/item/clothing/mask/pig/cursed, /obj/item/clothing/mask/cowmask/cursed, /obj/item/clothing/mask/horsehead/cursed)

	var/choice = pick(masks)
	var/obj/item/clothing/mask/magichead = new choice(get_turf(target))
	target.visible_message("<span class='danger'>[target]'s face bursts into flames, and a barnyard animal's head takes its place!</span>", \
						   "<span class='danger'>Your face burns up, and shortly after the fire you realise you have the face of a barnyard animal!</span>")
	if(!target.dropItemToGround(target.wear_mask))
		qdel(target.wear_mask)
	target.equip_to_slot_if_possible(magichead, ITEM_SLOT_MASK, 1, 1)

	target.flash_act()
