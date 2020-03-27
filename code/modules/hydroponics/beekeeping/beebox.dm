
#define BEEBOX_MAX_FRAMES				3		//Max frames per box
#define BEES_RATIO						0.5 	//Multiplied by the max number of honeycombs to find the max number of bees
#define BEE_PROB_NEW_BEE				20		//The chance for spare bee_resources to be turned into new bees
#define BEE_RESOURCE_HONEYCOMB_COST		100		//The amount of bee_resources for a new honeycomb to be produced, percentage cost 1-100
#define BEE_RESOURCE_NEW_BEE_COST		50		//The amount of bee_resources for a new bee to be produced, percentage cost 1-100



/mob/proc/bee_friendly()
	return 0


/mob/living/simple_animal/hostile/poison/bees/bee_friendly()
	return 1


/mob/living/carbon/human/bee_friendly()
	if(dna && dna.species && dna.species.id == "pod") //bees pollinate plants, duh.
		return 1
	if (wear_suit && head && istype(wear_suit, /obj/item/clothing) && istype(head, /obj/item/clothing))
		var/obj/item/clothing/CS = wear_suit
		var/obj/item/clothing/CH = head
		if (CS.clothing_flags & CH.clothing_flags & THICKMATERIAL)
			return 1
	return 0


/obj/structure/beebox
	name = "apiary"
	desc = "Dr. Miles Manners is just your average wasp-themed super hero by day, but by night he becomes DR. BEES!"
	icon = 'icons/obj/hydroponics/equipment.dmi'
	icon_state = "beebox"
	anchored = TRUE
	density = TRUE
	var/mob/living/simple_animal/hostile/poison/bees/queen/queen_bee = null
	var/list/bees = list() //bees owned by the box, not those inside it
	var/list/honeycombs = list()
	var/list/honey_frames = list()
	var/bee_resources = 0


/obj/structure/beebox/Initialize()
	. = ..()
	START_PROCESSING(SSobj, src)


/obj/structure/beebox/Destroy()
	STOP_PROCESSING(SSobj, src)
	bees.Cut()
	honeycombs.Cut()
	queen_bee = null
	return ..()


//Premade apiaries can spawn with a random reagent
/obj/structure/beebox/premade
	var/random_reagent = FALSE


/obj/structure/beebox/premade/Initialize()
	. = ..()

	icon_state = "beebox"
	var/datum/reagent/R = null
	if(random_reagent)
		R = pick(subtypesof(/datum/reagent))
		R = GLOB.chemical_reagents_list[R]

	queen_bee = new(src)
	queen_bee.beehome = src
	bees += queen_bee
	queen_bee.assign_reagent(R)

	for(var/i in 1 to BEEBOX_MAX_FRAMES)
		var/obj/item/honey_frame/HF = new(src)
		honey_frames += HF

	for(var/i in 1 to get_max_bees())
		var/mob/living/simple_animal/hostile/poison/bees/B = new(src)
		bees += B
		B.beehome = src
		B.assign_reagent(R)


/obj/structure/beebox/premade/random
	icon_state = "random_beebox"
	random_reagent = TRUE


/obj/structure/beebox/process()
	if(queen_bee)
		if(bee_resources >= BEE_RESOURCE_HONEYCOMB_COST)
			if(honeycombs.len < get_max_honeycomb())
				bee_resources = max(bee_resources-BEE_RESOURCE_HONEYCOMB_COST, 0)
				var/obj/item/reagent_containers/honeycomb/HC = new(src)
				if(queen_bee.beegent)
					HC.set_reagent(queen_bee.beegent.type)
				honeycombs += HC

		if(bees.len < get_max_bees())
			var/freebee = FALSE //a freebee, geddit?, hahaha HAHAHAHA
			if(bees.len <= 1) //there's always one set of worker bees, this isn't colony collapse disorder its 2d spessmen
				freebee = TRUE
			if((bee_resources >= BEE_RESOURCE_NEW_BEE_COST && prob(BEE_PROB_NEW_BEE)) || freebee)
				if(!freebee)
					bee_resources = max(bee_resources - BEE_RESOURCE_NEW_BEE_COST, 0)
				var/mob/living/simple_animal/hostile/poison/bees/B = new(get_turf(src))
				B.beehome = src
				B.assign_reagent(queen_bee.beegent)
				bees += B


/obj/structure/beebox/proc/get_max_honeycomb()
	. = 0
	for(var/hf in honey_frames)
		var/obj/item/honey_frame/HF = hf
		. += HF.honeycomb_capacity


/obj/structure/beebox/proc/get_max_bees()
	. = get_max_honeycomb() * BEES_RATIO


/obj/structure/beebox/examine(mob/user)
	. = ..()

	if(!queen_bee)
		. += "<span class='warning'>There is no queen bee! There won't bee any honeycomb without a queen!</span>"

	var/half_bee = get_max_bees()*0.5
	if(half_bee && (bees.len >= half_bee))
		. += "<span class='notice'>This place is aBUZZ with activity... there are lots of bees!</span>"

	. += "<span class='notice'>[bee_resources]/100 resource supply.</span>"
	. += "<span class='notice'>[bee_resources]% towards a new honeycomb.</span>"
	. += "<span class='notice'>[bee_resources*2]% towards a new bee.</span>"

	if(honeycombs.len)
		var/plural = honeycombs.len > 1
		. += "<span class='notice'>There [plural? "are" : "is"] [honeycombs.len] uncollected honeycomb[plural ? "s":""] in the apiary.</span>"

	if(honeycombs.len >= get_max_honeycomb())
		. += "<span class='warning'>There's no room for more honeycomb!</span>"


