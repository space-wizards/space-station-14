#define SOUND_EMITTER_LOCAL "local" //Plays the sound like a normal heard sound
#define SOUND_EMITTER_DIRECT "direct" //Plays the sound directly to hearers regardless of pressure/proximity/et cetera

#define SOUND_EMITTER_RADIUS "radius" //Plays the sound to everyone in a radius
#define SOUND_EMITTER_ZLEVEL "zlevel" //Plays the sound to everyone on the z-level
#define SOUND_EMITTER_GLOBAL "global" //Plays the sound to everyone in the game world

//Admin sound emitters with highly customizable functions!
/obj/effect/sound_emitter
	name = "sound emitter"
	desc = "Emits sounds, presumably."
	icon = 'icons/effects/effects.dmi'
	icon_state = "shield2"
	invisibility = INVISIBILITY_OBSERVER
	anchored = TRUE
	density = FALSE
	opacity = FALSE
	alpha = 175
	var/sound_file //The sound file the emitter plays
	var/sound_volume = 50 //The volume the sound file is played at
	var/play_radius = 3 //Any mobs within this many tiles will hear the sounds played if it's using the appropriate mode
	var/motus_operandi = SOUND_EMITTER_LOCAL //The mode this sound emitter is using
	var/emitter_range = SOUND_EMITTER_ZLEVEL //The range this emitter's sound is heard at; this isn't a number, but a string (see the defines above)

/obj/effect/sound_emitter/Destroy(force)
	if(!force)
		return QDEL_HINT_LETMELIVE
	. = ..()

/obj/effect/sound_emitter/singularity_act()
	return

/obj/effect/sound_emitter/singularity_pull()
	return

/obj/effect/sound_emitter/examine(mob/user)
	. = ..()
	if(!isobserver(user))
		return
	. += "<span class='boldnotice'>Sound File:</span> [sound_file ? sound_file : "None chosen"]"
	. += "<span class='boldnotice'>Mode:</span> [motus_operandi]</span>"
	. += "<span class='boldnotice'>Range:</span> [emitter_range]</span>"
	. += "<b>Sound is playing at [sound_volume]% volume.</b>"
	if(user.client.holder)
		. += "<b>Alt-click it to quickly activate it!</b>"

//ATTACK GHOST IGNORING PARENT RETURN VALUE
/obj/effect/sound_emitter/attack_ghost(mob/user)
	if(!check_rights_for(user.client, R_SOUND))
		examine(user)
		return
	edit_emitter(user)

/obj/effect/sound_emitter/AltClick(mob/user)
	if(check_rights_for(user.client, R_SOUND))
		activate(user)
		to_chat(user, "<span class='notice'>Sound emitter activated.</span>")

/obj/effect/sound_emitter/proc/edit_emitter(mob/user)
	var/dat = ""
	dat += "<b>Label:</b> <a href='?src=\ref[src];edit_label=1'>[maptext ? maptext : "No label set!"]</a><br>"
	dat += "<br>"
	dat += "<b>Sound File:</b> <a href='?src=\ref[src];edit_sound_file=1'>[sound_file ? sound_file : "No file chosen!"]</a><br>"
	dat += "<b>Volume:</b> <a href='?src=\ref[src];edit_volume=1'>[sound_volume]%</a><br>"
	dat += "<br>"
	dat += "<b>Mode:</b> <a href='?src=\ref[src];edit_mode=1'>[motus_operandi]</a><br>"
	if(motus_operandi != SOUND_EMITTER_LOCAL)
		dat += "<b>Range:</b> <a href='?src=\ref[src];edit_range=1'>[emitter_range]</a>[emitter_range == SOUND_EMITTER_RADIUS ? "<a href='?src=\ref[src];edit_radius=1'>[play_radius]-tile radius</a>" : ""]<br>"
	dat += "<br>"
	dat += "<a href='?src=\ref[src];play=1'>Play Sound</a> (interrupts other sound emitter sounds)"
	var/datum/browser/popup = new(user, "emitter", "", 500, 600)
	popup.set_content(dat)
	popup.open()

