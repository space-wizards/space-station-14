/*
Self-sustaining extracts:
	Produces 4 extracts that do not need reagents.
*/
/obj/item/slimecross/selfsustaining
	name = "self-sustaining extract"
	effect = "self-sustaining"
	icon_state = "selfsustaining"
	var/extract_type = /obj/item/slime_extract

/obj/item/autoslime
	name = "autoslime"
	desc = "It resembles a normal slime extract, but seems filled with a strange, multi-colored fluid."
	var/obj/item/slime_extract/extract
	var/effect_desc = "A self-sustaining slime extract. When used, lets you choose which reaction you want."

//Just divides into the actual item.
/obj/item/slimecross/selfsustaining/Initialize()
	..()
	visible_message("<span class='warning'>The [src] shudders, and splits into four smaller extracts.</span>")
	for(var/i = 0, i < 4, i++)
		var/obj/item/autoslime/A = new /obj/item/autoslime(src.loc)
		var/obj/item/slime_extract/X = new extract_type(A)
		A.extract = X
		A.icon = icon
		A.icon_state = icon_state
		A.color = color
		A.name = "self-sustaining " + colour + " extract"
	return INITIALIZE_HINT_QDEL

/obj/item/autoslime/Initialize()
	return ..()

/obj/item/autoslime/attack_self(mob/user)
	var/reagentselect = input(user, "Choose the reagent the extract will produce.", "Self-sustaining Reaction") as null|anything in sortList(extract.activate_reagents, /proc/cmp_typepaths_asc)
	var/amount = 5
	var/secondary

	if ((user.get_active_held_item() != src || user.stat || user.restrained()))
		return
	if(!reagentselect)
		return
	if(reagentselect == "lesser plasma")
		amount = 4
		reagentselect = /datum/reagent/toxin/plasma
	if(reagentselect == "holy water and uranium")
		reagentselect = /datum/reagent/water/holywater
		secondary = /datum/reagent/uranium
	extract.forceMove(user.drop_location())
	qdel(src)
	user.put_in_active_hand(extract)
	extract.reagents.add_reagent(reagentselect,amount)
	if(secondary)
		extract.reagents.add_reagent(secondary,amount)

/obj/item/autoslime/examine(mob/user)
  . = ..()
  if(effect_desc)
    . += "<span class='notice'>[effect_desc]</span>"

//Different types.

/obj/item/slimecross/selfsustaining/grey
	extract_type = /obj/item/slime_extract/grey
	colour = "grey"

/obj/item/slimecross/selfsustaining/orange
	extract_type = /obj/item/slime_extract/orange
	colour = "orange"

/obj/item/slimecross/selfsustaining/purple
	extract_type = /obj/item/slime_extract/purple
	colour = "purple"

/obj/item/slimecross/selfsustaining/blue
	extract_type = /obj/item/slime_extract/blue
	colour = "blue"

/obj/item/slimecross/selfsustaining/metal
	extract_type = /obj/item/slime_extract/metal
	colour = "metal"

/obj/item/slimecross/selfsustaining/yellow
	extract_type = /obj/item/slime_extract/yellow
	colour = "yellow"

/obj/item/slimecross/selfsustaining/darkpurple
	extract_type = /obj/item/slime_extract/darkpurple
	colour = "dark purple"

/obj/item/slimecross/selfsustaining/darkblue
	extract_type = /obj/item/slime_extract/darkblue
	colour = "dark blue"

/obj/item/slimecross/selfsustaining/silver
	extract_type = /obj/item/slime_extract/silver
	colour = "silver"

/obj/item/slimecross/selfsustaining/bluespace
	extract_type = /obj/item/slime_extract/bluespace
	colour = "bluespace"

/obj/item/slimecross/selfsustaining/sepia
	extract_type = /obj/item/slime_extract/sepia
	colour = "sepia"

/obj/item/slimecross/selfsustaining/cerulean
	extract_type = /obj/item/slime_extract/cerulean
	colour = "cerulean"

/obj/item/slimecross/selfsustaining/pyrite
	extract_type = /obj/item/slime_extract/pyrite
	colour = "pyrite"

/obj/item/slimecross/selfsustaining/red
	extract_type = /obj/item/slime_extract/red
	colour = "red"

/obj/item/slimecross/selfsustaining/green
	extract_type = /obj/item/slime_extract/green
	colour = "green"

/obj/item/slimecross/selfsustaining/pink
	extract_type = /obj/item/slime_extract/pink
	colour = "pink"

/obj/item/slimecross/selfsustaining/gold
	extract_type = /obj/item/slime_extract/gold
	colour = "gold"

/obj/item/slimecross/selfsustaining/oil
	extract_type = /obj/item/slime_extract/oil
	colour = "oil"

/obj/item/slimecross/selfsustaining/black
	extract_type = /obj/item/slime_extract/black
	colour = "black"

/obj/item/slimecross/selfsustaining/lightpink
	extract_type = /obj/item/slime_extract/lightpink
	colour = "light pink"

/obj/item/slimecross/selfsustaining/adamantine
	extract_type = /obj/item/slime_extract/adamantine
	colour = "adamantine"

/obj/item/slimecross/selfsustaining/rainbow
	extract_type = /obj/item/slime_extract/rainbow
	colour = "rainbow"
