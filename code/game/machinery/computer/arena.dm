#define ARENA_RED_TEAM "red"
#define ARENA_GREEN_TEAM "green"
#define ARENA_DEFAULT_ID "arena_default"
#define ARENA_CORNER_A "cornerA"
#define ARENA_CORNER_B "cornerB"

/// Arena related landmarks
/obj/effect/landmark/arena
	name = "arena landmark"
	var/landmark_tag
	var/arena_id = ARENA_DEFAULT_ID

/obj/effect/landmark/arena/start
	name = "arena corner A"
	landmark_tag = ARENA_CORNER_A

/obj/effect/landmark/arena/end
	name = "arena corner B"
	landmark_tag = ARENA_CORNER_B

/// Controller for admin event arenas
/obj/machinery/computer/arena
	name = "arena controller"
	/// Arena ID
	var/arena_id = ARENA_DEFAULT_ID
	/// Enables/disables spawning
	var/ready_to_spawn = FALSE
	/// Assoc list of map templates indexed by user friendly names
	var/static/list/arena_templates = list()
	/// Were the config directory arenas loaded
	var/static/default_arenas_loaded = FALSE
	/// Name of currently loaded template
	var/current_arena_template = "None"
	// What turf arena clears to
	var/empty_turf_type = /turf/open/indestructible
	// List of team ids
	var/list/teams = list(ARENA_RED_TEAM,ARENA_GREEN_TEAM)
	/// List of hud instances indedxed by team id
	var/static/list/team_huds = list()
	/// List of hud colors indexed by team id
	var/static/list/team_colors = list(ARENA_RED_TEAM = "red", ARENA_GREEN_TEAM = "green")
	// Team hud index in GLOB.huds indexed by team id
	var/static/list/team_hud_index = list()

	/// List of ckeys indexed by team id
	var/list/team_keys = list()
	/// List of outfit datums/types indexed by team id, can be empty
	var/list/outfits = list()
	/// Default team outfit if outfits[team] is empty
	var/default_outfit = /datum/outfit/job/assistant

	/// Is the arena template loading in
	var/loading = FALSE

	//How long between admin pressing start and doors opening
	var/start_delay = 30 SECONDS
	//Value for the countdown
	var/start_time
	var/list/countdowns = list() //List of countdown effects ticking down to start

	//Sound played when the fight starts.
	var/start_sound = 'sound/items/airhorn2.ogg'
	var/start_sound_volume = 50

/obj/machinery/computer/arena/Initialize(mapload, obj/item/circuitboard/C)
	. = ..()
	LoadDefaultArenas()
	GenerateAntagHuds()

/obj/machinery/computer/arena/proc/GenerateAntagHuds()
	for(var/team in teams)
		var/datum/atom_hud/antag/teamhud = team_huds[team]
		if(!teamhud) //These will be shared between arenas because this stuff is expensive and cross arena fighting is not a thing anyway
			teamhud = new
			teamhud.icon_color = team_colors[team]
			GLOB.huds += teamhud
			team_huds[team] = teamhud
			team_hud_index[team] = length(GLOB.huds)

/**
  * Loads the arenas from config directory.
  * THESE ARE FULLY CACHED FOR QUICK SWITCHING SO KEEP TRACK OF THE AMOUNT
  */
/obj/machinery/computer/arena/proc/LoadDefaultArenas()
	if(default_arenas_loaded)
		return
	var/arena_dir = "[global.config.directory]/arenas/"
	var/list/default_arenas = flist(arena_dir)
	for(var/arena_file in default_arenas)
		var/simple_name = replacetext(replacetext(arena_file,arena_dir,""),".dmm","")
		add_new_arena_template(null,arena_dir + arena_file,simple_name)

/obj/machinery/computer/arena/proc/get_landmark_turf(landmark_tag)
	for(var/obj/effect/landmark/arena/L in GLOB.landmarks_list)
		if(L.arena_id == arena_id && L.landmark_tag == landmark_tag && isturf(L.loc))
			return L.loc

/obj/machinery/computer/arena/proc/get_load_point()
	var/turf/A = get_landmark_turf(ARENA_CORNER_A)
	var/turf/B = get_landmark_turf(ARENA_CORNER_B)
	return locate(min(A.x,B.x),min(A.y,B.y),A.z)

