//temporary visual effects(/obj/effect/temp_visual) used by cult stuff
/obj/effect/temp_visual/cult
	icon = 'icons/effects/cult_effects.dmi'
	randomdir = 0
	duration = 10

/obj/effect/temp_visual/cult/sparks
	randomdir = 1
	name = "blood sparks"
	icon_state = "bloodsparkles"

/obj/effect/temp_visual/cult/blood  // The traditional teleport
	name = "blood jaunt"
	duration = 12
	icon_state = "bloodin"

/obj/effect/temp_visual/cult/blood/out
	icon_state = "bloodout"

/obj/effect/temp_visual/dir_setting/cult/phase  // The veil shifter teleport
	icon = 'icons/effects/cult_effects.dmi'
	name = "phase glow"
	duration = 7
	icon_state = "cultin"

/obj/effect/temp_visual/dir_setting/cult/phase/out
	icon = 'icons/effects/cult_effects.dmi'
	icon_state = "cultout"

/obj/effect/temp_visual/cult/sac
	name = "maw of Nar'Sie"
	icon_state = "sacconsume"

/obj/effect/temp_visual/cult/door
	name = "unholy glow"
	icon_state = "doorglow"
	layer = CLOSED_FIREDOOR_LAYER //above closed doors

/obj/effect/temp_visual/cult/door/unruned
	icon_state = "unruneddoorglow"

/obj/effect/temp_visual/cult/turf
	name = "unholy glow"
	icon_state = "wallglow"
	layer = ABOVE_NORMAL_TURF_LAYER

/obj/effect/temp_visual/cult/turf/floor
	icon_state = "floorglow"
	duration = 5
	plane = FLOOR_PLANE

/obj/effect/temp_visual/cult/portal
	icon_state = "space"
	duration = 600
	layer = ABOVE_OBJ_LAYER

//visuals for runes being magically created
/obj/effect/temp_visual/cult/rune_spawn
	icon_state = "runeouter"
	alpha = 0
	var/turnedness = 179 //179 turns counterclockwise, 181 turns clockwise

/obj/effect/temp_visual/cult/rune_spawn/Initialize(mapload, set_duration, set_color)
	if(isnum(set_duration))
		duration = set_duration
	if(set_color)
		add_atom_colour(set_color, FIXED_COLOUR_PRIORITY)
	. = ..()
	var/oldtransform = transform
	transform = matrix()*2
	var/matrix/M = transform
	M.Turn(turnedness)
	transform = M
	animate(src, alpha = 255, time = duration, easing = BOUNCE_EASING, flags = ANIMATION_PARALLEL)
	animate(src, transform = oldtransform, time = duration, flags = ANIMATION_PARALLEL)

/obj/effect/temp_visual/cult/rune_spawn/rune1
	icon_state = "rune1words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune1/inner
	icon_state = "rune1inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune1/center
	icon_state = "rune1center"

/obj/effect/temp_visual/cult/rune_spawn/rune2
	icon_state = "rune2words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune2/inner
	icon_state = "rune2inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune2/center
	icon_state = "rune2center"

/obj/effect/temp_visual/cult/rune_spawn/rune3
	icon_state = "rune3words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune3/inner
	icon_state = "rune3inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune3/center
	icon_state = "rune3center"

/obj/effect/temp_visual/cult/rune_spawn/rune4
	icon_state = "rune4words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune4/inner
	icon_state = "rune4inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune4/center
	icon_state = "rune4center"

/obj/effect/temp_visual/cult/rune_spawn/rune5
	icon_state = "rune5words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune5/inner
	icon_state = "rune5inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune5/center
	icon_state = "rune5center"

/obj/effect/temp_visual/cult/rune_spawn/rune6
	icon_state = "rune6words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune6/inner
	icon_state = "rune6inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune6/center
	icon_state = "rune6center"

/obj/effect/temp_visual/cult/rune_spawn/rune7
	icon_state = "rune7words"
	turnedness = 181

/obj/effect/temp_visual/cult/rune_spawn/rune7/inner
	icon_state = "rune7inner"
	turnedness = 179

/obj/effect/temp_visual/cult/rune_spawn/rune7/center
	icon_state = "rune7center"
