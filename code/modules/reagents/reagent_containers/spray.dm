/obj/item/reagent_containers/spray
	name = "spray bottle"
	desc = "A spray bottle, with an unscrewable top."
	icon = 'icons/obj/janitor.dmi'
	icon_state = "sprayer_large"
	item_state = "cleaner"
	lefthand_file = 'icons/mob/inhands/equipment/custodial_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/custodial_righthand.dmi'
	item_flags = NOBLUDGEON
	reagent_flags = OPENCONTAINER
	slot_flags = ITEM_SLOT_BELT
	throwforce = 0
	w_class = WEIGHT_CLASS_SMALL
	throw_speed = 3
	throw_range = 7
	var/stream_mode = 0 //whether we use the more focused mode
	var/current_range = 3 //the range of tiles the sprayer will reach.
	var/spray_range = 3 //the range of tiles the sprayer will reach when in spray mode.
	var/stream_range = 1 //the range of tiles the sprayer will reach when in stream mode.
	var/stream_amount = 10 //the amount of reagents transfered when in stream mode.
	var/can_fill_from_container = TRUE
	amount_per_transfer_from_this = 5
	volume = 250
	possible_transfer_amounts = list(5,10,15,20,25,30,50,100)

/obj/item/reagent_containers/spray/afterattack(atom/A, mob/user)
	. = ..()
	if(istype(A, /obj/structure/sink) || istype(A, /obj/structure/janitorialcart) || istype(A, /obj/machinery/hydroponics))
		return

	if((A.is_drainable() && !A.is_refillable()) && get_dist(src,A) <= 1 && can_fill_from_container)
		if(!A.reagents.total_volume)
			to_chat(user, "<span class='warning'>[A] is empty.</span>")
			return

		if(reagents.holder_full())
			to_chat(user, "<span class='warning'>[src] is full.</span>")
			return

		var/trans = A.reagents.trans_to(src, 50, transfered_by = user) //transfer 50u , using the spray's transfer amount would take too long to refill
		to_chat(user, "<span class='notice'>You fill \the [src] with [trans] units of the contents of \the [A].</span>")
		return

	if(reagents.total_volume < amount_per_transfer_from_this)
		to_chat(user, "<span class='warning'>Not enough left!</span>")
		return

	spray(A, user)

	playsound(src.loc, 'sound/effects/spray2.ogg', 50, TRUE, -6)
	user.changeNext_move(CLICK_CD_RANGE*2)
	user.newtonian_move(get_dir(A, user))

	var/turf/T = get_turf(src)
	var/contained = reagents.log_list()

	log_combat(user, T, "sprayed", src, addition="which had [contained]")
	log_game("[key_name(user)] fired [contained] from \a [src] at [AREACOORD(T)].") //copypasta falling out of my pockets
	return


/obj/item/reagent_containers/spray/proc/spray(atom/A, mob/user)
	var/range = max(min(current_range, get_dist(src, A)), 1)
	var/obj/effect/decal/chempuff/D = new /obj/effect/decal/chempuff(get_turf(src))
	D.create_reagents(amount_per_transfer_from_this)
	var/puff_reagent_left = range //how many turf, mob or dense objet we can react with before we consider the chem puff consumed
	if(stream_mode)
		reagents.trans_to(D, amount_per_transfer_from_this)
		puff_reagent_left = 1
	else
		reagents.trans_to(D, amount_per_transfer_from_this, 1/range)
	D.color = mix_color_from_reagents(D.reagents.reagent_list)
	var/wait_step = max(round(2+3/range), 2)
	do_spray(A, wait_step, D, range, puff_reagent_left, user)

/obj/item/reagent_containers/spray/proc/do_spray(atom/A, wait_step, obj/effect/decal/chempuff/D, range, puff_reagent_left, mob/user)
	set waitfor = FALSE
	var/range_left = range
	for(var/i=0, i<range, i++)
		range_left--
		step_towards(D,A)
		sleep(wait_step)

		for(var/atom/T in get_turf(D))
			if(T == D || T.invisibility) //we ignore the puff itself and stuff below the floor
				continue
			if(puff_reagent_left <= 0)
				break

			if(stream_mode)
				if(isliving(T))
					var/mob/living/M = T
					if(!M.can_inject())
						continue
					if((M.mobility_flags & MOBILITY_STAND) || !range_left)
						D.reagents.reaction(M, VAPOR)
						puff_reagent_left -= 1
						var/contained = D.reagents.log_list() // looks like more copypasta but now the reagents are in a different place fuck you old coder
						log_combat(user, M,  "sprayed with", src, addition="which had [contained]")
				else if(!range_left)
					D.reagents.reaction(T, VAPOR)
			else
				D.reagents.reaction(T, VAPOR)
				if(ismob(T))
					puff_reagent_left -= 1

		if(puff_reagent_left > 0 && (!stream_mode || !range_left))
			D.reagents.reaction(get_turf(D), VAPOR)
			puff_reagent_left -= 1

		if(puff_reagent_left <= 0) // we used all the puff so we delete it.
			qdel(D)
			return
	qdel(D)

