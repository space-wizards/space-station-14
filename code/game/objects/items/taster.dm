/obj/item/taster
	name = "taster"
	desc = "Tastes things, so you don't have to!"
	icon = 'icons/obj/surgery.dmi'
	icon_state = "tonguenormal"

	w_class = WEIGHT_CLASS_TINY

	speech_span = null

	var/taste_sensitivity = 15

/obj/item/taster/afterattack(atom/O, mob/user, proximity)
	. = ..()
	if(!proximity)
		return

	if(O.reagents)
		var/message = O.reagents.generate_taste_message(taste_sensitivity)
		to_chat(user, "<span class='notice'>[src] tastes <span class='italics'>[message]</span> in [O].</span>")
