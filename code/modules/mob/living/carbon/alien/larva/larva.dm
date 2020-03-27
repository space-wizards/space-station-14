/mob/living/carbon/alien/larva
	name = "alien larva"
	real_name = "alien larva"
	icon_state = "larva0"
	pass_flags = PASSTABLE | PASSMOB
	mob_size = MOB_SIZE_SMALL
	density = FALSE
	hud_type = /datum/hud/larva

	maxHealth = 25
	health = 25

	var/amount_grown = 0
	var/max_grown = 100
	var/time_of_birth

	rotate_on_lying = 0
	bodyparts = list(/obj/item/bodypart/chest/larva, /obj/item/bodypart/head/larva)


//This is fine right now, if we're adding organ specific damage this needs to be updated
/mob/living/carbon/alien/larva/Initialize()

	AddAbility(new/obj/effect/proc_holder/alien/hide(null))
	AddAbility(new/obj/effect/proc_holder/alien/larva_evolve(null))
	. = ..()

/mob/living/carbon/alien/larva/create_internal_organs()
	internal_organs += new /obj/item/organ/alien/plasmavessel/small/tiny
	..()

//This needs to be fixed
/mob/living/carbon/alien/larva/Stat()
	..()
	if(statpanel("Status"))
		stat(null, "Progress: [amount_grown]/[max_grown]")

/mob/living/carbon/alien/larva/adjustPlasma(amount)
	if(stat != DEAD && amount > 0)
		amount_grown = min(amount_grown + 1, max_grown)
	..(amount)

//can't equip anything
/mob/living/carbon/alien/larva/attack_ui(slot_id)
	return

/mob/living/carbon/alien/larva/restrained(ignore_grab)
	. = 0

// new damage icon system
// now constructs damage icon for each organ from mask * damage field


/mob/living/carbon/alien/larva/show_inv(mob/user)
	return

/mob/living/carbon/alien/larva/toggle_throw_mode()
	return

/mob/living/carbon/alien/larva/start_pulling(atom/movable/AM, state, force = move_force, supress_message = FALSE)
	return

/mob/living/carbon/alien/larva/stripPanelUnequip(obj/item/what, mob/who)
	to_chat(src, "<span class='warning'>You don't have the dexterity to do this!</span>")
	return

/mob/living/carbon/alien/larva/stripPanelEquip(obj/item/what, mob/who)
	to_chat(src, "<span class='warning'>You don't have the dexterity to do this!</span>")
	return
