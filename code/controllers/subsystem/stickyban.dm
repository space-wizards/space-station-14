SUBSYSTEM_DEF(stickyban)
	name = "PRISM"
	init_order = INIT_ORDER_STICKY_BAN
	flags = SS_NO_FIRE

	var/list/cache = list()
	var/list/dbcache = list()
	var/list/confirmed_exempt = list()
	var/dbcacheexpire = 0


/datum/controller/subsystem/stickyban/Initialize(timeofday)
	if (length(GLOB.stickybanadminexemptions))
		restore_stickybans()
	var/list/bannedkeys = sticky_banned_ckeys()
	//sanitize the sticky ban list

	//delete db bans that no longer exist in the database and add new legacy bans to the database
	if (SSdbcore.Connect() || length(SSstickyban.dbcache))
		if (length(GLOB.stickybanadminexemptions))
			restore_stickybans()
		for (var/oldban in (world.GetConfig("ban") - bannedkeys))
			var/ckey = ckey(oldban)
			if (ckey != oldban && (ckey in bannedkeys))
				continue

			var/list/ban = params2list(world.GetConfig("ban", oldban))
			if (ban && !ban["fromdb"])
				if (!import_raw_stickyban_to_db(ckey, ban))
					log_world("Could not import stickyban on [oldban] into the database. Ignoring")
					continue
				dbcacheexpire = 0
				bannedkeys += ckey
			world.SetConfig("ban", oldban, null)

	if (length(GLOB.stickybanadminexemptions)) //the previous loop can sleep
		restore_stickybans()

	for (var/bannedkey in bannedkeys)
		var/ckey = ckey(bannedkey)
		var/list/ban = get_stickyban_from_ckey(bannedkey)

		//byond stores sticky bans by key, that's lame
		if (ckey != bannedkey)
			world.SetConfig("ban", bannedkey, null)

		if (!ban["ckey"])
			ban["ckey"] = ckey

		ban["matches_this_round"] = list()
		ban["existing_user_matches_this_round"] = list()
		ban["admin_matches_this_round"] = list()
		ban["pending_matches_this_round"] = list()

		cache[ckey] = ban
		world.SetConfig("ban", ckey, list2stickyban(ban))

	return ..()

/datum/controller/subsystem/stickyban/proc/Populatedbcache()
	var/newdbcache = list() //so if we runtime or the db connection dies we don't kill the existing cache

	var/datum/DBQuery/query_stickybans = SSdbcore.NewQuery("SELECT ckey, reason, banning_admin, datetime FROM [format_table_name("stickyban")] ORDER BY ckey")
	var/datum/DBQuery/query_ckey_matches = SSdbcore.NewQuery("SELECT stickyban, matched_ckey, first_matched, last_matched, exempt FROM [format_table_name("stickyban_matched_ckey")] ORDER BY first_matched")
	var/datum/DBQuery/query_cid_matches = SSdbcore.NewQuery("SELECT stickyban, matched_cid, first_matched, last_matched FROM [format_table_name("stickyban_matched_cid")] ORDER BY first_matched")
	var/datum/DBQuery/query_ip_matches = SSdbcore.NewQuery("SELECT stickyban, INET_NTOA(matched_ip), first_matched, last_matched FROM [format_table_name("stickyban_matched_ip")] ORDER BY first_matched")

	SSdbcore.QuerySelect(list(query_stickybans, query_ckey_matches, query_cid_matches, query_ip_matches))

	if (query_stickybans.last_error)
		qdel(query_stickybans)
		qdel(query_ckey_matches)
		qdel(query_cid_matches)
		qdel(query_ip_matches)
		return

	while (query_stickybans.NextRow())
		var/list/ban = list()

		ban["ckey"] = query_stickybans.item[1]
		ban["message"] = query_stickybans.item[2]
		ban["reason"] = "(InGameBan)([query_stickybans.item[3]])"
		ban["admin"] = query_stickybans.item[3]
		ban["datetime"] = query_stickybans.item[4]
		ban["type"] = list("sticky")

		newdbcache["[query_stickybans.item[1]]"] = ban


	if (!query_ckey_matches.last_error)
		while (query_ckey_matches.NextRow())
			var/list/match = list()

			match["stickyban"] = query_ckey_matches.item[1]
			match["matched_ckey"] = query_ckey_matches.item[2]
			match["first_matched"] = query_ckey_matches.item[3]
			match["last_matched"] = query_ckey_matches.item[4]
			match["exempt"] = text2num(query_ckey_matches.item[5])

			var/ban = newdbcache[query_ckey_matches.item[1]]
			if (!ban)
				continue
			var/keys = ban[text2num(query_ckey_matches.item[5]) ? "whitelist" : "keys"]
			if (!keys)
				keys = ban[text2num(query_ckey_matches.item[5]) ? "whitelist" : "keys"] = list()
			keys[query_ckey_matches.item[2]] = match

	if (!query_cid_matches.last_error)
		while (query_cid_matches.NextRow())
			var/list/match = list()

			match["stickyban"] = query_cid_matches.item[1]
			match["matched_cid"] = query_cid_matches.item[2]
			match["first_matched"] = query_cid_matches.item[3]
			match["last_matched"] = query_cid_matches.item[4]

			var/ban = newdbcache[query_cid_matches.item[1]]
			if (!ban)
				continue
			var/computer_ids = ban["computer_id"]
			if (!computer_ids)
				computer_ids = ban["computer_id"] = list()
			computer_ids[query_cid_matches.item[2]] = match


	if (!query_ip_matches.last_error)
		while (query_ip_matches.NextRow())
			var/list/match = list()

			match["stickyban"] = query_ip_matches.item[1]
			match["matched_ip"] = query_ip_matches.item[2]
			match["first_matched"] = query_ip_matches.item[3]
			match["last_matched"] = query_ip_matches.item[4]

			var/ban = newdbcache[query_ip_matches.item[1]]
			if (!ban)
				continue
			var/IPs = ban["IP"]
			if (!IPs)
				IPs = ban["IP"] = list()
			IPs[query_ip_matches.item[2]] = match

	dbcache = newdbcache
	dbcacheexpire = world.time+STICKYBAN_DB_CACHE_TIME

	qdel(query_stickybans)
	qdel(query_ckey_matches)
	qdel(query_cid_matches)
	qdel(query_ip_matches)


