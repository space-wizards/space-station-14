/obj/item/quantum_keycard
	name = "quantum keycard"
	desc = "A keycard able to link to a quantum pad's particle signature, allowing other quantum pads to travel there instead of their linked pad."
	icon = 'icons/obj/device.dmi'
	icon_state = "quantum_keycard"
	item_state = "card-id"
	lefthand_file = 'icons/mob/inhands/equipment/idcards_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/idcards_righthand.dmi'
	w_class = WEIGHT_CLASS_TINY
	var/obj/machinery/quantumpad/qpad

/obj/item/quantum_keycard/examine(mob/user)
	. = ..()
	if(qpad)
		. += "It's currently linked to a quantum pad."
		. += "<span class='notice'>Alt-click to unlink the keycard.</span>"
	else
		. += "<span class='notice'>Insert [src] into an active quantum pad to link it.</span>"

/obj/item/quantum_keycard/AltClick(mob/living/user)
	if(!istype(user) || !user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		return
	to_chat(user, "<span class='notice'>You start pressing [src]'s unlink button...</span>")
	if(do_after(user, 40, target = src))
		to_chat(user, "<span class='notice'>The keycard beeps twice and disconnects the quantum link.</span>")
		qpad = null

/obj/item/quantum_keycard/update_icon_state()
	if(qpad)
		icon_state = "quantum_keycard_on"
	else
		icon_state = initial(icon_state)
