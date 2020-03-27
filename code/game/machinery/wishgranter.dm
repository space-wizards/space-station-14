/obj/machinery/wish_granter
	name = "wish granter"
	desc = "You're not so sure about this, anymore..."
	icon = 'icons/obj/device.dmi'
	icon_state = "syndbeacon"

	use_power = NO_POWER_USE
	density = TRUE

	var/charges = 1
	var/insisting = 0

/obj/machinery/wish_granter/attack_hand(mob/living/carbon/user)
	. = ..()
	if(.)
		return
	if(charges <= 0)
		to_chat(user, "<span class='boldnotice'>The Wish Granter lies silent.</span>")
		return

	else if(!ishuman(user))
		to_chat(user, "<span class='boldnotice'>You feel a dark stirring inside of the Wish Granter, something you want nothing of. Your instincts are better than any man's.</span>")
		return

	else if(is_special_character(user))
		to_chat(user, "<span class='boldnotice'>Even to a heart as dark as yours, you know nothing good will come of this. Something instinctual makes you pull away.</span>")

	else if (!insisting)
		to_chat(user, "<span class='boldnotice'>Your first touch makes the Wish Granter stir, listening to you. Are you really sure you want to do this?</span>")
		insisting++

	else
		to_chat(user, "<span class='boldnotice'>You speak. [pick("I want the station to disappear","Humanity is corrupt, mankind must be destroyed","I want to be rich", "I want to rule the world","I want immortality.")]. The Wish Granter answers.</span>")
		to_chat(user, "<span class='boldnotice'>Your head pounds for a moment, before your vision clears. You are the avatar of the Wish Granter, and your power is LIMITLESS! And it's all yours. You need to make sure no one can take it from you. No one can know, first.</span>")

		charges--
		insisting = 0

		user.mind.add_antag_datum(/datum/antagonist/wishgranter)

		to_chat(user, "<span class='warning'>You have a very bad feeling about this.</span>")

	return