/obj/machinery/computer/arena/proc/get_arena_turfs()
	var/lp = get_load_point()
	var/turf/A = get_landmark_turf(ARENA_CORNER_A)
	var/turf/B = get_landmark_turf(ARENA_CORNER_B)
	var/turf/hp = locate(max(A.x,B.x),max(A.y,B.y),A.z)
	return block(lp,hp)

/obj/machinery/computer/arena/proc/clear_arena()
	for(var/turf/T in get_arena_turfs())
		T.empty(turf_type = /turf/open/indestructible)
	current_arena_template = "None"

/obj/machinery/computer/arena/proc/load_arena(arena_template,mob/user)
	if(loading)
		return
	var/datum/map_template/M = arena_templates[arena_template]
	if(!M)
		to_chat(user,"<span class='warning'>No such arena</span>")
		return
	clear_arena() //Clear current arena
	var/turf/A = get_landmark_turf(ARENA_CORNER_A)
	var/turf/B = get_landmark_turf(ARENA_CORNER_B)
	var/wh = abs(A.x - B.x) + 1
	var/hz = abs(A.y - B.y) + 1
	if(M.width > wh || M.height > hz)
		to_chat(user,"<span class='warning'>Arena template is too big for the current arena!</span>")
		return
	loading = TRUE
	var/bd = M.load(get_load_point())
	if(bd)
		current_arena_template = arena_template
	loading = FALSE

	message_admins("[key_name_admin(user)] loaded [arena_template] event arena for [arena_id] arena.")
	log_admin("[key_name(user)] loaded [arena_template] event arena for [arena_id] arena.")



/obj/machinery/computer/arena/proc/add_new_arena_template(user,fname,friendly_name)
	if(!fname)
		fname = input(user, "Upload dmm file to use as arena template","Upload Map Template") as null|file
	if(!fname)
		return
	if(!friendly_name)
		friendly_name = "[fname]" //Could ask the user for friendly name here

	var/datum/map_template/T = new(fname,friendly_name,TRUE)
	if(!T.cached_map || T.cached_map.check_for_errors())
		to_chat(user,"Map failed to parse check for errors.")
		return

	arena_templates[T.name] = T
	message_admins("[key_name_admin(user)] uploaded new event arena: [friendly_name].")
	log_admin("[key_name(user)] uploaded new event arena: [friendly_name].")

/obj/machinery/computer/arena/proc/load_team(user,team)
	var/rawteam = stripped_multiline_input(user,"Enter team list (ckeys separated by newline)")
	for(var/i in splittext(rawteam,"\n"))
		var/key = ckey(i)
		if(!i)
			continue
		add_team_member(user,team,key)

/obj/machinery/computer/arena/proc/add_team_member(mob/user,team,key)
	if(!key)
		var/list/keys = list()
		for(var/mob/M in GLOB.player_list)
			keys += M.client
		var/client/selection = input("Please, select a player!", "Team member", null, null) as null|anything in sortKey(keys)
		//Could be freeform if you want to add disconnected i guess
		if(!selection)
			return
		key = selection.ckey
	if(!team_keys[team])
		team_keys[team] = list(key)
	else
		team_keys[team] |= key
	to_chat(user,"[key] added to [team] team.")

/obj/machinery/computer/arena/proc/remove_member(mob/user,ckey,team)
	team_keys[team] -= ckey
	to_chat(user,"[ckey] removed from [team] team.")

/obj/machinery/computer/arena/proc/spawn_member(obj/machinery/arena_spawn/spawnpoint,ckey,team)
	var/mob/oldbody = get_mob_by_key(ckey)
	if(!isobserver(oldbody))
		return
	var/mob/living/carbon/human/M = new/mob/living/carbon/human(get_turf(spawnpoint))
	oldbody.client.prefs.copy_to(M)
	M.set_species(/datum/species/human) // Could use setting per team
	M.equipOutfit(outfits[team] ? outfits[team] : default_outfit)
	M.faction += team //In case anyone wants to add team based stuff to arena special effects
	M.key = ckey

	var/datum/atom_hud/antag/team_hud = team_huds[team]
	team_hud.join_hud(M)
	set_antag_hud(M,"arena",team_hud_index[team])

/obj/machinery/computer/arena/proc/change_outfit(mob/user,team)
	outfits[team] = user.client.robust_dress_shop()

/obj/machinery/computer/arena/proc/toggle_spawn(mob/user)
	ready_to_spawn = !ready_to_spawn
	to_chat(user,"You [ready_to_spawn ? "enable" : "disable"] the spawners.")
	log_admin("[key_name(user)] toggled event arena spawning for [arena_id] arena.")
	// Could use update_icon on spawnpoints here to show they're on
	if(ready_to_spawn)
		for(var/mob/M in all_contestants())
			to_chat(M,"<span class='userdanger'>Arena you're signed up for is ready!</span>")

