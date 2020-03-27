/*
A mob of type /mob/camera/disease is an overmind coordinating at least one instance of /datum/disease/advance/sentient_disease
that has infected a host. All instances in a host will be synchronized with the stats of the overmind's disease_template. Any
samples outside of a host will retain the stats they had when they left the host, but infecting a new host will cause
the new instance inside the host to be updated to the template's stats.
*/

/mob/camera/disease
	name = "Sentient Disease"
	real_name = "Sentient Disease"
	desc = ""
	icon = 'icons/mob/cameramob.dmi'
	icon_state = "marker"
	mouse_opacity = MOUSE_OPACITY_ICON
	move_on_shuttle = FALSE
	see_in_dark = 8
	invisibility = INVISIBILITY_OBSERVER
	layer = BELOW_MOB_LAYER
	lighting_alpha = LIGHTING_PLANE_ALPHA_MOSTLY_INVISIBLE
	sight = SEE_SELF|SEE_THRU
	initial_language_holder = /datum/language_holder/universal

	var/freemove = TRUE
	var/freemove_end = 0
	var/const/freemove_time = 1200
	var/freemove_end_timerid

	var/datum/action/innate/disease_adapt/adaptation_menu_action
	var/datum/disease_ability/examining_ability
	var/datum/browser/browser
	var/browser_open = FALSE

	var/mob/living/following_host
	var/list/disease_instances
	var/list/hosts //this list is associative, affected_mob -> disease_instance
	var/datum/disease/advance/sentient_disease/disease_template

	var/total_points = 0
	var/points = 0

	var/last_move_tick = 0
	var/move_delay = 1

	var/next_adaptation_time = 0
	var/adaptation_cooldown = 600

	var/list/purchased_abilities
	var/list/unpurchased_abilities

/mob/camera/disease/Initialize(mapload)
	.= ..()

	disease_instances = list()
	hosts = list()

	purchased_abilities = list()
	unpurchased_abilities = list()

	disease_template = new /datum/disease/advance/sentient_disease()
	disease_template.overmind = src
	qdel(SSdisease.archive_diseases[disease_template.GetDiseaseID()])
	SSdisease.archive_diseases[disease_template.GetDiseaseID()] = disease_template //important for stuff that uses disease IDs

	var/datum/atom_hud/my_hud = GLOB.huds[DATA_HUD_SENTIENT_DISEASE]
	my_hud.add_hud_to(src)

	browser = new /datum/browser(src, "disease_menu", "Adaptation Menu", 1000, 770, src)

	freemove_end = world.time + freemove_time
	freemove_end_timerid = addtimer(CALLBACK(src, .proc/infect_random_patient_zero), freemove_time, TIMER_STOPPABLE)

/mob/camera/disease/Destroy()
	. = ..()
	QDEL_NULL(adaptation_menu_action)
	for(var/V in GLOB.sentient_disease_instances)
		var/datum/disease/advance/sentient_disease/S = V
		if(S.overmind == src)
			S.overmind = null

/mob/camera/disease/Login()
	..()
	if(freemove)
		to_chat(src, "<span class='warning'>You have [DisplayTimeText(freemove_end - world.time)] to select your first host. Click on a human to select your host.</span>")


/mob/camera/disease/Stat()
	..()
	if(statpanel("Status"))
		if(freemove)
			stat("Host Selection Time: [round((freemove_end - world.time)/10)]s")
		else
			stat("Adaptation Points: [points]/[total_points]")
			stat("Hosts: [disease_instances.len]")
			var/adapt_ready = next_adaptation_time - world.time
			if(adapt_ready > 0)
				stat("Adaptation Ready: [round(adapt_ready/10, 0.1)]s")


