


/*
field_generator power level display
   The icon used for the field_generator need to have 'num_power_levels' number of icon states
   named 'Field_Gen +p[num]' where 'num' ranges from 1 to 'num_power_levels'

   The power level is displayed using overlays. The current displayed power level is stored in 'powerlevel'.
   The overlay in use and the powerlevel variable must be kept in sync.  A powerlevel equal to 0 means that
   no power level overlay is currently in the overlays list.
   -Aygar
*/

#define field_generator_max_power 250

#define FG_OFFLINE 0
#define FG_CHARGING 1
#define FG_ONLINE 2

//field generator construction defines
#define FG_UNSECURED 0
#define FG_SECURED 1
#define FG_WELDED 2

/obj/machinery/field/generator
	name = "field generator"
	desc = "A large thermal battery that projects a high amount of energy when powered."
	icon = 'icons/obj/machines/field_generator.dmi'
	icon_state = "Field_Gen"
	anchored = FALSE
	density = TRUE
	use_power = NO_POWER_USE
	max_integrity = 500
	CanAtmosPass = ATMOS_PASS_YES
	//100% immune to lasers and energy projectiles since it absorbs their energy.
	armor = list("melee" = 25, "bullet" = 10, "laser" = 100, "energy" = 100, "bomb" = 0, "bio" = 0, "rad" = 0, "fire" = 50, "acid" = 70)
	var/const/num_power_levels = 6	// Total number of power level icon has
	var/power_level = 0
	var/active = FG_OFFLINE
	var/power = 20  // Current amount of power
	var/state = FG_UNSECURED
	var/warming_up = 0
	var/list/obj/machinery/field/containment/fields
	var/list/obj/machinery/field/generator/connected_gens
	var/clean_up = 0

/obj/machinery/field/generator/update_overlays()
	. = ..()
	if(warming_up)
		. += "+a[warming_up]"
	if(LAZYLEN(fields))
		. += "+on"
	if(power_level)
		. += "+p[power_level]"


/obj/machinery/field/generator/Initialize()
	. = ..()
	fields = list()
	connected_gens = list()

/obj/machinery/field/generator/ComponentInitialize()
	. = ..()
	AddComponent(/datum/component/empprotection, EMP_PROTECT_SELF | EMP_PROTECT_WIRES)

/obj/machinery/field/generator/process()
	if(active == FG_ONLINE)
		calc_power()

/obj/machinery/field/generator/interact(mob/user)
	if(state == FG_WELDED)
		if(get_dist(src, user) <= 1)//Need to actually touch the thing to turn it on
			if(active >= FG_CHARGING)
				to_chat(user, "<span class='warning'>You are unable to turn off [src] once it is online!</span>")
				return 1
			else
				user.visible_message("<span class='notice'>[user] turns on [src].</span>", \
					"<span class='notice'>You turn on [src].</span>", \
					"<span class='hear'>You hear heavy droning.</span>")
				turn_on()
				investigate_log("<font color='green'>activated</font> by [key_name(user)].", INVESTIGATE_SINGULO)

				add_fingerprint(user)
	else
		to_chat(user, "<span class='warning'>[src] needs to be firmly secured to the floor first!</span>")

/obj/machinery/field/generator/can_be_unfasten_wrench(mob/user, silent)
	if(active)
		if(!silent)
			to_chat(user, "<span class='warning'>Turn \the [src] off first!</span>")
		return FAILED_UNFASTEN

	else if(state == FG_WELDED)
		if(!silent)
			to_chat(user, "<span class='warning'>[src] is welded to the floor!</span>")
		return FAILED_UNFASTEN

	return ..()

/obj/machinery/field/generator/default_unfasten_wrench(mob/user, obj/item/I, time = 20)
	. = ..()
	if(. == SUCCESSFUL_UNFASTEN)
		if(anchored)
			state = FG_SECURED
		else
			state = FG_UNSECURED

/obj/machinery/field/generator/wrench_act(mob/living/user, obj/item/I)
	..()
	default_unfasten_wrench(user, I)
	return TRUE

/obj/machinery/field/generator/welder_act(mob/living/user, obj/item/I)
	. = ..()
	if(active)
		to_chat(user, "<span class='warning'>[src] needs to be off!</span>")
		return TRUE

	switch(state)
		if(FG_UNSECURED)
			to_chat(user, "<span class='warning'>[src] needs to be wrenched to the floor!</span>")

		if(FG_SECURED)
			if(!I.tool_start_check(user, amount=0))
				return TRUE
			user.visible_message("<span class='notice'>[user] starts to weld [src] to the floor.</span>", \
				"<span class='notice'>You start to weld \the [src] to the floor...</span>", \
				"<span class='hear'>You hear welding.</span>")
			if(I.use_tool(src, user, 20, volume=50) && state == FG_SECURED)
				state = FG_WELDED
				to_chat(user, "<span class='notice'>You weld the field generator to the floor.</span>")

		if(FG_WELDED)
			if(!I.tool_start_check(user, amount=0))
				return TRUE
			user.visible_message("<span class='notice'>[user] starts to cut [src] free from the floor.</span>", \
				"<span class='notice'>You start to cut \the [src] free from the floor...</span>", \
				"<span class='hear'>You hear welding.</span>")
			if(I.use_tool(src, user, 20, volume=50) && state == FG_WELDED)
				state = FG_SECURED
				to_chat(user, "<span class='notice'>You cut \the [src] free from the floor.</span>")

	return TRUE