/obj/machinery/computer/arena/proc/all_contestants()
	. = list()
	for(var/team in team_keys)
		for(var/key in team_keys[team])
			var/mob/M = get_mob_by_key(key)
			if(M)
				. += M

/obj/machinery/computer/arena/proc/reset_arena()
	clear_arena()
	set_doors(closed = TRUE)

/obj/machinery/computer/arena/proc/get_spawn(team)
	for(var/obj/machinery/arena_spawn/A in GLOB.machines)
		if(A.arena_id == arena_id && A.team == team)
			return A

/obj/machinery/computer/arena/proc/start_match(mob/user)
	//TODO: Check if everyone is spawned in, if not ask for confirmation.
	var/timetext = DisplayTimeText(start_delay)
	to_chat(user,"<span class='notice'>The match will start in [timetext].</span>")
	for(var/mob/M in all_contestants())
		to_chat(M,"<span class='userdanger'>The gates will open in [timetext]!</span>")
	start_time = world.time + start_delay
	addtimer(CALLBACK(src,.proc/begin),start_delay)
	for(var/team in teams)
		var/obj/machinery/arena_spawn/team_spawn = get_spawn(team)
		var/obj/effect/countdown/arena/A = new(team_spawn)
		A.start()
		countdowns += A

/obj/machinery/computer/arena/proc/begin()
	ready_to_spawn = FALSE
	set_doors(closed = FALSE)
	if(start_sound)
		for(var/team in teams)
			var/obj/machinery/arena_spawn/A = get_spawn(team)
			playsound(A,start_sound, start_sound_volume)
	for(var/mob/M in all_contestants())
		to_chat(M,"<span class='userdanger'>START!</span>")
	//Clean up the countdowns
	QDEL_LIST(countdowns)
	start_time = null
	updateUsrDialog()


/obj/machinery/computer/arena/proc/set_doors(closed = FALSE)
	for(var/obj/machinery/door/poddoor/D in GLOB.machines) //I really dislike pathing of these
		if(D.id != arena_id)
			continue
		if(closed)
			INVOKE_ASYNC(D, /obj/machinery/door/poddoor.proc/close)
		else
			INVOKE_ASYNC(D, /obj/machinery/door/poddoor.proc/open)

/obj/machinery/computer/arena/Topic(href, href_list)
	if(..())
		return
	var/mob/user = usr

	if(!user.client.holder) // Should it require specific perm ?
		return

	if(href_list["upload"])
		add_new_arena_template(user)
	if(href_list["change_arena"])
		load_arena(href_list["change_arena"],user)
	if(href_list["toggle_spawn"])
		toggle_spawn(user)
	if(href_list["start"])
		start_match(user)
	if(href_list["team_action"])
		var/team = href_list["team"]
		switch(href_list["team_action"])
			if("addmember")
				add_team_member(user,team)
			if("loadteam")
				load_team(user,team)
			if("outfit")
				change_outfit(user,team)
	if(href_list["special"])
		switch(href_list["special"])
			if("reset")
				reset_arena()
			//Just example in case you want to add more
			if("randomarena")
				load_random_arena(user)
			if("spawntrophy")
				trophy_for_last_man_standing(user)
	if(href_list["member_action"])
		var/ckey = href_list["ckey"]
		var/team = href_list["team"]
		switch(href_list["member_action"])
			if("remove")
				remove_member(user,ckey,team)
	updateUsrDialog()

// Special functions

/obj/machinery/computer/arena/proc/load_random_arena(mob/user)
	if(!length(arena_templates))
		to_chat(user,"<span class='warning'>No arenas present</span>")
		return
	var/picked = pick(arena_templates)
	load_arena(picked,user)

/obj/machinery/computer/arena/proc/trophy_for_last_man_standing()
	var/arena_turfs = get_arena_turfs()
	for(var/mob/living/L in GLOB.mob_living_list)
		if(L.stat != DEAD && (get_turf(L) in arena_turfs))
			var/obj/item/reagent_containers/food/drinks/trophy/gold_cup/G = new(get_turf(L))
			G.name = "[L.real_name]'s Trophy"

