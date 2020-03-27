/obj/item/picket_sign
	icon_state = "picket"
	name = "blank picket sign"
	desc = "It's blank."
	force = 5
	w_class = WEIGHT_CLASS_BULKY
	attack_verb = list("bashed","smacked")
	resistance_flags = FLAMMABLE

	var/label = ""
	var/last_wave = 0

/obj/item/picket_sign/cyborg
	name = "metallic nano-sign"
	desc = "A high tech picket sign used by silicons that can reprogram its surface at will. Probably hurts to get hit by, too."
	force = 13
	resistance_flags = NONE
	actions_types = list(/datum/action/item_action/nano_picket_sign)

/obj/item/picket_sign/proc/retext(mob/user)
	if(!user.is_literate())
		to_chat(user, "<span class='notice'>You scribble illegibly on [src]!</span>")
		return
	var/txt = stripped_input(user, "What would you like to write on the sign?", "Sign Label", null , 30)
	if(txt && user.canUseTopic(src, BE_CLOSE))
		label = txt
		name = "[label] sign"
		desc =	"It reads: [label]"

/obj/item/picket_sign/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/pen) || istype(W, /obj/item/toy/crayon))
		retext(user)
	else
		return ..()

/obj/item/picket_sign/attack_self(mob/living/carbon/human/user)
	if( last_wave + 20 < world.time )
		last_wave = world.time
		if(label)
			user.visible_message("<span class='warning'>[user] waves around \the \"[label]\" sign.</span>")
		else
			user.visible_message("<span class='warning'>[user] waves around blank sign.</span>")
		user.changeNext_move(CLICK_CD_MELEE)

/datum/crafting_recipe/picket_sign
	name = "Picket Sign"
	result = /obj/item/picket_sign
	reqs = list(/obj/item/stack/rods = 1,
				/obj/item/stack/sheet/cardboard = 2)
	time = 80
	category = CAT_MISC