/obj/item/reagent_containers/spray/attack_self(mob/user)
	stream_mode = !stream_mode
	if(stream_mode)
		amount_per_transfer_from_this = stream_amount
		current_range = stream_range
	else
		amount_per_transfer_from_this = initial(amount_per_transfer_from_this)
		current_range = spray_range
	to_chat(user, "<span class='notice'>You switch the nozzle setting to [stream_mode ? "\"stream\"":"\"spray\""]. You'll now use [amount_per_transfer_from_this] units per use.</span>")

/obj/item/reagent_containers/spray/attackby(obj/item/I, mob/user, params)
	var/hotness = I.get_temperature()
	if(hotness && reagents)
		reagents.expose_temperature(hotness)
		to_chat(user, "<span class='notice'>You heat [name] with [I]!</span>")
	return ..()

/obj/item/reagent_containers/spray/verb/empty()
	set name = "Empty Spray Bottle"
	set category = "Object"
	set src in usr
	if(usr.incapacitated())
		return
	if (alert(usr, "Are you sure you want to empty that?", "Empty Bottle:", "Yes", "No") != "Yes")
		return
	if(isturf(usr.loc) && src.loc == usr)
		to_chat(usr, "<span class='notice'>You empty \the [src] onto the floor.</span>")
		reagents.reaction(usr.loc)
		src.reagents.clear_reagents()

/obj/item/reagent_containers/spray/on_reagent_change(changetype)
	var/total_reagent_weight
	var/amount_of_reagents
	for (var/datum/reagent/R in reagents.reagent_list)
		total_reagent_weight = total_reagent_weight + R.reagent_weight
		amount_of_reagents++

	if(total_reagent_weight && amount_of_reagents) //don't bother if the container is empty - DIV/0
		var/average_reagent_weight = total_reagent_weight / amount_of_reagents
		spray_range = CLAMP(round((initial(spray_range) / average_reagent_weight) - ((amount_of_reagents - 1) * 1)), 3, 5) //spray distance between 3 and 5 tiles rounded down; extra reagents lose a tile
	else
		spray_range = initial(spray_range)
	if(stream_mode == 0)
		current_range = spray_range

//space cleaner
/obj/item/reagent_containers/spray/cleaner
	name = "space cleaner"
	desc = "BLAM!-brand non-foaming space cleaner!"
	icon_state = "cleaner"
	volume = 100
	list_reagents = list(/datum/reagent/space_cleaner = 100)
	amount_per_transfer_from_this = 2
	stream_amount = 5

/obj/item/reagent_containers/spray/cleaner/suicide_act(mob/user)
	user.visible_message("<span class='suicide'>[user] is putting the nozzle of \the [src] in [user.p_their()] mouth. It looks like [user.p_theyre()] trying to commit suicide!</span>")
	if(do_mob(user,user,30))
		if(reagents.total_volume >= amount_per_transfer_from_this)//if not empty
			user.visible_message("<span class='suicide'>[user] pulls the trigger!</span>")
			src.spray(user)
			return BRUTELOSS
		else
			user.visible_message("<span class='suicide'>[user] pulls the trigger...but \the [src] is empty!</span>")
			return SHAME
	else
		user.visible_message("<span class='suicide'>[user] decided life was worth living.</span>")
		return

//spray tan
/obj/item/reagent_containers/spray/spraytan
	name = "spray tan"
	volume = 50
	desc = "Gyaro brand spray tan. Do not spray near eyes or other orifices."
	list_reagents = list(/datum/reagent/spraytan = 50)


//pepperspray
/obj/item/reagent_containers/spray/pepper
	name = "pepperspray"
	desc = "Manufactured by UhangInc, used to blind and down an opponent quickly."
	icon = 'icons/obj/items_and_weapons.dmi'
	icon_state = "pepperspray"
	item_state = "pepperspray"
	lefthand_file = 'icons/mob/inhands/equipment/security_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/security_righthand.dmi'
	volume = 50
	stream_range = 4
	amount_per_transfer_from_this = 5
	list_reagents = list(/datum/reagent/consumable/condensedcapsaicin = 50)