/obj/structure/beebox/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/honey_frame))
		var/obj/item/honey_frame/HF = I
		if(honey_frames.len < BEEBOX_MAX_FRAMES)
			visible_message("<span class='notice'>[user] adds a frame to the apiary.</span>")
			if(!user.transferItemToLoc(HF, src))
				return
			honey_frames += HF
		else
			to_chat(user, "<span class='warning'>There's no room for any more frames in the apiary!</span>")
		return

	if(I.tool_behaviour == TOOL_WRENCH)
		if(default_unfasten_wrench(user, I, time = 20))
			return

	if(istype(I, /obj/item/queen_bee))
		if(queen_bee)
			to_chat(user, "<span class='warning'>This hive already has a queen!</span>")
			return

		var/obj/item/queen_bee/qb = I
		user.temporarilyRemoveItemFromInventory(qb)

		qb.queen.forceMove(src)
		bees += qb.queen
		queen_bee = qb.queen
		qb.queen = null

		if(queen_bee)
			visible_message("<span class='notice'>[user] sets [qb] down inside the apiary, making it their new home.</span>")
			var/relocated = 0
			for(var/b in bees)
				var/mob/living/simple_animal/hostile/poison/bees/B = b
				if(B.reagent_incompatible(queen_bee))
					bees -= B
					B.beehome = null
					if(B.loc == src)
						B.forceMove(drop_location())
					relocated++
			if(relocated)
				to_chat(user, "<span class='warning'>This queen has a different reagent to some of the bees who live here, those bees will not return to this apiary!</span>")

		else
			to_chat(user, "<span class='warning'>The queen bee disappeared! Disappearing bees have been in the news lately...</span>")

		qdel(qb)
		return

	..()

/obj/structure/beebox/interact(mob/user)
	. = ..()
	if(!user.bee_friendly())
		//Time to get stung!
		var/bees = FALSE
		for(var/b in bees) //everyone who's ever lived here now instantly hates you, suck it assistant!
			var/mob/living/simple_animal/hostile/poison/bees/B = b
			if(B.isqueen)
				continue
			if(B.loc == src)
				B.forceMove(drop_location())
			B.target = user
			bees = TRUE
		if(bees)
			visible_message("<span class='danger'>[user] disturbs the bees!</span>")
		else
			visible_message("<span class='danger'>[user] disturbs the [name] to no effect!</span>")
	else
		var/option = alert(user, "What action do you wish to perform?","Apiary","Remove a Honey Frame","Remove the Queen Bee", "Cancel")
		if(!Adjacent(user))
			return
		switch(option)
			if("Remove a Honey Frame")
				if(!honey_frames.len)
					to_chat(user, "<span class='warning'>There are no honey frames to remove!</span>")
					return

				var/obj/item/honey_frame/HF = pick_n_take(honey_frames)
				if(HF)
					if(!user.put_in_active_hand(HF))
						HF.forceMove(drop_location())
					visible_message("<span class='notice'>[user] removes a frame from the apiary.</span>")

					var/amtH = HF.honeycomb_capacity
					var/fallen = 0
					while(honeycombs.len && amtH) //let's pretend you always grab the frame with the most honeycomb on it
						var/obj/item/reagent_containers/honeycomb/HC = pick_n_take(honeycombs)
						if(HC)
							HC.forceMove(drop_location())
							amtH--
							fallen++
					if(fallen)
						var/multiple = fallen > 1
						visible_message("<span class='notice'>[user] scrapes [multiple ? "[fallen]" : "a"] honeycomb[multiple ? "s" : ""] off of the frame.</span>")

			if("Remove the Queen Bee")
				if(!queen_bee || queen_bee.loc != src)
					to_chat(user, "<span class='warning'>There is no queen bee to remove!</span>")
					return
				var/obj/item/queen_bee/QB = new()
				queen_bee.forceMove(QB)
				bees -= queen_bee
				QB.queen = queen_bee
				QB.name = queen_bee.name
				if(!user.put_in_active_hand(QB))
					QB.forceMove(drop_location())
				visible_message("<span class='notice'>[user] removes the queen from the apiary.</span>")
				queen_bee = null

/obj/structure/beebox/deconstruct(disassembled = TRUE)
	new /obj/item/stack/sheet/mineral/wood (loc, 20)
	for(var/mob/living/simple_animal/hostile/poison/bees/B in bees)
		if(B.loc == src)
			B.forceMove(drop_location())
	for(var/obj/item/honey_frame/HF in honey_frames)
		if(HF.loc == src)
			HF.forceMove(drop_location())
	qdel(src)

/obj/structure/beebox/unwrenched
		anchored = FALSE
