///Datum that handles
/datum/achievement_data
	///Ckey of this achievement data's owner
	var/owner_ckey
	///Up to date list of all achievements and their info.
	var/data = list()
	///Original status of achievement.
	var/original_cached_data = list()
	///Have we done our set-up yet?
	var/initialized = FALSE

/datum/achievement_data/New(ckey)
	owner_ckey = ckey
	if(SSachievements.initialized && !initialized)
		InitializeData()

/datum/achievement_data/proc/InitializeData()
	initialized = TRUE
	load_all_achievements() //So we know which achievements we have unlocked so far.

///Gets list of changed rows in MassInsert format
/datum/achievement_data/proc/get_changed_data()
	. = list()
	for(var/T in data)
		var/datum/award/A = SSachievements.awards[T]
		if(data[T] != original_cached_data[T])//If our data from before is not the same as now, save it to db.
			var/deets = A.get_changed_rows(owner_ckey,data[T])
			if(deets)
				. += list(deets)

/datum/achievement_data/proc/load_all_achievements()
	set waitfor = FALSE

	var/list/kv = list()
	var/datum/DBQuery/Query = SSdbcore.NewQuery("SELECT achievement_key,value FROM [format_table_name("achievements")] WHERE ckey = '[sanitizeSQL(owner_ckey)]'")
	if(!Query.Execute())
		qdel(Query)
		return
	while(Query.NextRow())
		var/key = Query.item[1]
		var/value = text2num(Query.item[2])
		kv[key] = value
	qdel(Query)

	for(var/T in subtypesof(/datum/award))
		var/datum/award/A = SSachievements.awards[T]
		if(!A || !A.name) //Skip abstract achievements types
			continue
		if(!data[T])
			data[T] = A.parse_value(kv[A.database_id])
			original_cached_data[T] = data[T]

///Updates local cache with db data for the given achievement type if it wasn't loaded yet.
/datum/achievement_data/proc/get_data(achievement_type)
	var/datum/award/A = SSachievements.awards[achievement_type]
	if(!A.name)
		return FALSE
	if(!data[achievement_type])
		data[achievement_type] = A.load(owner_ckey)
		original_cached_data[achievement_type] = data[achievement_type]

///Unlocks an achievement of a specific type.
/datum/achievement_data/proc/unlock(achievement_type, mob/user)
	if(!SSachievements.achievements_enabled)
		return
	var/datum/award/A = SSachievements.awards[achievement_type]
	get_data(achievement_type) //Get the current status first if necessary
	if(istype(A, /datum/award/achievement))
		data[achievement_type] = TRUE
		A.on_unlock(user) //Only on default achievement, as scores keep going up.
	else if(istype(A, /datum/award/score))
		data[achievement_type] += 1

///Getter for the status/score of an achievement
/datum/achievement_data/proc/get_achievement_status(achievement_type)
	return data[achievement_type]

///Resets an achievement to default values.
/datum/achievement_data/proc/reset(achievement_type)
	if(!SSachievements.achievements_enabled)
		return
	var/datum/award/A = SSachievements.awards[achievement_type]
	get_data(achievement_type)
	if(istype(A, /datum/award/achievement))
		data[achievement_type] = FALSE
	else if(istype(A, /datum/award/score))
		data[achievement_type] = 0

/datum/achievement_data/ui_base_html(html)
	var/datum/asset/spritesheet/simple/assets = get_asset_datum(/datum/asset/spritesheet/simple/achievements)
	. = replacetext(html, "<!--customheadhtml-->", assets.css_tag())

/datum/achievement_data/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.always_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		var/datum/asset/spritesheet/simple/assets = get_asset_datum(/datum/asset/spritesheet/simple/achievements)
		assets.send(user)
		ui = new(user, src, ui_key, "achievements", "Achievements Menu", 800, 1000, master_ui, state)
		ui.open()

/datum/achievement_data/ui_data(mob/user)
	var/ret_data = list() // screw standards (qustinnus you must rename src.data ok)
	ret_data["categories"] = list("Bosses", "Misc" , "Scores")
	ret_data["achievements"] = list()
	ret_data["user_key"] = user.ckey

	var/datum/asset/spritesheet/simple/assets = get_asset_datum(/datum/asset/spritesheet/simple/achievements)
	//This should be split into static data later
	for(var/achievement_type in SSachievements.awards)
		if(!SSachievements.awards[achievement_type].name) //No name? we a subtype.
			continue
		if(isnull(data[achievement_type])) //We're still loading
			continue
		var/list/this = list(
			"name" = SSachievements.awards[achievement_type].name,
			"desc" = SSachievements.awards[achievement_type].desc,
			"category" = SSachievements.awards[achievement_type].category,
			"icon_class" = assets.icon_class_name(SSachievements.awards[achievement_type].icon),
			"value" = data[achievement_type],
			"score" = ispath(achievement_type,/datum/award/score)
			)
		ret_data["achievements"] += list(this)

	return ret_data

/datum/achievement_data/ui_static_data(mob/user)
	. = ..()
	.["highscore"] = list()
	for(var/score in SSachievements.scores)
		var/datum/award/score/S = SSachievements.scores[score]
		if(!S.name || !S.track_high_scores || !S.high_scores.len)
			continue
		.["highscore"] += list(list("name" = S.name,"scores" = S.high_scores))

/client/verb/checkachievements()
	set category = "OOC"
	set name = "Check achievements"
	set desc = "See all of your achievements!"

	player_details.achievements.ui_interact(usr)
