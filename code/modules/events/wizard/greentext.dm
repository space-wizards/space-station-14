/datum/round_event_control/wizard/greentext //Gotta have it!
	name = "Greentext"
	weight = 4
	typepath = /datum/round_event/wizard/greentext
	max_occurrences = 1
	earliest_start = 0 MINUTES

/datum/round_event/wizard/greentext/start()

	var/list/holder_canadates = GLOB.player_list.Copy()
	for(var/mob/M in holder_canadates)
		if(!ishuman(M))
			holder_canadates -= M
	if(!holder_canadates) //Very unlikely, but just in case
		return 0

	var/mob/living/carbon/human/H = pick(holder_canadates)
	new /obj/item/greentext(H.loc)
	to_chat(H, "<font color='green'>The mythical greentext appear at your feet! Pick it up if you dare...</font>")


/obj/item/greentext
	name = "greentext"
	desc = "No one knows what this massive tome does, but it feels <i><font color='green'>desirable</font></i> all the same..."
	w_class = WEIGHT_CLASS_BULKY
	icon = 'icons/obj/wizard.dmi'
	icon_state = "greentext"
	var/mob/living/last_holder
	var/mob/living/new_holder
	var/list/color_altered_mobs = list()
	var/datum/callback/roundend_callback
	resistance_flags = FIRE_PROOF | ACID_PROOF
	var/quiet = FALSE

/obj/item/greentext/Initialize(mapload)
	. = ..()
	GLOB.poi_list |= src
	roundend_callback = CALLBACK(src,.proc/check_winner)
	SSticker.OnRoundend(roundend_callback)

/obj/item/greentext/equipped(mob/living/user as mob)
	to_chat(user, "<font color='green'>So long as you leave this place with greentext in hand you know will be happy...</font>")
	var/list/other_objectives = user.mind.get_all_objectives()
	if(user.mind && other_objectives.len > 0)
		to_chat(user, "<span class='warning'>... so long as you still perform your other objectives that is!</span>")
	new_holder = user
	if(!last_holder)
		last_holder = user
	if(!(user in color_altered_mobs))
		color_altered_mobs += user
	user.add_atom_colour("#00FF00", ADMIN_COLOUR_PRIORITY)
	START_PROCESSING(SSobj, src)
	..()

/obj/item/greentext/dropped(mob/living/user as mob)
	if(user in color_altered_mobs)
		to_chat(user, "<span class='warning'>A sudden wave of failure washes over you...</span>")
		user.add_atom_colour("#FF0000", ADMIN_COLOUR_PRIORITY) //ya blew it
	last_holder 	= null
	new_holder 		= null
	STOP_PROCESSING(SSobj, src)
	..()

/obj/item/greentext/proc/check_winner()
	if(!new_holder)
		return

	if(is_centcom_level(new_holder.z))//you're winner!
		to_chat(new_holder, "<font color='green'>At last it feels like victory is assured!</font>")
		new_holder.mind.add_antag_datum(/datum/antagonist/greentext)
		new_holder.log_message("won with greentext!!!", LOG_ATTACK, color="green")
		color_altered_mobs -= new_holder
		resistance_flags |= ON_FIRE
		qdel(src)

/obj/item/greentext/process()
	if(last_holder && last_holder != new_holder) //Somehow it was swiped without ever getting dropped
		to_chat(last_holder, "<span class='warning'>A sudden wave of failure washes over you...</span>")
		last_holder.add_atom_colour("#FF0000", ADMIN_COLOUR_PRIORITY)
		last_holder = new_holder //long live the king

/obj/item/greentext/Destroy(force)
	if(!(resistance_flags & ON_FIRE) && !force)
		return QDEL_HINT_LETMELIVE

	SSticker.round_end_events -= roundend_callback
	GLOB.poi_list.Remove(src)
	for(var/i in GLOB.player_list)
		var/mob/M = i
		var/message = "<span class='warning'>A dark temptation has passed from this world"
		if(M in color_altered_mobs)
			message += " and you're finally able to forgive yourself"
			if(M.color == "#FF0000" || M.color == "#00FF00")
				M.remove_atom_colour(ADMIN_COLOUR_PRIORITY)
		message += "...</span>"
		// can't skip the mob check as it also does the decolouring
		if(!quiet)
			to_chat(M, message)
	. = ..()

/obj/item/greentext/quiet
	quiet = TRUE