/obj/item/reagent_containers/spray/pepper/empty //for protolathe printing
	list_reagents = null

/obj/item/reagent_containers/spray/pepper/suicide_act(mob/living/carbon/user)
	user.visible_message("<span class='suicide'>[user] begins huffing \the [src]! It looks like [user.p_theyre()] getting a dirty high!</span>")
	return OXYLOSS

// Fix pepperspraying yourself
/obj/item/reagent_containers/spray/pepper/afterattack(atom/A as mob|obj, mob/user)
	if (A.loc == user)
		return
	. = ..()

//water flower
/obj/item/reagent_containers/spray/waterflower
	name = "water flower"
	desc = "A seemingly innocent sunflower...with a twist."
	icon = 'icons/obj/hydroponics/harvest.dmi'
	icon_state = "sunflower"
	item_state = "sunflower"
	amount_per_transfer_from_this = 1
	volume = 10
	list_reagents = list(/datum/reagent/water = 10)

/obj/item/reagent_containers/spray/waterflower/attack_self(mob/user) //Don't allow changing how much the flower sprays
	return

///Subtype used for the lavaland clown ruin.
/obj/item/reagent_containers/spray/waterflower/superlube
	name = "clown flower"
	desc = "A delightly devilish flower... you got a feeling where this is going."
	icon = 'icons/obj/chemical.dmi'
	icon_state = "clownflower"
	volume = 30
	list_reagents = list(/datum/reagent/lube/superlube = 30)

/obj/item/reagent_containers/spray/waterflower/cyborg
	reagent_flags = NONE
	volume = 100
	list_reagents = list(/datum/reagent/water = 100)
	var/generate_amount = 5
	var/generate_type = /datum/reagent/water
	var/last_generate = 0
	var/generate_delay = 10	//deciseconds
	can_fill_from_container = FALSE

/obj/item/reagent_containers/spray/waterflower/cyborg/hacked
	name = "nova flower"
	desc = "This doesn't look safe at all..."
	list_reagents = list(/datum/reagent/clf3 = 3)
	volume = 3
	generate_type = /datum/reagent/clf3
	generate_amount = 1
	generate_delay = 40		//deciseconds

/obj/item/reagent_containers/spray/waterflower/cyborg/Initialize()
	. = ..()
	START_PROCESSING(SSfastprocess, src)

/obj/item/reagent_containers/spray/waterflower/cyborg/Destroy()
	STOP_PROCESSING(SSfastprocess, src)
	return ..()

/obj/item/reagent_containers/spray/waterflower/cyborg/process()
	if(world.time < last_generate + generate_delay)
		return
	last_generate = world.time
	generate_reagents()

/obj/item/reagent_containers/spray/waterflower/cyborg/empty()
	to_chat(usr, "<span class='warning'>You can not empty this!</span>")
	return

/obj/item/reagent_containers/spray/waterflower/cyborg/proc/generate_reagents()
	reagents.add_reagent(generate_type, generate_amount)

//chemsprayer
/obj/item/reagent_containers/spray/chemsprayer
	name = "chem sprayer"
	desc = "A utility used to spray large amounts of reagents in a given area."
	icon = 'icons/obj/guns/projectile.dmi'
	icon_state = "chemsprayer"
	item_state = "chemsprayer"
	lefthand_file = 'icons/mob/inhands/weapons/guns_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/weapons/guns_righthand.dmi'
	throwforce = 0
	w_class = WEIGHT_CLASS_NORMAL
	stream_mode = 1
	current_range = 7
	spray_range = 4
	stream_range = 7
	amount_per_transfer_from_this = 10
	volume = 600

/obj/item/reagent_containers/spray/chemsprayer/afterattack(atom/A as mob|obj, mob/user)
	// Make it so the bioterror spray doesn't spray yourself when you click your inventory items
	if (A.loc == user)
		return
	. = ..()

/obj/item/reagent_containers/spray/chemsprayer/spray(atom/A, mob/user)
	var/direction = get_dir(src, A)
	var/turf/T = get_turf(A)
	var/turf/T1 = get_step(T,turn(direction, 90))
	var/turf/T2 = get_step(T,turn(direction, -90))
	var/list/the_targets = list(T,T1,T2)

	for(var/i=1, i<=3, i++) // intialize sprays
		if(reagents.total_volume < 1)
			return
		..(the_targets[i], user)

