/turf/open/floor/light
	name = "light floor"
	desc = "A wired glass tile embedded into the floor."
	light_range = 5
	icon_state = "light_on"
	floor_tile = /obj/item/stack/tile/light
	broken_states = list("light_broken")
	var/on = TRUE
	var/state = 0//0 = fine, 1 = flickering, 2 = breaking, 3 = broken
	var/list/coloredlights = list("g", "r", "y", "b", "p", "w", "s","o","g")
	var/currentcolor = 1
	var/can_modify_colour = TRUE
	tiled_dirt = FALSE


/turf/open/floor/light/examine(mob/user)
	. = ..()
	. += "<span class='notice'>There's a <b>small crack</b> on the edge of it.</span>"

/turf/open/floor/light/Initialize()
	. = ..()
	update_icon()

/turf/open/floor/light/break_tile()
	..()
	light_range = 0
	update_light()

/turf/open/floor/light/update_icon()
	..()
	if(on)
		switch(state)
			if(0)
				icon_state = "light_on-[coloredlights[currentcolor]]"
				set_light(1)
			if(1)
				var/num = pick("1","2","3","4")
				icon_state = "light_on_flicker[num]"
				set_light(1)
			if(2)
				icon_state = "light_on_broken"
				set_light(1)
			if(3)
				icon_state = "light_off"
				set_light(0)
	else
		set_light(0)
		icon_state = "light_off"


/turf/open/floor/light/ChangeTurf(path, new_baseturf, flags)
	set_light(0)
	return ..()

/turf/open/floor/light/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(!can_modify_colour)
		return
	if(!on)
		on = TRUE
		currentcolor = 1
		return
	else
		currentcolor++
	if(currentcolor > coloredlights.len)
		on = FALSE
	update_icon()

/turf/open/floor/light/attack_ai(mob/user)
	return attack_hand(user)

/turf/open/floor/light/attackby(obj/item/C, mob/user, params)
	if(..())
		return
	if(istype(C, /obj/item/light/bulb)) //only for light tiles
		if(state && user.temporarilyRemoveItemFromInventory(C))
			qdel(C)
			state = 0 //fixing it by bashing it with a light bulb, fun eh?
			update_icon()
			to_chat(user, "<span class='notice'>You replace the light bulb.</span>")
		else
			to_chat(user, "<span class='notice'>The light bulb seems fine, no need to replace it.</span>")


//Cycles through all of the colours
/turf/open/floor/light/colour_cycle
	coloredlights = list("cycle_all")
	can_modify_colour = FALSE



//Two different "dancefloor" types so that you can have a checkered pattern
// (also has a longer delay than colour_cycle between cycling colours)
/turf/open/floor/light/colour_cycle/dancefloor_a
	name = "dancefloor"
	desc = "Funky floor."
	coloredlights = list("dancefloor_A")

/turf/open/floor/light/colour_cycle/dancefloor_b
	name = "dancefloor"
	desc = "Funky floor."
	coloredlights = list("dancefloor_B")