/datum/controller/subsystem/stickyban/proc/import_raw_stickyban_to_db(ckey, list/ban)
	. = FALSE
	if (!ban["admin"])
		ban["admin"] = "LEGACY"
	if (!ban["message"])
		ban["message"] = "Evasion"

	var/datum/DBQuery/query_create_stickyban = SSdbcore.NewQuery("INSERT IGNORE INTO [format_table_name("stickyban")] (ckey, reason, banning_admin) VALUES ('[sanitizeSQL(ckey)]', '[sanitizeSQL(ban["message"])]', '[sanitizeSQL(ban["admin"])]')")
	if (!query_create_stickyban.warn_execute())
		qdel(query_create_stickyban)
		return
	qdel(query_create_stickyban)

	var/list/sqlckeys = list()
	var/list/sqlcids = list()
	var/list/sqlips = list()

	if (ban["keys"])
		var/list/keys = splittext(ban["keys"], ",")
		for (var/key in keys)
			var/list/sqlckey = list()
			sqlckey["stickyban"] = "'[sanitizeSQL(ckey)]'"
			sqlckey["matched_ckey"] = "'[sanitizeSQL(ckey(key))]'"
			sqlckey["exempt"] = FALSE
			sqlckeys[++sqlckeys.len] = sqlckey

	if (ban["whitelist"])
		var/list/keys = splittext(ban["whitelist"], ",")
		for (var/key in keys)
			var/list/sqlckey = list()
			sqlckey["stickyban"] = "'[sanitizeSQL(ckey)]'"
			sqlckey["matched_ckey"] = "'[sanitizeSQL(ckey(key))]'"
			sqlckey["exempt"] = TRUE
			sqlckeys[++sqlckeys.len] = sqlckey

	if (ban["computer_id"])
		var/list/cids = splittext(ban["computer_id"], ",")
		for (var/cid in cids)
			var/list/sqlcid = list()
			sqlcid["stickyban"] = "'[sanitizeSQL(ckey)]'"
			sqlcid["matched_cid"] = "'[sanitizeSQL(cid)]'"
			sqlcids[++sqlcids.len] = sqlcid

	if (ban["IP"])
		var/list/ips = splittext(ban["IP"], ",")
		for (var/ip in ips)
			var/list/sqlip = list()
			sqlip["stickyban"] = "'[sanitizeSQL(ckey)]'"
			sqlip["matched_ip"] = "'[sanitizeSQL(ip)]'"
			sqlips[++sqlips.len] = sqlip

	if (length(sqlckeys))
		SSdbcore.MassInsert(format_table_name("stickyban_matched_ckey"), sqlckeys, FALSE, TRUE)

	if (length(sqlcids))
		SSdbcore.MassInsert(format_table_name("stickyban_matched_cid"), sqlcids, FALSE, TRUE)

	if (length(sqlips))
		SSdbcore.MassInsert(format_table_name("stickyban_matched_ip"), sqlips, FALSE, TRUE)


	return TRUE