/obj/machinery/field/generator/attack_animal(mob/living/simple_animal/M)
	if(M.environment_smash & ENVIRONMENT_SMASH_RWALLS && active == FG_OFFLINE && state != FG_UNSECURED)
		state = FG_UNSECURED
		anchored = FALSE
		M.visible_message("<span class='warning'>[M] rips [src] free from its moorings!</span>")
	else
		..()
	if(!anchored)
		step(src, get_dir(M, src))

/obj/machinery/field/generator/blob_act(obj/structure/blob/B)
	if(active)
		return 0
	else
		..()

/obj/machinery/field/generator/bullet_act(obj/projectile/Proj)
	if(Proj.flag != "bullet")
		power = min(power + Proj.damage, field_generator_max_power)
		check_power_level()
	. = ..()


/obj/machinery/field/generator/Destroy()
	cleanup()
	return ..()


/obj/machinery/field/generator/proc/check_power_level()
	var/new_level = round(num_power_levels * power / field_generator_max_power)
	if(new_level != power_level)
		power_level = new_level
		update_icon()

/obj/machinery/field/generator/proc/turn_off()
	active = FG_OFFLINE
	CanAtmosPass = ATMOS_PASS_YES
	air_update_turf(TRUE)
	INVOKE_ASYNC(src, .proc/cleanup)
	addtimer(CALLBACK(src, .proc/cool_down), 50)

/obj/machinery/field/generator/proc/cool_down()
	if(active || warming_up <= 0)
		return
	warming_up--
	update_icon()
	if(warming_up > 0)
		addtimer(CALLBACK(src, .proc/cool_down), 50)

/obj/machinery/field/generator/proc/turn_on()
	active = FG_CHARGING
	addtimer(CALLBACK(src, .proc/warm_up), 50)

/obj/machinery/field/generator/proc/warm_up()
	if(!active)
		return
	warming_up++
	update_icon()
	if(warming_up >= 3)
		start_fields()		
	else
		addtimer(CALLBACK(src, .proc/warm_up), 50)

/obj/machinery/field/generator/proc/calc_power(set_power_draw)
	var/power_draw = 2 + fields.len
	if(set_power_draw)
		power_draw = set_power_draw

	if(draw_power(round(power_draw/2,1)))
		check_power_level()
		return 1
	else
		visible_message("<span class='danger'>The [name] shuts down!</span>", "<span class='hear'>You hear something shutting down.</span>")
		turn_off()
		investigate_log("ran out of power and <font color='red'>deactivated</font>", INVESTIGATE_SINGULO)
		power = 0
		check_power_level()
		return 0

//This could likely be better, it tends to start loopin if you have a complex generator loop setup.  Still works well enough to run the engine fields will likely recode the field gens and fields sometime -Mport
/obj/machinery/field/generator/proc/draw_power(draw = 0, failsafe = FALSE, obj/machinery/field/generator/G = null, obj/machinery/field/generator/last = null)
	if((G && (G == src)) || (failsafe >= 8))//Loopin, set fail
		return 0
	else
		failsafe++

	if(power >= draw)//We have enough power
		power -= draw
		return 1

	else//Need more power
		draw -= power
		power = 0
		for(var/CG in connected_gens)
			var/obj/machinery/field/generator/FG = CG
			if(FG == last)//We just asked you
				continue
			if(G)//Another gen is askin for power and we dont have it
				if(FG.draw_power(draw,failsafe,G,src))//Can you take the load
					return 1
				else
					return 0
			else//We are askin another for power
				if(FG.draw_power(draw,failsafe,src,src))
					return 1
				else
					return 0


/obj/machinery/field/generator/proc/start_fields()
	if(state != FG_WELDED || !anchored)
		turn_off()
		return
	move_resist = INFINITY
	CanAtmosPass = ATMOS_PASS_NO
	air_update_turf(TRUE)
	addtimer(CALLBACK(src, .proc/setup_field, 1), 1)
	addtimer(CALLBACK(src, .proc/setup_field, 2), 2)
	addtimer(CALLBACK(src, .proc/setup_field, 4), 3)
	addtimer(CALLBACK(src, .proc/setup_field, 8), 4)
	addtimer(VARSET_CALLBACK(src, active, FG_ONLINE), 5)	