/obj/machinery/computer/arena/ui_interact(mob/user, ui_key, datum/tgui/ui, force_open, datum/tgui/master_ui, datum/ui_state/state)
	. = ..()
	var/list/dat = list()
	dat += "<div>Spawning is currently [ready_to_spawn ? "<span class='good'>enabled</span>" : "<span class='bad'>disabled</span>"] <a href='?src=[REF(src)];toggle_spawn=1'>Toggle</a></div>"
	dat += "<div><a href='?src=[REF(src)];start=1'>[start_time ? "Stop countdown" : "Start!"]</a></div>"
	for(var/team in teams)
		dat += "<h2>[capitalize(team)] team:</h2>"
		dat += "<ul>"
		for(var/ckey in team_keys[team])
			var/player_status = "Not Present"
			var/mob/M = get_mob_by_key(ckey)
			if(M)
				//Should define waiting room upper/lower corner and check if they're there instead of generic live/dead check
				if(isobserver(M))
					player_status = "Ghosted"
				else
					player_status = M.stat == DEAD ? "Dead" : "Alive"
				dat += "<li>[ckey] - [player_status] - "
				dat += "<a href='?_src_=holder;[HrefToken(TRUE)];adminplayerobservefollow=[REF(M)]'>FLW</a>"
				dat += "<a href='?src=[REF(src)];member_action=remove;team=[team];ckey=[ckey]'>Remove</a>"
				//Add more per player features here
				dat += "</li>"
		dat += "</ul>"
		dat += "<div> Team Outfit : [outfits[team] ? outfits[team] : default_outfit]</div>"
		dat += "<a href='?src=[REF(src)];team_action=loadteam;team=[team]'>Load team</a>"
		dat += "<a href='?src=[REF(src)];team_action=addmember;team=[team]'>Add member</a>"
		dat += "<a href='?src=[REF(src)];team_action=outfit;team=[team]'>Change Outfit</a>"
		//Add more per team features here

	dat += "Current arena: [current_arena_template]"
	dat += "<h2>Arena List:</h2>"
	for(var/A in arena_templates)
		dat += "<a href='?src=[REF(src)];change_arena=[url_encode(A)]'>[A]</a><br>"
	dat += "<hr>"
	dat += "<a href='?src=[REF(src)];upload=1'>Upload new arena</a><br>"
	dat += "<hr>"
	//Special actions
	dat += "<a href='?src=[REF(src)];special=reset'>Reset Arena.</a><br>"
	dat += "<a href='?src=[REF(src)];special=randomarena'>Load random arena.</a><br>"
	dat += "<a href='?src=[REF(src)];special=spawntrophy'>Spawn trophies for survivors.</a><br>"

	var/datum/browser/popup = new(user, "arena controller", "Arena Controller", 500, 600)
	popup.set_content(dat.Join())
	popup.open()

/// Arena spawnpoint
/obj/machinery/arena_spawn
	name = "Arena Spawnpoint"
	icon = 'icons/obj/device.dmi'
	icon_state = "syndbeacon"
	resistance_flags = INDESTRUCTIBLE
	/// In case we have multiple arena controllers at once.
	var/arena_id = ARENA_DEFAULT_ID
	/// Team ID
	var/team = "default"
	/// only exist to cut down on glob.machines lookups, do not modify
	var/obj/machinery/computer/arena/_controller

/obj/machinery/arena_spawn/red
	name = "Red Team Spawnpoint"
	color = "red"
	team = ARENA_RED_TEAM

/obj/machinery/arena_spawn/green
	name = "Green Team Spawnpoint"
	color = "green"
	team = ARENA_GREEN_TEAM

/obj/machinery/arena_spawn/proc/get_controller()
	if(_controller && !QDELETED(_controller) && _controller.arena_id == arena_id)
		return _controller
	for(var/obj/machinery/computer/arena/A in GLOB.machines)
		if(A.arena_id == arena_id)
			_controller = A
			return _controller

/obj/machinery/arena_spawn/attack_ghost(mob/user)
	var/obj/machinery/computer/arena/C = get_controller()
	if(!C) //Unlinked spawn
		return
	if(C.ready_to_spawn)
		var/list/allowed_keys = C.team_keys[team]
		if(!(user.ckey in allowed_keys))
			to_chat(user,"<span class='warning'>You're not on the team list.</span>")
			return
		C.spawn_member(src,user.ckey,team)

#undef ARENA_GREEN_TEAM
#undef ARENA_RED_TEAM
#undef ARENA_DEFAULT_ID
#undef ARENA_CORNER_A
#undef ARENA_CORNER_B