/obj/item/reagent_containers/spray/chemsprayer/bioterror
	list_reagents = list(/datum/reagent/toxin/sodium_thiopental = 100, /datum/reagent/toxin/coniine = 100, /datum/reagent/toxin/venom = 100, /datum/reagent/consumable/condensedcapsaicin = 100, /datum/reagent/toxin/initropidril = 100, /datum/reagent/toxin/polonium = 100)


/obj/item/reagent_containers/spray/chemsprayer/janitor
	name = "janitor chem sprayer"
	desc = "A utility used to spray large amounts of cleaning reagents in a given area. It regenerates space cleaner by itself but it's unable to be fueled by normal means."
	icon_state = "chemsprayer_janitor"
	item_state = "chemsprayer_janitor"
	lefthand_file = 'icons/mob/inhands/weapons/guns_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/weapons/guns_righthand.dmi'
	reagent_flags = NONE
	list_reagents = list(/datum/reagent/space_cleaner = 1000)
	volume = 1000
	amount_per_transfer_from_this = 5
	var/generate_amount = 50
	var/generate_type = /datum/reagent/space_cleaner
	var/last_generate = 0
	var/generate_delay = 10	//deciseconds

/obj/item/reagent_containers/spray/chemsprayer/janitor/Initialize()
	. = ..()
	START_PROCESSING(SSfastprocess, src)

/obj/item/reagent_containers/spray/chemsprayer/janitor/Destroy()
	STOP_PROCESSING(SSfastprocess, src)
	return ..()

/obj/item/reagent_containers/spray/chemsprayer/janitor/process()
	if(world.time < last_generate + generate_delay)
		return
	last_generate = world.time
	reagents.add_reagent(generate_type, generate_amount)

// Plant-B-Gone
/obj/item/reagent_containers/spray/plantbgone // -- Skie
	name = "Plant-B-Gone"
	desc = "Kills those pesky weeds!"
	icon = 'icons/obj/hydroponics/equipment.dmi'
	icon_state = "plantbgone"
	item_state = "plantbgone"
	lefthand_file = 'icons/mob/inhands/equipment/hydroponics_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/hydroponics_righthand.dmi'
	volume = 100
	list_reagents = list(/datum/reagent/toxin/plantbgone = 100)

/obj/item/reagent_containers/spray/syndicate
	name = "suspicious spray bottle"
	desc = "A spray bottle, with a high performance plastic nozzle. The color scheme makes you feel slightly uneasy."
	icon = 'icons/obj/chemical.dmi'
	icon_state = "sprayer_sus_8"
	item_state = "sprayer_sus"
	lefthand_file = 'icons/mob/inhands/equipment/medical_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/medical_righthand.dmi'
	spray_range = 4
	stream_range = 2
	volume = 100
	custom_premium_price = 900

/obj/item/reagent_containers/spray/syndicate/Initialize()
	. = ..()
	icon_state = pick("sprayer_sus_1", "sprayer_sus_2", "sprayer_sus_3", "sprayer_sus_4", "sprayer_sus_5","sprayer_sus_6", "sprayer_sus_7", "sprayer_sus_8")

/obj/item/reagent_containers/spray/medical
	name = "medical spray bottle"
	icon = 'icons/obj/chemical.dmi'
	icon_state = "sprayer_med_red"
	item_state = "sprayer_med_red"
	lefthand_file = 'icons/mob/inhands/equipment/medical_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/medical_righthand.dmi'
	volume = 100
	unique_reskin = list("Red" = "sprayer_med_red",
						"Yellow" = "sprayer_med_yellow",
						"Blue" = "sprayer_med_blue")

/obj/item/reagent_containers/spray/medical/AltClick(mob/user)
	if(unique_reskin && !current_skin && user.canUseTopic(src, BE_CLOSE, NO_DEXTERITY))
		reskin_obj(user)

/obj/item/reagent_containers/spray/medical/reskin_obj(mob/M)
	..()
	switch(icon_state)
		if("sprayer_med_red")
			item_state = "sprayer_med_red"
		if("sprayer_med_yellow")
			item_state = "sprayer_med_yellow"
		if("sprayer_med_blue")
			item_state = "sprayer_med_blue"
	M.update_inv_hands()

/obj/item/reagent_containers/spray/rhigoxane
	name = "medical spray (rhigoxane)"
	desc = "A medical spray bottle.This one contains rhigoxane, it is used to treat burns and cool down temperature if applied with spray."
	icon_state = "sprayer_large"
	list_reagents = list(/datum/reagent/medicine/rhigoxane = 100)
