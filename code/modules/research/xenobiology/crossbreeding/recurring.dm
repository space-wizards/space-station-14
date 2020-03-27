/*
Recurring extracts:
	Generates a new charge every few seconds.
	If depleted of its' last charge, stops working.
*/
/obj/item/slimecross/recurring
	name = "recurring extract"
	desc = "A tiny, glowing core, wrapped in several layers of goo."
	effect = "recurring"
	icon_state = "recurring"
	var/extract_type
	var/obj/item/slime_extract/extract
	var/cooldown = 0
	var/max_cooldown = 5 //In sets of 2 seconds.

/obj/item/slimecross/recurring/Initialize()
	. = ..()
	extract = new extract_type(src.loc)
	visible_message("<span class='notice'>[src] wraps a layer of goo around itself!</span>")
	extract.name = name
	extract.desc = desc
	extract.icon = icon
	extract.icon_state = icon_state
	extract.color = color
	extract.recurring = TRUE
	src.forceMove(extract)
	START_PROCESSING(SSobj,src)

/obj/item/slimecross/recurring/process()
	if(cooldown > 0)
		cooldown--
	else if(extract.Uses < 10 && extract.Uses > 0)
		extract.Uses++
		cooldown = max_cooldown
	else if(extract.Uses <= 0)
		extract.visible_message("<span class='warning'>The light inside [extract] flickers and dies out.</span>")
		extract.desc = "A tiny, inert core, bleeding dark, cerulean-colored goo."
		extract.icon_state = "prismatic"
		qdel(src)

/obj/item/slimecross/recurring/Destroy()
	. = ..()
	STOP_PROCESSING(SSobj,src)

/obj/item/slimecross/recurring/grey
	extract_type = /obj/item/slime_extract/grey
	colour = "grey"

/obj/item/slimecross/recurring/orange
	extract_type = /obj/item/slime_extract/orange
	colour = "orange"

/obj/item/slimecross/recurring/purple
	extract_type = /obj/item/slime_extract/purple
	colour = "purple"

/obj/item/slimecross/recurring/blue
	extract_type = /obj/item/slime_extract/blue
	colour = "blue"

/obj/item/slimecross/recurring/metal
	extract_type = /obj/item/slime_extract/metal
	colour = "metal"
	max_cooldown = 10

/obj/item/slimecross/recurring/yellow
	extract_type = /obj/item/slime_extract/yellow
	colour = "yellow"
	max_cooldown = 10

/obj/item/slimecross/recurring/darkpurple
	extract_type = /obj/item/slime_extract/darkpurple
	colour = "dark purple"
	max_cooldown = 10

/obj/item/slimecross/recurring/darkblue
	extract_type = /obj/item/slime_extract/darkblue
	colour = "dark blue"

/obj/item/slimecross/recurring/silver
	extract_type = /obj/item/slime_extract/silver
	colour = "silver"

/obj/item/slimecross/recurring/bluespace
	extract_type = /obj/item/slime_extract/bluespace
	colour = "bluespace"

/obj/item/slimecross/recurring/sepia
	extract_type = /obj/item/slime_extract/sepia
	colour = "sepia"
	max_cooldown = 18 //No infinite timestop for you!

/obj/item/slimecross/recurring/cerulean
	extract_type = /obj/item/slime_extract/cerulean
	colour = "cerulean"

/obj/item/slimecross/recurring/pyrite
	extract_type = /obj/item/slime_extract/pyrite
	colour = "pyrite"

/obj/item/slimecross/recurring/red
	extract_type = /obj/item/slime_extract/red
	colour = "red"

/obj/item/slimecross/recurring/green
	extract_type = /obj/item/slime_extract/green
	colour = "green"

/obj/item/slimecross/recurring/pink
	extract_type = /obj/item/slime_extract/pink
	colour = "pink"

/obj/item/slimecross/recurring/gold
	extract_type = /obj/item/slime_extract/gold
	colour = "gold"
	max_cooldown = 15

/obj/item/slimecross/recurring/oil
	extract_type = /obj/item/slime_extract/oil
	colour = "oil" //Why would you want this?

/obj/item/slimecross/recurring/black
	extract_type = /obj/item/slime_extract/black
	colour = "black"

/obj/item/slimecross/recurring/lightpink
	extract_type = /obj/item/slime_extract/lightpink
	colour = "light pink"

/obj/item/slimecross/recurring/adamantine
	extract_type = /obj/item/slime_extract/adamantine
	colour = "adamantine"
	max_cooldown = 10

/obj/item/slimecross/recurring/rainbow
	extract_type = /obj/item/slime_extract/rainbow
	colour = "rainbow"
	max_cooldown = 20 //It's pretty powerful.
