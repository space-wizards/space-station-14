


/*
	Dear ninja gloves

	This isn't because I like you
	this is because your father is a bastard

	...
	I guess you're a little cool.
	 -Sayu


	see ninjaDrainAct.dm for ninjadrain_act()
	Touch() simply calls this on it's target now
	Ninja's electricuting people when?
	-Remie

*/


/obj/item/clothing/gloves/space_ninja
	desc = "These nano-enhanced gloves insulate from electricity and provide fire resistance."
	name = "ninja gloves"
	icon_state = "s-ninja"
	item_state = "s-ninja"
	siemens_coefficient = 0
	cold_protection = HANDS
	min_cold_protection_temperature = GLOVES_MIN_TEMP_PROTECT
	heat_protection = HANDS
	max_heat_protection_temperature = GLOVES_MAX_TEMP_PROTECT
	strip_delay = 120
	resistance_flags = LAVA_PROOF | FIRE_PROOF | ACID_PROOF
	var/draining = 0
	var/candrain = 0
	var/mindrain = 200
	var/maxdrain = 400


/obj/item/clothing/gloves/space_ninja/Touch(atom/A,proximity)
	if(!candrain || draining)
		return FALSE
	if(!ishuman(loc))
		return FALSE	//Only works while worn

	var/mob/living/carbon/human/H = loc

	var/obj/item/clothing/suit/space/space_ninja/suit = H.wear_suit
	if(!istype(suit))
		return FALSE
	if(isturf(A))
		return FALSE

	if(!proximity)
		return FALSE

	A.add_fingerprint(H)

	draining = TRUE
	. = A.ninjadrain_act(suit,H,src)
	draining = FALSE

	if(isnum(.)) //Numerical values of drained handle their feedback here, Alpha values handle it themselves (Research hacking)
		if(.)
			to_chat(H, "<span class='notice'>Gained <B>[DisplayEnergy(.)]</B> of energy from [A].</span>")
		else
			to_chat(H, "<span class='danger'>\The [A] has run dry of energy, you must find another source!</span>")
	else
		. = FALSE	//as to not cancel attack_hand()


/obj/item/clothing/gloves/space_ninja/proc/toggledrain()
	var/mob/living/carbon/human/U = loc
	to_chat(U, "<span class='notice'>You [candrain?"disable":"enable"] special interaction.</span>")
	candrain=!candrain

/obj/item/clothing/gloves/space_ninja/examine(mob/user)
	. = ..()
	if(HAS_TRAIT_FROM(src, TRAIT_NODROP, NINJA_SUIT_TRAIT))
		. += "The energy drain mechanism is <B>[candrain?"active":"inactive"]</B>."