/obj/machinery/field/generator/proc/setup_field(NSEW)
	var/turf/T = loc
	if(!istype(T))
		return 0

	var/obj/machinery/field/generator/G = null
	var/steps = 0
	if(!NSEW)//Make sure its ran right
		return 0
	for(var/dist in 0 to 7) // checks out to 8 tiles away for another generator
		T = get_step(T, NSEW)
		if(T.density)//We cant shoot a field though this
			return 0

		G = locate(/obj/machinery/field/generator) in T
		if(G)
			steps -= 1
			if(!G.active)
				return 0
			break

		for(var/TC in T.contents)
			var/atom/A = TC
			if(ismob(A))
				continue
			if(A.density)
				return 0

		steps++

	if(!G)
		return 0

	T = loc
	for(var/dist in 0 to steps) // creates each field tile
		var/field_dir = get_dir(T,get_step(G.loc, NSEW))
		T = get_step(T, NSEW)
		if(!locate(/obj/machinery/field/containment) in T)
			var/obj/machinery/field/containment/CF = new(T)
			CF.set_master(src,G)
			CF.setDir(field_dir)
			fields += CF
			G.fields += CF
			for(var/mob/living/L in T)
				CF.Crossed(L)

	connected_gens |= G
	G.connected_gens |= src
	shield_floor(TRUE)
	update_icon()


/obj/machinery/field/generator/proc/cleanup()
	clean_up = 1
	for (var/F in fields)
		qdel(F)

	shield_floor(FALSE)

	for(var/CG in connected_gens)
		var/obj/machinery/field/generator/FG = CG
		FG.connected_gens -= src
		if(!FG.clean_up)//Makes the other gens clean up as well
			FG.cleanup()
		connected_gens -= FG
	clean_up = 0
	update_icon()

	//This is here to help fight the "hurr durr, release singulo cos nobody will notice before the
	//singulo eats the evidence". It's not fool-proof but better than nothing.
	//I want to avoid using global variables.
	INVOKE_ASYNC(src, .proc/notify_admins)

	move_resist = initial(move_resist)

/obj/machinery/field/generator/proc/shield_floor(create)
	if(connected_gens.len < 2)
		return
	var/CGcounter
	for(CGcounter = 1; CGcounter < connected_gens.len, CGcounter++)		
		 
		var/list/CGList = ((connected_gens[CGcounter].connected_gens & connected_gens[CGcounter+1].connected_gens)^src)
		if(!CGList.len)
			return
		var/obj/machinery/field/generator/CG = CGList[1]
		
		var/x_step
		var/y_step
		if(CG.x > x && CG.y > y)
			for(x_step=x; x_step <= CG.x; x_step++)
				for(y_step=y; y_step <= CG.y; y_step++)
					place_floor(locate(x_step,y_step,z),create)
		else if(CG.x > x && CG.y < y)
			for(x_step=x; x_step <= CG.x; x_step++)
				for(y_step=y; y_step >= CG.y; y_step--)
					place_floor(locate(x_step,y_step,z),create)
		else if(CG.x < x && CG.y > y)
			for(x_step=x; x_step >= CG.x; x_step--)
				for(y_step=y; y_step <= CG.y; y_step++)
					place_floor(locate(x_step,y_step,z),create)
		else
			for(x_step=x; x_step >= CG.x; x_step--)
				for(y_step=y; y_step >= CG.y; y_step--)
					place_floor(locate(x_step,y_step,z),create)
					

/obj/machinery/field/generator/proc/place_floor(Location,create)
	if(create && !locate(/obj/effect/shield) in Location)
		new/obj/effect/shield(Location)
	else if(!create)		
		var/obj/effect/shield/S=locate(/obj/effect/shield) in Location
		if(S)			
			qdel(S)

/obj/machinery/field/generator/proc/notify_admins()
	var/temp = TRUE //stops spam
	for(var/obj/singularity/O in GLOB.singularities)
		if(O.last_warning && temp)
			if((world.time - O.last_warning) > 50) //to stop message-spam
				temp = FALSE
				var/turf/T = get_turf(src)
				message_admins("A singulo exists and a containment field has failed at [ADMIN_VERBOSEJMP(T)].")
				investigate_log("has <font color='red'>failed</font> whilst a singulo exists at [AREACOORD(T)].", INVESTIGATE_SINGULO)
				notify_ghosts("IT'S LOOSE", source = src, action = NOTIFY_ORBIT, flashwindow = FALSE, ghost_sound = 'sound/machines/warning-buzzer.ogg', header = "IT'S LOOSE", notify_volume = 75)
		O.last_warning = world.time

/obj/machinery/field/generator/shock(mob/living/user)
	if(fields.len)
		..()

/obj/machinery/field/generator/bump_field(atom/movable/AM as mob|obj)
	if(fields.len)
		..()

#undef FG_UNSECURED
#undef FG_SECURED
#undef FG_WELDED

#undef FG_OFFLINE
#undef FG_CHARGING
#undef FG_ONLINE
