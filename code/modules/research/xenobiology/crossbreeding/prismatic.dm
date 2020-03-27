/*
Prismatic extracts:
	Becomes an infinite-use paintbrush.
*/
/obj/item/slimecross/prismatic
	name = "prismatic extract"
	desc = "It's constantly wet with a semi-transparent, colored goo."
	effect = "prismatic"
	effect_desc = "When used it paints whatever it hits."
	icon_state = "prismatic"
	var/paintcolor = "#FFFFFF"

/obj/item/slimecross/prismatic/afterattack(turf/target, mob/user, proximity)
	if(!proximity)
		return
	if(!istype(target) || isspaceturf(target))
		return
	target.add_atom_colour(paintcolor, WASHABLE_COLOUR_PRIORITY)
	playsound(target, 'sound/effects/slosh.ogg', 20, TRUE)

/obj/item/slimecross/prismatic/grey/
	colour = "grey"
	desc = "It's constantly wet with a pungent-smelling, clear chemical."

/obj/item/slimecross/prismatic/grey/afterattack(turf/target, mob/user, proximity)
	. = ..()
	if(!proximity)
		return
	if(istype(target) && target.color != initial(target.color))
		target.remove_atom_colour(WASHABLE_COLOUR_PRIORITY)
		playsound(target, 'sound/effects/slosh.ogg', 20, TRUE)

/obj/item/slimecross/prismatic/orange
	paintcolor = "#FFA500"
	colour = "orange"

/obj/item/slimecross/prismatic/purple
	paintcolor = "#B19CD9"
	colour = "purple"

/obj/item/slimecross/prismatic/blue
	paintcolor = "#ADD8E6"
	colour = "blue"

/obj/item/slimecross/prismatic/metal
	paintcolor = "#7E7E7E"
	colour = "metal"

/obj/item/slimecross/prismatic/yellow
	paintcolor = "#FFFF00"
	colour = "yellow"

/obj/item/slimecross/prismatic/darkpurple
	paintcolor = "#551A8B"
	colour = "dark purple"

/obj/item/slimecross/prismatic/darkblue
	paintcolor = "#0000FF"
	colour = "dark blue"

/obj/item/slimecross/prismatic/silver
	paintcolor = "#D3D3D3"
	colour = "silver"

/obj/item/slimecross/prismatic/bluespace
	paintcolor = "#32CD32"
	colour = "bluespace"

/obj/item/slimecross/prismatic/sepia
	paintcolor = "#704214"
	colour = "sepia"

/obj/item/slimecross/prismatic/cerulean
	paintcolor = "#2956B2"
	colour = "cerulean"

/obj/item/slimecross/prismatic/pyrite
	paintcolor = "#FAFAD2"
	colour = "pyrite"

/obj/item/slimecross/prismatic/red
	paintcolor = "#FF0000"
	colour = "red"

/obj/item/slimecross/prismatic/green
	paintcolor = "#00FF00"
	colour = "green"

/obj/item/slimecross/prismatic/pink
	paintcolor = "#FF69B4"
	colour = "pink"

/obj/item/slimecross/prismatic/gold
	paintcolor = "#FFD700"
	colour = "gold"

/obj/item/slimecross/prismatic/oil
	paintcolor = "#505050"
	colour = "oil"

/obj/item/slimecross/prismatic/black
	paintcolor = "#000000"
	colour = "black"

/obj/item/slimecross/prismatic/lightpink
	paintcolor = "#FFB6C1"
	colour = "light pink"

/obj/item/slimecross/prismatic/adamantine
	paintcolor = "#008B8B"
	colour = "adamantine"

/obj/item/slimecross/prismatic/rainbow
	paintcolor = "#FFFFFF"
	colour = "rainbow"

/obj/item/slimecross/prismatic/rainbow/attack_self(mob/user)
	var/newcolor = input(user, "Choose the slime color:", "Color change",paintcolor) as color|null
	if ((user.get_active_held_item() != src || user.stat || user.restrained()))
		return
	if(!newcolor)
		return
	paintcolor = newcolor
	return