/mob/camera/disease/examine(mob/user)
	. = ..()
	if(isobserver(user))
		. += {"<span class='notice'>[src] has [points]/[total_points] adaptation points.</span>
		<span class='notice'>[src] has the following unlocked:</span>"}
		for(var/datum/disease_ability/ability in purchased_abilities)
			. += "<span class='notice'>[ability.name]</span>"

/mob/camera/disease/say(message, bubble_type, list/spans = list(), sanitize = TRUE, datum/language/language = null, ignore_spam = FALSE, forced = null)
	return

/mob/camera/disease/Move(NewLoc, Dir = 0)
	if(freemove)
		forceMove(NewLoc)
	else
		if(world.time > (last_move_tick + move_delay))
			follow_next(Dir & NORTHWEST)
			last_move_tick = world.time

/mob/camera/disease/Hear(message, atom/movable/speaker, message_language, raw_message, radio_freq, list/spans, message_mode)
	. = ..()
	var/atom/movable/to_follow = speaker
	if(radio_freq)
		var/atom/movable/virtualspeaker/V = speaker
		to_follow = V.source
	var/link
	if(to_follow in hosts)
		link = FOLLOW_LINK(src, to_follow)
	else
		link = ""
	// Recompose the message, because it's scrambled by default
	message = compose_message(speaker, message_language, raw_message, radio_freq, spans, message_mode)
	to_chat(src, "[link] [message]")


/mob/camera/disease/mind_initialize()
	. = ..()
	if(!mind.has_antag_datum(/datum/antagonist/disease))
		mind.add_antag_datum(/datum/antagonist/disease)
	var/datum/atom_hud/medsensor = GLOB.huds[DATA_HUD_MEDICAL_ADVANCED]
	medsensor.add_hud_to(src)

/mob/camera/disease/proc/pick_name()
	var/static/list/taken_names
	if(!taken_names)
		taken_names = list("Unknown" = TRUE)
		for(var/T in (subtypesof(/datum/disease) - /datum/disease/advance))
			var/datum/disease/D = T
			taken_names[initial(D.name)] = TRUE
	var/set_name
	while(!set_name)
		var/input = sanitize_name(stripped_input(src, "Select a name for your disease", "Select Name", "", MAX_NAME_LEN))
		if(!input)
			set_name = "Sentient Virus"
			break
		if(taken_names[input])
			to_chat(src, "<span class='warning'>You cannot use the name of such a well-known disease!</span>")
		else
			set_name = input
	real_name = "[set_name] (Sentient Disease)"
	name = "[set_name] (Sentient Disease)"
	disease_template.AssignName(set_name)
	var/datum/antagonist/disease/A = mind.has_antag_datum(/datum/antagonist/disease)
	if(A)
		A.disease_name = set_name

/mob/camera/disease/proc/infect_random_patient_zero(del_on_fail = TRUE)
	if(!freemove)
		return FALSE
	var/list/possible_hosts = list()
	var/list/afk_possible_hosts = list()
	for(var/i in GLOB.human_list)
		var/mob/living/carbon/human/H = i
		var/turf/T = get_turf(H)
		if((H.stat != DEAD) && T && is_station_level(T.z) && H.CanContractDisease(disease_template))
			if(H.client && !H.client.is_afk())
				possible_hosts += H
			else
				afk_possible_hosts += H

	shuffle_inplace(possible_hosts)
	shuffle_inplace(afk_possible_hosts)
	possible_hosts += afk_possible_hosts //ideally we want a not-afk person, but we will settle for an afk one if there are no others (mostly for testing)

	while(possible_hosts.len)
		var/mob/living/carbon/human/target = possible_hosts[1]
		if(force_infect(target))
			return TRUE
		possible_hosts.Cut(1, 2)

	if(del_on_fail)
		to_chat(src, "<span class=userdanger'>No hosts were available for your disease to infect.</span>")
		qdel(src)
	return FALSE

/mob/camera/disease/proc/force_infect(mob/living/L)
	var/datum/disease/advance/sentient_disease/V = disease_template.Copy()
	var/result = L.ForceContractDisease(V, FALSE, TRUE)
	if(result && freemove)
		end_freemove()
	return result

/mob/camera/disease/proc/end_freemove()
	if(!freemove)
		return
	freemove = FALSE
	move_on_shuttle = TRUE
	adaptation_menu_action = new /datum/action/innate/disease_adapt()
	adaptation_menu_action.Grant(src)
	for(var/V in GLOB.disease_ability_singletons)
		unpurchased_abilities[V] = TRUE
		var/datum/disease_ability/A = V
		if(A.start_with && A.CanBuy(src))
			A.Buy(src, TRUE, FALSE)
	if(freemove_end_timerid)
		deltimer(freemove_end_timerid)
	sight = SEE_SELF

/mob/camera/disease/proc/add_infection(datum/disease/advance/sentient_disease/V)
	disease_instances += V
	hosts[V.affected_mob] = V
	total_points = max(total_points, disease_instances.len)
	points += 1

	var/image/holder = V.affected_mob.hud_list[SENTIENT_DISEASE_HUD]
	var/mutable_appearance/MA = new /mutable_appearance(holder)
	MA.icon_state = "virus_infected"
	MA.layer = BELOW_MOB_LAYER
	MA.color = COLOR_GREEN_GRAY
	MA.alpha = 200
	holder.appearance = MA
	var/datum/atom_hud/my_hud = GLOB.huds[DATA_HUD_SENTIENT_DISEASE]
	my_hud.add_to_hud(V.affected_mob)

	to_chat(src, "<span class='notice'>A new host, <b>[V.affected_mob.real_name]</b>, has been infected.</span>")

	if(!following_host)
		set_following(V.affected_mob)
	refresh_adaptation_menu()

/mob/camera/disease/proc/remove_infection(datum/disease/advance/sentient_disease/V)
	if(QDELETED(src))
		disease_instances -= V
		hosts -= V.affected_mob
	else
		to_chat(src, "<span class='notice'>One of your hosts, <b>[V.affected_mob.real_name]</b>, has been purged of your infection.</span>")

		var/datum/atom_hud/my_hud = GLOB.huds[DATA_HUD_SENTIENT_DISEASE]
		my_hud.remove_from_hud(V.affected_mob)

		if(following_host == V.affected_mob)
			follow_next()

		disease_instances -= V
		hosts -= V.affected_mob

		if(!disease_instances.len)
			to_chat(src, "<span class='userdanger'>The last of your infection has disappeared.</span>")
			set_following(null)
			qdel(src)
		refresh_adaptation_menu()

/mob/camera/disease/proc/set_following(mob/living/L)
	if(following_host)
		UnregisterSignal(following_host, COMSIG_MOVABLE_MOVED)
	RegisterSignal(L, COMSIG_MOVABLE_MOVED, .proc/follow_mob)
	following_host = L
	follow_mob()

/mob/camera/disease/proc/follow_next(reverse = FALSE)
	var/index = hosts.Find(following_host)
	if(index)
		if(reverse)
			index = index == 1 ? hosts.len : index - 1
		else
			index = index == hosts.len ? 1 : index + 1
		set_following(hosts[index])

/mob/camera/disease/proc/follow_mob(datum/source, newloc, dir)
	var/turf/T = get_turf(following_host)
	if(T)
		forceMove(T)

/mob/camera/disease/DblClickOn(var/atom/A, params)
	if(hosts[A])
		set_following(A)
	else
		..()

/mob/camera/disease/ClickOn(var/atom/A, params)
	if(freemove && ishuman(A))
		var/mob/living/carbon/human/H = A
		if(alert(src, "Select [H.name] as your initial host?", "Select Host", "Yes", "No") != "Yes")
			return
		if(!freemove)
			return
		if(QDELETED(H) || !force_infect(H))
			to_chat(src, "<span class='warning'>[H ? H.name : "Host"] cannot be infected.</span>")
	else
		..()

/mob/camera/disease/proc/adapt_cooldown()
	to_chat(src, "<span class='notice'>You have altered your genetic structure. You will be unable to adapt again for [DisplayTimeText(adaptation_cooldown)].</span>")
	next_adaptation_time = world.time + adaptation_cooldown
	addtimer(CALLBACK(src, .proc/notify_adapt_ready), adaptation_cooldown)

/mob/camera/disease/proc/notify_adapt_ready()
	to_chat(src, "<span class='notice'>You are now ready to adapt again.</span>")
	refresh_adaptation_menu()

/mob/camera/disease/proc/refresh_adaptation_menu()
	if(browser_open)
		adaptation_menu()

/mob/camera/disease/proc/adaptation_menu()
	var/datum/disease/advance/sentient_disease/DT = disease_template
	if(!DT)
		return
	var/list/dat = list()

	if(examining_ability)
		dat += "<a href='byond://?src=[REF(src)];main_menu=1'>Back</a><br>"
		dat += "<h1>[examining_ability.name]</h1>"
		dat += "[examining_ability.stat_block][examining_ability.long_desc][examining_ability.threshold_block]"
		for(var/entry in examining_ability.threshold_block)
			dat += "<b>[entry]</b>: [examining_ability.threshold_block[entry]]<br>"
	else
		dat += "<h1>Disease Statistics</h1><br>\
			Resistance: [DT.totalResistance()]<br>\
			Stealth: [DT.totalStealth()]<br>\
			Stage Speed: [DT.totalStageSpeed()]<br>\
			Transmissibility: [DT.totalTransmittable()]<hr>\
			Cure: [DT.cure_text]"
		dat += "<hr><h1>Adaptations</h1>\
			Points: [points] / [total_points]\
			<table border=1>\
			<tr><td>Cost</td><td></td><td>Unlock</td><td width='180px'>Name</td><td>Type</td><td>Description</td></tr>"
		for(var/V in GLOB.disease_ability_singletons)
			var/datum/disease_ability/A = V
			var/purchase_text
			if(unpurchased_abilities[A])
				if(A.CanBuy(src))
					purchase_text = "<a href='byond://?src=[REF(src)];buy_ability=[REF(A)]'>Purchase</a>"
				else
					purchase_text = "<span class='linkOff'>Purchase</span>"
			else
				if(A.CanRefund(src))
					purchase_text = "<a href='byond://?src=[REF(src)];refund_ability=[REF(A)]'>Refund</a>"
				else
					purchase_text = "<span class='linkOff'>Refund</span>"
			dat += "<tr><td>[A.cost]</td><td>[purchase_text]</td><td>[A.required_total_points]</td><td><a href='byond://?src=[REF(src)];examine_ability=[REF(A)]'>[A.name]</a></td><td>[A.category]</td><td>[A.short_desc]</td></tr>"

		dat += "</table><br>Infect many hosts at once to gain adaptation points.<hr><h1>Infected Hosts</h1>"
		for(var/V in hosts)
			var/mob/living/L = V
			dat += "<br><a href='byond://?src=[REF(src)];follow_instance=[REF(L)]'>[L.real_name]</a>"

	browser.set_content(dat.Join())
	browser.open()
	browser_open = TRUE

/mob/camera/disease/Topic(href, list/href_list)
	..()
	if(href_list["close"])
		browser_open = FALSE
	if(usr != src)
		return
	if(href_list["follow_instance"])
		var/mob/living/L = locate(href_list["follow_instance"]) in hosts
		set_following(L)

	if(href_list["buy_ability"])
		var/datum/disease_ability/A = locate(href_list["buy_ability"]) in unpurchased_abilities
		if(!istype(A))
			return
		if(A.CanBuy(src))
			A.Buy(src)
		adaptation_menu()

	if(href_list["refund_ability"])
		var/datum/disease_ability/A = locate(href_list["refund_ability"]) in purchased_abilities
		if(!istype(A))
			return
		if(A.CanRefund(src))
			A.Refund(src)
		adaptation_menu()

	if(href_list["examine_ability"])
		var/datum/disease_ability/A = locate(href_list["examine_ability"]) in GLOB.disease_ability_singletons
		if(!istype(A))
			return
		examining_ability = A
		adaptation_menu()

	if(href_list["main_menu"])
		examining_ability = null
		adaptation_menu()


/datum/action/innate/disease_adapt
	name = "Adaptation Menu"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "disease_menu"

/datum/action/innate/disease_adapt/Activate()
	var/mob/camera/disease/D = owner
	D.adaptation_menu()
