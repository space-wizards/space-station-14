//////////////////////////////////////////////
//////////     SLIME CROSSBREEDS    //////////
//////////////////////////////////////////////
// A system of combining two extract types. //
// Performed by feeding a slime 10 of an    //
// extract color.                           //
//////////////////////////////////////////////
/*==========================================*\
To add a crossbreed:
	The file name is automatically selected
	by the crossbreeding effect, which uses
	the format slimecross/[modifier]/[color].

	If a crossbreed doesn't exist, don't
	worry. If no file is found at that
	location, it will simple display that
	the crossbreed was too unstable.

	As a result, do not feel the need to
	try to add all of the crossbred
	effects at once, if you're here and
	trying to make a new slime type. Just
	get your slimetype in the codebase and
	get around to the crossbreeds eventually!
\*==========================================*/

/obj/item/slimecross //The base type for crossbred extracts. Mostly here for posterity, and to set base case things.
	name = "crossbred slime extract"
	desc = "An extremely potent slime extract, formed through crossbreeding."
	icon = 'icons/obj/slimecrossing.dmi'
	icon_state = "base"
	var/colour = "null"
	var/effect = "null"
	var/effect_desc = "null"
	force = 0
	w_class = WEIGHT_CLASS_TINY
	throwforce = 0
	throw_speed = 3
	throw_range = 6

/obj/item/slimecross/examine(mob/user)
	. = ..()
	if(effect_desc)
		. += "<span class='notice'>[effect_desc]</span>"

/obj/item/slimecross/Initialize()
	. = ..()
	name =  effect + " " + colour + " extract"
	var/itemcolor = "#FFFFFF"
	switch(colour)
		if("orange")
			itemcolor = "#FFA500"
		if("purple")
			itemcolor = "#B19CD9"
		if("blue")
			itemcolor = "#ADD8E6"
		if("metal")
			itemcolor = "#7E7E7E"
		if("yellow")
			itemcolor = "#FFFF00"
		if("dark purple")
			itemcolor = "#551A8B"
		if("dark blue")
			itemcolor = "#0000FF"
		if("silver")
			itemcolor = "#D3D3D3"
		if("bluespace")
			itemcolor = "#32CD32"
		if("sepia")
			itemcolor = "#704214"
		if("cerulean")
			itemcolor = "#2956B2"
		if("pyrite")
			itemcolor = "#FAFAD2"
		if("red")
			itemcolor = "#FF0000"
		if("green")
			itemcolor = "#00FF00"
		if("pink")
			itemcolor = "#FF69B4"
		if("gold")
			itemcolor = "#FFD700"
		if("oil")
			itemcolor = "#505050"
		if("black")
			itemcolor = "#000000"
		if("light pink")
			itemcolor = "#FFB6C1"
		if("adamantine")
			itemcolor = "#008B8B"
	add_atom_colour(itemcolor, FIXED_COLOUR_PRIORITY)

/obj/item/slimecrossbeaker //To be used as a result for extract reactions that make chemicals.
	name = "result extract"
	desc = "You shouldn't see this."
	icon = 'icons/obj/slimecrossing.dmi'
	icon_state = "base"
	var/del_on_empty = TRUE
	var/list/list_reagents

/obj/item/slimecrossbeaker/Initialize()
	. = ..()
	create_reagents(50, INJECTABLE | DRAWABLE)
	if(list_reagents)
		for(var/reagent in list_reagents)
			reagents.add_reagent(reagent, list_reagents[reagent])
	if(del_on_empty)
		START_PROCESSING(SSobj,src)

/obj/item/slimecrossbeaker/Destroy()
	STOP_PROCESSING(SSobj,src)
	return ..()

/obj/item/slimecrossbeaker/process()
	if(!reagents.total_volume)
		visible_message("<span class='notice'>[src] has been drained completely, and melts away.</span>")
		qdel(src)

/obj/item/slimecrossbeaker/bloodpack //Pack of 50u blood. Deletes on empty.
	name = "blood extract"
	desc = "A sphere of liquid blood, somehow managing to stay together."
	color = "#FF0000"
	list_reagents = list(/datum/reagent/blood = 50)

/obj/item/slimecrossbeaker/pax //5u synthpax.
	name = "peace-inducing extract"
	desc = "A small blob of synthetic pax."
	color = "#FFCCCC"
	list_reagents = list(/datum/reagent/pax/peaceborg = 5)

/obj/item/slimecrossbeaker/omnizine //15u omnizine.
	name = "healing extract"
	desc = "A gelatinous extract of pure omnizine."
	color = "#FF00FF"
	list_reagents = list(/datum/reagent/medicine/omnizine = 15)

/obj/item/slimecrossbeaker/autoinjector //As with the above, but automatically injects whomever it is used on with contents.
	var/ignore_flags = FALSE
	var/self_use_only = FALSE

/obj/item/slimecrossbeaker/autoinjector/Initialize()
	. = ..()
	reagents.flags = DRAWABLE // Cannot be refilled, since it's basically an autoinjector!

/obj/item/slimecrossbeaker/autoinjector/attack(mob/living/M, mob/user)
	if(!reagents.total_volume)
		to_chat(user, "<span class='warning'>[src] is empty!</span>")
		return
	if(!iscarbon(M))
		return
	if(self_use_only && M != user)
		to_chat(user, "<span class='warning'>This can only be used on yourself.</span>")
		return
	if(reagents.total_volume && (ignore_flags || M.can_inject(user, 1)))
		reagents.trans_to(M, reagents.total_volume, transfered_by = user)
		if(user != M)
			to_chat(M, "<span class='warning'>[user] presses [src] against you!</span>")
			to_chat(user, "<span class='notice'>You press [src] against [M], injecting [M.p_them()].</span>")
		else
			to_chat(user, "<span class='notice'>You press [src] against yourself, and it flattens against you!</span>")
	else
		to_chat(user, "<span class='warning'>There's no place to stick [src]!</span>")

/obj/item/slimecrossbeaker/autoinjector/regenpack
	ignore_flags = TRUE //It is, after all, intended to heal.
	name = "mending solution"
	desc = "A strange glob of sweet-smelling semifluid, which seems to stick to skin rather easily."
	color = "#FF00FF"
	list_reagents = list(/datum/reagent/medicine/regen_jelly = 20)

/obj/item/slimecrossbeaker/autoinjector/slimejelly //Primarily for slimepeople, but you do you.
	self_use_only = TRUE
	ignore_flags = TRUE
	name = "slime jelly bubble"
	desc = "A sphere of slime jelly. It seems to stick to your skin, but avoids other surfaces."
	color = "#00FF00"
	list_reagents = list(/datum/reagent/toxin/slimejelly = 50)

/obj/item/slimecrossbeaker/autoinjector/peaceandlove
	name = "peaceful distillation"
	desc = "A light pink gooey sphere. Simply touching it makes you a little dizzy."
	color = "#DDAAAA"
	list_reagents = list(/datum/reagent/pax/peaceborg = 10, /datum/reagent/drug/space_drugs = 15) //Peace, dudes

/obj/item/slimecrossbeaker/autoinjector/peaceandlove/Initialize()
	. = ..()
	reagents.flags = NONE // It won't be *that* easy to get your hands on pax.

/obj/item/slimecrossbeaker/autoinjector/slimestimulant
	name = "invigorating gel"
	desc = "A bubbling purple mixture, designed to heal and boost movement."
	color = "#FF00FF"
	list_reagents = list(/datum/reagent/medicine/regen_jelly = 30, /datum/reagent/drug/methamphetamine = 9)
