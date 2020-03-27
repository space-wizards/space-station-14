/obj/item/etherealballdeployer
	name = "Portable Ethereal Disco Ball"
	desc = "Press the button for a deployment of slightly-unethical PARTY!"
	icon = 'icons/obj/device.dmi'
	icon_state = "ethdisco"

/obj/item/etherealballdeployer/attack_self(mob/living/carbon/user)
	.=..()
	to_chat(user, "<span class='notice'>You deploy the Ethereal Disco Ball.</span>")
	new /obj/structure/etherealball(user.loc)
	qdel(src)

/obj/structure/etherealball
	name = "Ethereal Disco Ball"
	desc = "The ethics of this discoball are questionable."
	icon = 'icons/obj/device.dmi'
	icon_state = "ethdisco_head_0"
	anchored = TRUE
	density = TRUE
	var/TurnedOn = FALSE
	var/current_color
	var/TimerID
	var/range = 7
	var/power = 3

/obj/structure/etherealball/Initialize()
	. = ..()
	update_icon()

/obj/structure/etherealball/attack_hand(mob/living/carbon/human/user)
	. = ..()
	if(TurnedOn)
		TurnOff()
		to_chat(user, "<span class='notice'>You turn the disco ball off!</span>")
	else
		TurnOn()
		to_chat(user, "<span class='notice'>You turn the disco ball on!</span>")

/obj/structure/etherealball/AltClick(mob/living/carbon/human/user)
	. = ..()
	if(anchored)
		to_chat(user, "<span class='notice'>You unlock the disco ball.</span>")
		anchored = FALSE
	else
		to_chat(user, "<span class='notice'>You lock the disco ball.</span>")
		anchored = TRUE

/obj/structure/etherealball/proc/TurnOn()
	TurnedOn = TRUE //Same
	DiscoFever()

/obj/structure/etherealball/proc/TurnOff()
	TurnedOn = FALSE
	set_light(0)
	remove_atom_colour(TEMPORARY_COLOUR_PRIORITY)
	update_icon()
	if(TimerID)
		deltimer(TimerID)

/obj/structure/etherealball/proc/DiscoFever()
	remove_atom_colour(TEMPORARY_COLOUR_PRIORITY)
	current_color = random_color()
	set_light(range, power, current_color)
	add_atom_colour("#[current_color]", FIXED_COLOUR_PRIORITY)
	update_icon()
	TimerID = addtimer(CALLBACK(src, .proc/DiscoFever), 5, TIMER_STOPPABLE)  //Call ourselves every 0.5 seconds to change colors

/obj/structure/etherealball/update_icon_state()
	icon_state = "ethdisco_head_[TurnedOn]"

/obj/structure/etherealball/update_overlays()
	. = ..()
	var/mutable_appearance/base_overlay = mutable_appearance(icon, "ethdisco_base")
	base_overlay.appearance_flags = RESET_COLOR
	. += base_overlay