/obj/effect/sound_emitter/Topic(href, href_list)
	..()
	if(!ismob(usr) || !usr.client || !check_rights_for(usr.client, R_SOUND))
		return
	var/mob/user = usr
	if(href_list["edit_label"])
		var/new_label = stripped_input(user, "Choose a new label.", "Sound Emitter")
		if(!new_label)
			return
		maptext = new_label
		to_chat(user, "<span class='notice'>Label set to [maptext].</span>")
	if(href_list["edit_sound_file"])
		var/new_file = input(user, "Choose a sound file.", "Sound Emitter") as null|sound
		if(!new_file)
			return
		sound_file = new_file
		to_chat(user, "<span class='notice'>New sound file set to [sound_file].</span>")
	if(href_list["edit_volume"])
		var/new_volume = input(user, "Choose a volume.", "Sound Emitter", sound_volume) as null|num
		if(isnull(new_volume))
			return
		new_volume = CLAMP(new_volume, 0, 100)
		sound_volume = new_volume
		to_chat(user, "<span class='notice'>Volume set to [sound_volume]%.</span>")
	if(href_list["edit_mode"])
		var/new_mode
		var/mode_list = list("Local (normal sound)" = SOUND_EMITTER_LOCAL, "Direct (not affected by environment/location)" = SOUND_EMITTER_DIRECT)
		new_mode = input(user, "Choose a new mode.", "Sound Emitter") as null|anything in mode_list
		if(!new_mode)
			return
		motus_operandi = mode_list[new_mode]
		to_chat(user, "<span class='notice'>Mode set to [motus_operandi].</span>")
	if(href_list["edit_range"])
		var/new_range
		var/range_list = list("Radius (all mobs within a radius)" = SOUND_EMITTER_RADIUS, "Z-Level (all mobs on the same z)" = SOUND_EMITTER_ZLEVEL, "Global (all players)" = SOUND_EMITTER_GLOBAL)
		new_range = input(user, "Choose a new range.", "Sound Emitter") as null|anything in range_list
		if(!new_range)
			return
		emitter_range = range_list[new_range]
		to_chat(user, "<span class='notice'>Range set to [emitter_range].</span>")
	if(href_list["edit_radius"])
		var/new_radius = input(user, "Choose a radius.", "Sound Emitter", sound_volume) as null|num
		if(isnull(new_radius))
			return
		new_radius = CLAMP(new_radius, 0, 127)
		play_radius = new_radius
		to_chat(user, "<span class='notice'>Audible radius set to [play_radius].</span>")
	if(href_list["play"])
		activate(user)
	edit_emitter(user) //Refresh the UI to see our changes

/obj/effect/sound_emitter/proc/activate(mob/user)
	var/list/hearing_mobs = list()
	if(motus_operandi == SOUND_EMITTER_LOCAL)
		playsound(src, sound_file, sound_volume, FALSE)
		return
	switch(emitter_range)
		if(SOUND_EMITTER_RADIUS)
			for(var/mob/M in GLOB.player_list)
				if(get_dist(src, M) <= play_radius)
					hearing_mobs += M
		if(SOUND_EMITTER_ZLEVEL)
			for(var/mob/M in GLOB.player_list)
				if(M.z == z)
					hearing_mobs += M
		if(SOUND_EMITTER_GLOBAL)
			hearing_mobs = GLOB.player_list.Copy()
	for(var/mob/M in hearing_mobs)
		if(M.client.prefs.toggles & SOUND_MIDI)
			M.playsound_local(M, sound_file, sound_volume, FALSE, channel = CHANNEL_ADMIN, pressure_affected = FALSE)
	if(user)
		log_admin("[ADMIN_LOOKUPFLW(user)] activated a sound emitter with file \"[sound_file]\" at [AREACOORD(src)]")
	flick("shield1", src)

#undef SOUND_EMITTER_LOCAL
#undef SOUND_EMITTER_DIRECT
#undef SOUND_EMITTER_RADIUS
#undef SOUND_EMITTER_ZLEVEL
#undef SOUND_EMITTER_GLOBAL
