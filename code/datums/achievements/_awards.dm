/datum/award
	///Name of the achievement, If null it wont show up in the achievement browser. (Handy for inheritance trees)
	var/name
	var/desc = "You did it."
	///Found in /datum/asset/spritesheet/simple/achievements
	var/icon = "default"
	var/category = "Normal"

	///What ID do we use in db, limited to 32 characters
	var/database_id
	//Bump this up if you're changing outdated table identifier and/or achievement type
	var/achievement_version = 2

	//Value returned on db connection failure, in case we want to differ 0 and nonexistent later on
	var/default_value = FALSE

///This proc loads the achievement data from the hub.
/datum/award/proc/load(key)
	if(!SSdbcore.Connect())
		return default_value
	if(!key || !database_id || !name)
		return default_value
	var/raw_value = get_raw_value(key)
	return parse_value(raw_value)

///This saves the changed data to the hub.
/datum/award/proc/get_changed_rows(key, value)
	if(!database_id || !key || !name)
		return
	return list("ckey" = "'[sanitizeSQL(key)]'","achievement_key" = "'[sanitizeSQL(database_id)]'", "value" = "'[sanitizeSQL(value)]'")

/datum/award/proc/get_metadata_row()
	return list("achievement_key"  = "'[sanitizeSQL(database_id)]'", "achievement_version" = "'[sanitizeSQL(achievement_version)]'", "achievement_type" = "'award'", "achievement_name" = "'[sanitizeSQL(name)]'", "achievement_description" = "'[sanitizeSQL(desc)]'")

///Get raw numerical achievement value from the database
/datum/award/proc/get_raw_value(key)
	var/datum/DBQuery/Q = SSdbcore.NewQuery("SELECT value FROM [format_table_name("achievements")] WHERE ckey = '[sanitizeSQL(key)]' AND achievement_key = '[sanitizeSQL(database_id)]'")
	if(!Q.Execute(async = TRUE))
		qdel(Q)
		return 0
	var/result = 0
	if(Q.NextRow())
		result = text2num(Q.item[1])
	qdel(Q)
	return result

//Should return sanitized value for achievement cache
/datum/award/proc/parse_value(raw_value)
	return default_value

///Can be overriden for achievement specific events
/datum/award/proc/on_unlock(mob/user)
	return

///Achievements are one-off awards for usually doing cool things.
/datum/award/achievement
	desc = "Achievement for epic people"

/datum/award/achievement/get_metadata_row()
	. = ..()
	.["achievement_type"] = "'achievement'"

/datum/award/achievement/parse_value(raw_value)
	return raw_value > 0

/datum/award/achievement/on_unlock(mob/user)
	. = ..()
	to_chat(user, "<span class='greenannounce'><B>Achievement unlocked: [name]!</B></span>")

///Scores are for leaderboarded things, such as killcount of a specific boss
/datum/award/score
	desc = "you did it sooo many times."
	category = "Scores"
	default_value = 0

	var/track_high_scores = TRUE
	var/list/high_scores = list()

/datum/award/score/New()
	. = ..()
	if(track_high_scores)
		LoadHighScores()

/datum/award/score/get_metadata_row()
	. = ..()
	.["achievement_type"] = "'score'"

/datum/award/score/proc/LoadHighScores()
	var/datum/DBQuery/Q = SSdbcore.NewQuery("SELECT ckey,value FROM [format_table_name("achievements")] WHERE achievement_key = '[sanitizeSQL(database_id)]' ORDER BY value DESC LIMIT 50")
	if(!Q.Execute(async = TRUE))
		qdel(Q)
		return
	else
		while(Q.NextRow())
			var/key = Q.item[1]
			var/score = text2num(Q.item[2])
			high_scores[key] = score
		qdel(Q)

/datum/award/score/parse_value(raw_value)
	return isnum(raw_value) ? raw_value : 0
