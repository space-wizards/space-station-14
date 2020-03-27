/client/proc/edit_admin_permissions()
	set category = "Admin"
	set name = "Permissions Panel"
	set desc = "Edit admin permissions"
	if(!check_rights(R_PERMISSIONS))
		return
	usr.client.holder.edit_admin_permissions()

/datum/admins/proc/edit_admin_permissions(action, target, operation, page)
	if(!check_rights(R_PERMISSIONS))
		return
	var/list/output = list("<link rel='stylesheet' type='text/css' href='panels.css'><a href='?_src_=holder;[HrefToken()];editrightsbrowser=1'>\[Permissions\]</a>")
	if(action)
		output += " | <a href='?_src_=holder;[HrefToken()];editrightsbrowserlog=1;editrightspage=0'>\[Log\]</a> | <a href='?_src_=holder;[HrefToken()];editrightsbrowsermanage=1'>\[Management\]</a><hr style='background:#000000; border:0; height:3px'>"
	else
		output += "<br><a href='?_src_=holder;[HrefToken()];editrightsbrowserlog=1;editrightspage=0'>\[Log\]</a><br><a href='?_src_=holder;[HrefToken()];editrightsbrowsermanage=1'>\[Management\]</a>"
	if(action == 1)
		var/list/searchlist = list(" WHERE ")
		if(target)
			searchlist += "ckey = '[sanitizeSQL(target)]'"
		if(operation)
			if(target)
				searchlist += " AND "
			searchlist += "operation = '[sanitizeSQL(operation)]'"
		var/search
		if(searchlist.len > 1)
			search = searchlist.Join("")
		var/logcount = 0
		var/logssperpage = 20
		var/pagecount = 0
		page = text2num(page)
		var/datum/DBQuery/query_count_admin_logs = SSdbcore.NewQuery("SELECT COUNT(id) FROM [format_table_name("admin_log")][search]")
		if(!query_count_admin_logs.warn_execute())
			qdel(query_count_admin_logs)
			return
		if(query_count_admin_logs.NextRow())
			logcount = text2num(query_count_admin_logs.item[1])
		qdel(query_count_admin_logs)
		if(logcount > logssperpage)
			output += "<br><b>Page: </b>"
			while(logcount > 0)
				output += "|<a href='?_src_=holder;[HrefToken()];editrightsbrowserlog=1;editrightstarget=[target];editrightsoperation=[operation];editrightspage=[pagecount]'>[pagecount == page ? "<b>\[[pagecount]\]</b>" : "\[[pagecount]\]"]</a>"
				logcount -= logssperpage
				pagecount++
			output += "|"
		var/limit = " LIMIT [logssperpage * page], [logssperpage]"
		var/datum/DBQuery/query_search_admin_logs = SSdbcore.NewQuery("SELECT datetime, round_id, IFNULL((SELECT byond_key FROM [format_table_name("player")] WHERE ckey = adminckey), adminckey), operation, IF(ckey IS NULL, target, byond_key), log FROM [format_table_name("admin_log")] LEFT JOIN [format_table_name("player")] ON target = ckey[search] ORDER BY datetime DESC[limit]")
		if(!query_search_admin_logs.warn_execute())
			qdel(query_search_admin_logs)
			return
		while(query_search_admin_logs.NextRow())
			var/datetime = query_search_admin_logs.item[1]
			var/round_id = query_search_admin_logs.item[2]
			var/admin_key  = query_search_admin_logs.item[3]
			operation = query_search_admin_logs.item[4]
			target = query_search_admin_logs.item[5]
			var/log = query_search_admin_logs.item[6]
			output += "<p style='margin:0px'><b>[datetime] | Round ID [round_id] | Admin [admin_key] | Operation [operation] on [target]</b><br>[log]</p><hr style='background:#000000; border:0; height:3px'>"
		qdel(query_search_admin_logs)
	if(action == 2)
		output += "<h3>Admin ckeys with invalid ranks</h3>"
		var/datum/DBQuery/query_check_admin_errors = SSdbcore.NewQuery("SELECT IFNULL((SELECT byond_key FROM [format_table_name("player")] WHERE [format_table_name("player")].ckey = [format_table_name("admin")].ckey), ckey), [format_table_name("admin")].`rank` FROM [format_table_name("admin")] LEFT JOIN [format_table_name("admin_ranks")] ON [format_table_name("admin_ranks")].`rank` = [format_table_name("admin")].`rank` WHERE [format_table_name("admin_ranks")].`rank` IS NULL")
		if(!query_check_admin_errors.warn_execute())
			qdel(query_check_admin_errors)
			return
		while(query_check_admin_errors.NextRow())
			var/admin_key = query_check_admin_errors.item[1]
			var/admin_rank = query_check_admin_errors.item[2]
			output += "[admin_key] has non-existent rank [admin_rank] | <a href='?_src_=holder;[HrefToken()];editrightsbrowsermanage=1;editrightschange=[admin_key]'>\[Change Rank\]</a> | <a href='?_src_=holder;[HrefToken()];editrightsbrowsermanage=1;editrightsremove=[admin_key]'>\[Remove\]</a>"
			output += "<hr style='background:#000000; border:0; height:1px'>"
		qdel(query_check_admin_errors)
		output += "<h3>Unused ranks</h3>"
		var/datum/DBQuery/query_check_unused_rank = SSdbcore.NewQuery("SELECT [format_table_name("admin_ranks")].`rank`, flags, exclude_flags, can_edit_flags FROM [format_table_name("admin_ranks")] LEFT JOIN [format_table_name("admin")] ON [format_table_name("admin")].`rank` = [format_table_name("admin_ranks")].`rank` WHERE [format_table_name("admin")].`rank` IS NULL")
		if(!query_check_unused_rank.warn_execute())
			qdel(query_check_unused_rank)
			return
		while(query_check_unused_rank.NextRow())
			var/admin_rank = query_check_unused_rank.item[1]
			output += {"Rank [admin_rank] is not held by any admin | <a href='?_src_=holder;[HrefToken()];editrightsbrowsermanage=1;editrightsremoverank=[admin_rank]'>\[Remove\]</a>
			<br>Permissions: [rights2text(text2num(query_check_unused_rank.item[2])," ")]
			<br>Denied: [rights2text(text2num(query_check_unused_rank.item[3])," ", "-")]
			<br>Allowed to edit: [rights2text(text2num(query_check_unused_rank.item[4])," ", "*")]
			<hr style='background:#000000; border:0; height:1px'>"}
		qdel(query_check_unused_rank)
	else if(!action)
		output += {"<head>
		<title>Permissions Panel</title>
		<script type='text/javascript' src='search.js'></script>
		</head>
		<body onload='selectTextField();updateSearch();'>
		<div id='main'><table id='searchable' cellspacing='0'>
		<tr class='title'>
		<th style='width:150px;'>CKEY <a class='small' href='?src=[REF(src)];[HrefToken()];editrights=add'>\[+\]</a></th>
		<th style='width:125px;'>RANK</th>
		<th style='width:40%;'>PERMISSIONS</th>
		<th style='width:20%;'>DENIED</th>
		<th style='width:40%;'>ALLOWED TO EDIT</th>
		</tr>
		"}
		for(var/adm_ckey in GLOB.admin_datums+GLOB.deadmins)
			var/datum/admins/D = GLOB.admin_datums[adm_ckey]
			if(!D)
				D = GLOB.deadmins[adm_ckey]
				if (!D)
					continue
			var/deadminlink = ""
			if(D.owner)
				adm_ckey = D.owner.key
			if (D.deadmined)
				deadminlink = " <a class='small' href='?src=[REF(src)];[HrefToken()];editrights=activate;key=[adm_ckey]'>\[RA\]</a>"
			else
				deadminlink = " <a class='small' href='?src=[REF(src)];[HrefToken()];editrights=deactivate;key=[adm_ckey]'>\[DA\]</a>"
			output += "<tr>"
			output += "<td style='text-align:center;'>[adm_ckey]<br>[deadminlink]<a class='small' href='?src=[REF(src)];[HrefToken()];editrights=remove;key=[adm_ckey]'>\[-\]</a><a class='small' href='?src=[REF(src)];[HrefToken()];editrights=sync;key=[adm_ckey]'>\[SYNC TGDB\]</a></td>"
			output += "<td><a href='?src=[REF(src)];[HrefToken()];editrights=rank;key=[adm_ckey]'>[D.rank.name]</a></td>"
			output += "<td><a class='small' href='?src=[REF(src)];[HrefToken()];editrights=permissions;key=[adm_ckey]'>[rights2text(D.rank.include_rights," ")]</a></td>"
			output += "<td><a class='small' href='?src=[REF(src)];[HrefToken()];editrights=permissions;key=[adm_ckey]'>[rights2text(D.rank.exclude_rights," ", "-")]</a></td>"
			output += "<td><a class='small' href='?src=[REF(src)];[HrefToken()];editrights=permissions;key=[adm_ckey]'>[rights2text(D.rank.can_edit_rights," ", "*")]</a></td>"
			output += "</tr>"
		output += "</table></div><div id='top'><b>Search:</b> <input type='text' id='filter' value='' style='width:70%;' onkeyup='updateSearch();'></div></body>"
	if(QDELETED(usr))
		return
	usr << browse("<!DOCTYPE html><html>[jointext(output, "")]</html>","window=editrights;size=1000x650")

/datum/admins/proc/edit_rights_topic(list/href_list)
	if(!check_rights(R_PERMISSIONS))
		message_admins("[key_name_admin(usr)] attempted to edit admin permissions without sufficient rights.")
		log_admin("[key_name(usr)] attempted to edit admin permissions without sufficient rights.")
		return
	if(IsAdminAdvancedProcCall())
		to_chat(usr, "<span class='admin prefix'>Admin Edit blocked: Advanced ProcCall detected.</span>")
		return
	var/datum/asset/permissions_assets = get_asset_datum(/datum/asset/simple/permissions)
	permissions_assets.send(src)
	var/admin_key = href_list["key"]
	var/admin_ckey = ckey(admin_key)
	var/datum/admins/D = GLOB.admin_datums[admin_ckey]
	var/use_db
	var/task = href_list["editrights"]
	var/skip
	var/legacy_only
	if(task == "activate" || task == "deactivate" || task == "sync")
		skip = TRUE
	if(!CONFIG_GET(flag/admin_legacy_system) && CONFIG_GET(flag/protect_legacy_admins) && task == "rank")
		if(admin_ckey in GLOB.protected_admins)
			to_chat(usr, "<span class='admin prefix'>Editing the rank of this admin is blocked by server configuration.</span>")
			return
	if(!CONFIG_GET(flag/admin_legacy_system) && CONFIG_GET(flag/protect_legacy_ranks) && task == "permissions")
		if(D.rank in GLOB.protected_ranks)
			to_chat(usr, "<span class='admin prefix'>Editing the flags of this rank is blocked by server configuration.</span>")
			return
	if(CONFIG_GET(flag/load_legacy_ranks_only) && (task == "add" || task == "rank" || task == "permissions"))
		to_chat(usr, "<span class='admin prefix'>Database rank loading is disabled, only temporary changes can be made to a rank's permissions and permanently creating a new rank is blocked.</span>")
		legacy_only = TRUE
	if(check_rights(R_DBRANKS, FALSE))
		if(!skip)
			if(!SSdbcore.Connect())
				to_chat(usr, "<span class='danger'>Unable to connect to database, changes are temporary only.</span>")
				use_db = FALSE
			else
				use_db = alert("Permanent changes are saved to the database for future rounds, temporary changes will affect only the current round", "Permanent or Temporary?", "Permanent", "Temporary", "Cancel")
				if(use_db == "Cancel")
					return
				if(use_db == "Permanent")
					use_db = TRUE
					admin_ckey = sanitizeSQL(admin_ckey)
				else
					use_db = FALSE
			if(QDELETED(usr))
				return
	if(task != "add")
		D = GLOB.admin_datums[admin_ckey]
		if(!D)
			D = GLOB.deadmins[admin_ckey]
		if(!D)
			return
		if((task != "sync") && !check_if_greater_rights_than_holder(D))
			message_admins("[key_name_admin(usr)] attempted to change the rank of [admin_key] without sufficient rights.")
			log_admin("[key_name(usr)] attempted to change the rank of [admin_key] without sufficient rights.")
			return
	switch(task)
		if("add")
			admin_ckey = add_admin(admin_ckey, admin_key, use_db)
			if(!admin_ckey)
				return
			change_admin_rank(admin_ckey, admin_key, use_db, null, legacy_only)
		if("remove")
			remove_admin(admin_ckey, admin_key, use_db, D)
		if("rank")
			change_admin_rank(admin_ckey, admin_key, use_db, D, legacy_only)
		if("permissions")
			change_admin_flags(admin_ckey, admin_key, use_db, D, legacy_only)
		if("activate")
			force_readmin(admin_key, D)
		if("deactivate")
			force_deadmin(admin_key, D)
		if("sync")
			sync_lastadminrank(admin_ckey, admin_key, D)
	edit_admin_permissions()

/datum/admins/proc/add_admin(admin_ckey, admin_key, use_db)
	if(admin_ckey)
		. = admin_ckey
	else
		admin_key = input("New admin's key","Admin key") as text|null
		. = ckey(admin_key)
	if(!.)
		return FALSE
	if(!admin_ckey && (. in GLOB.admin_datums+GLOB.deadmins))
		to_chat(usr, "<span class='danger'>[admin_key] is already an admin.</span>")
		return FALSE
	if(use_db)
		. = sanitizeSQL(.)
		//if an admin exists without a datum they won't be caught by the above
		var/datum/DBQuery/query_admin_in_db = SSdbcore.NewQuery("SELECT 1 FROM [format_table_name("admin")] WHERE ckey = '[.]'")
		if(!query_admin_in_db.warn_execute())
			qdel(query_admin_in_db)
			return FALSE
		if(query_admin_in_db.NextRow())
			qdel(query_admin_in_db)
			to_chat(usr, "<span class='danger'>[admin_key] already listed in admin database. Check the Management tab if they don't appear in the list of admins.</span>")
			return FALSE
		qdel(query_admin_in_db)
		var/datum/DBQuery/query_add_admin = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin")] (ckey, `rank`) VALUES ('[.]', 'NEW ADMIN')")
		if(!query_add_admin.warn_execute())
			qdel(query_add_admin)
			return FALSE
		qdel(query_add_admin)
		var/datum/DBQuery/query_add_admin_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'add admin', '[.]', 'New admin added: [.]')")
		if(!query_add_admin_log.warn_execute())
			qdel(query_add_admin_log)
			return FALSE
		qdel(query_add_admin_log)

/datum/admins/proc/remove_admin(admin_ckey, admin_key, use_db, datum/admins/D)
	if(alert("Are you sure you want to remove [admin_ckey]?","Confirm Removal","Do it","Cancel") == "Do it")
		GLOB.admin_datums -= admin_ckey
		GLOB.deadmins -= admin_ckey
		if(D)
			D.disassociate()
		var/m1 = "[key_name_admin(usr)] removed [admin_key] from the admins list [use_db ? "permanently" : "temporarily"]"
		var/m2 = "[key_name(usr)] removed [admin_key] from the admins list [use_db ? "permanently" : "temporarily"]"
		if(use_db)
			var/datum/DBQuery/query_add_rank = SSdbcore.NewQuery("DELETE FROM [format_table_name("admin")] WHERE ckey = '[admin_ckey]'")
			if(!query_add_rank.warn_execute())
				qdel(query_add_rank)
				return
			qdel(query_add_rank)
			var/datum/DBQuery/query_add_rank_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'remove admin', '[admin_ckey]', 'Admin removed: [admin_ckey]')")
			if(!query_add_rank_log.warn_execute())
				qdel(query_add_rank_log)
				return
			qdel(query_add_rank_log)
			sync_lastadminrank(admin_ckey, admin_key)
		message_admins(m1)
		log_admin(m2)

/datum/admins/proc/force_readmin(admin_key, datum/admins/D)
	if(!D || !D.deadmined)
		return
	D.activate()
	message_admins("[key_name_admin(usr)] forcefully readmined [admin_key]")
	log_admin("[key_name(usr)] forcefully readmined [admin_key]")

/datum/admins/proc/force_deadmin(admin_key, datum/admins/D)
	if(!D || D.deadmined)
		return
	message_admins("[key_name_admin(usr)] forcefully deadmined [admin_key]")
	log_admin("[key_name(usr)] forcefully deadmined [admin_key]")
	D.deactivate() //after logs so the deadmined admin can see the message.

/datum/admins/proc/auto_deadmin()
	to_chat(owner, "<span class='interface'>You are now a normal player.</span>")
	var/old_owner = owner
	deactivate()
	message_admins("[old_owner] deadmined via auto-deadmin config.")
	log_admin("[old_owner] deadmined via auto-deadmin config.")
	return TRUE

/datum/admins/proc/change_admin_rank(admin_ckey, admin_key, use_db, datum/admins/D, legacy_only)
	var/datum/admin_rank/R
	var/list/rank_names = list()
	if(!use_db || (use_db && !legacy_only))
		rank_names += "*New Rank*"
	for(R in GLOB.admin_ranks)
		if((R.rights & usr.client.holder.rank.can_edit_rights) == R.rights)
			rank_names[R.name] = R
	var/new_rank = input("Please select a rank", "New rank") as null|anything in rank_names
	if(new_rank == "*New Rank*")
		new_rank = input("Please input a new rank", "New custom rank") as text|null
	if(!new_rank)
		return
	R = rank_names[new_rank]
	if(!R) //rank with that name doesn't exist yet - make it
		if(D)
			R = new(new_rank, D.rank.rights) //duplicate our previous admin_rank but with a new name
		else
			R = new(new_rank) //blank new admin_rank
		GLOB.admin_ranks += R
	var/m1 = "[key_name_admin(usr)] edited the admin rank of [admin_key] to [new_rank] [use_db ? "permanently" : "temporarily"]"
	var/m2 = "[key_name(usr)] edited the admin rank of [admin_key] to [new_rank] [use_db ? "permanently" : "temporarily"]"
	if(use_db)
		new_rank = sanitizeSQL(new_rank)
		//if a player was tempminned before having a permanent change made to their rank they won't yet be in the db
		var/old_rank
		var/datum/DBQuery/query_admin_in_db = SSdbcore.NewQuery("SELECT `rank` FROM [format_table_name("admin")] WHERE ckey = '[admin_ckey]'")
		if(!query_admin_in_db.warn_execute())
			qdel(query_admin_in_db)
			return
		if(!query_admin_in_db.NextRow())
			add_admin(admin_ckey, admin_key, TRUE)
			old_rank = "NEW ADMIN"
		else
			old_rank = query_admin_in_db.item[1]
		qdel(query_admin_in_db)
		//similarly if a temp rank is created it won't be in the db if someone is permanently changed to it
		var/datum/DBQuery/query_rank_in_db = SSdbcore.NewQuery("SELECT 1 FROM [format_table_name("admin_ranks")] WHERE `rank` = '[new_rank]'")
		if(!query_rank_in_db.warn_execute())
			qdel(query_rank_in_db)
			return
		if(!query_rank_in_db.NextRow())
			QDEL_NULL(query_rank_in_db)
			var/datum/DBQuery/query_add_rank = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_ranks")] (`rank`, flags, exclude_flags, can_edit_flags) VALUES ('[new_rank]', '0', '0', '0')")
			if(!query_add_rank.warn_execute())
				qdel(query_add_rank)
				return
			qdel(query_add_rank)
			var/datum/DBQuery/query_add_rank_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'add rank', '[new_rank]', 'New rank added: [new_rank]')")
			if(!query_add_rank_log.warn_execute())
				qdel(query_add_rank_log)
				return
			qdel(query_add_rank_log)
		qdel(query_rank_in_db)
		var/datum/DBQuery/query_change_rank = SSdbcore.NewQuery("UPDATE [format_table_name("admin")] SET `rank` = '[new_rank]' WHERE ckey = '[admin_ckey]'")
		if(!query_change_rank.warn_execute())
			qdel(query_change_rank)
			return
		qdel(query_change_rank)
		var/datum/DBQuery/query_change_rank_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'change admin rank', '[admin_ckey]', 'Rank of [admin_ckey] changed from [old_rank] to [new_rank]')")
		if(!query_change_rank_log.warn_execute())
			qdel(query_change_rank_log)
			return
		qdel(query_change_rank_log)
	if(D) //they were previously an admin
		D.disassociate() //existing admin needs to be disassociated
		D.rank = R //set the admin_rank as our rank
		var/client/C = GLOB.directory[admin_ckey]
		D.associate(C)
	else
		D = new(R, admin_ckey, TRUE) //new admin
	message_admins(m1)
	log_admin(m2)

/datum/admins/proc/change_admin_flags(admin_ckey, admin_key, use_db, datum/admins/D, legacy_only)
	var/new_flags = input_bitfield(usr, "Include permission flags<br>[use_db ? "This will affect ALL admins with this rank." : "This will affect only the current admin [admin_key]"]", "admin_flags", D.rank.include_rights, 350, 590, allowed_edit_list = usr.client.holder.rank.can_edit_rights)
	if(isnull(new_flags))
		return
	var/new_exclude_flags = input_bitfield(usr, "Exclude permission flags<br>Flags enabled here will be removed from a rank.<br>Note these take precedence over included flags.<br>[use_db ? "This will affect ALL admins with this rank." : "This will affect only the current admin [admin_key]"]", "admin_flags", D.rank.exclude_rights, 350, 670, "red", usr.client.holder.rank.can_edit_rights)
	if(isnull(new_exclude_flags))
		return
	var/new_can_edit_flags = input_bitfield(usr, "Editable permission flags<br>These are the flags this rank is allowed to edit if they have access to the permissions panel.<br>They will be unable to modify admins to a rank that has a flag not included here.<br>[use_db ? "This will affect ALL admins with this rank." : "This will affect only the current admin [admin_key]"]", "admin_flags", D.rank.can_edit_rights, 350, 710, allowed_edit_list = usr.client.holder.rank.can_edit_rights)
	if(isnull(new_can_edit_flags))
		return
	var/m1 = "[key_name_admin(usr)] edited the permissions of [use_db ? " rank [D.rank.name] permanently" : "[admin_key] temporarily"]"
	var/m2 = "[key_name(usr)] edited the permissions of [use_db ? " rank [D.rank.name] permanently" : "[admin_key] temporarily"]"
	if(use_db || legacy_only)
		var/rank_name = sanitizeSQL(D.rank.name)
		var/old_flags
		var/old_exclude_flags
		var/old_can_edit_flags
		var/datum/DBQuery/query_get_rank_flags = SSdbcore.NewQuery("SELECT flags, exclude_flags, can_edit_flags FROM [format_table_name("admin_ranks")] WHERE `rank` = '[rank_name]'")
		if(!query_get_rank_flags.warn_execute())
			qdel(query_get_rank_flags)
			return
		if(query_get_rank_flags.NextRow())
			old_flags = text2num(query_get_rank_flags.item[1])
			old_exclude_flags = text2num(query_get_rank_flags.item[2])
			old_can_edit_flags = text2num(query_get_rank_flags.item[3])
		qdel(query_get_rank_flags)
		var/datum/DBQuery/query_change_rank_flags = SSdbcore.NewQuery("UPDATE [format_table_name("admin_ranks")] SET flags = '[new_flags]', exclude_flags = '[new_exclude_flags]', can_edit_flags = '[new_can_edit_flags]' WHERE `rank` = '[rank_name]'")
		if(!query_change_rank_flags.warn_execute())
			qdel(query_change_rank_flags)
			return
		qdel(query_change_rank_flags)
		var/datum/DBQuery/query_change_rank_flags_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'change rank flags', '[rank_name]', 'Permissions of [rank_name] changed from[rights2text(old_flags," ")][rights2text(old_exclude_flags," ", "-")][rights2text(old_can_edit_flags," ", "*")] to[rights2text(new_flags," ")][rights2text(new_exclude_flags," ", "-")][rights2text(new_can_edit_flags," ", "*")]')")
		if(!query_change_rank_flags_log.warn_execute())
			qdel(query_change_rank_flags_log)
			return
		qdel(query_change_rank_flags_log)
		for(var/datum/admin_rank/R in GLOB.admin_ranks)
			if(R.name != D.rank.name)
				continue
			R.rights = new_flags &= ~new_exclude_flags
			R.exclude_rights = new_exclude_flags
			R.include_rights = new_flags
			R.can_edit_rights = new_can_edit_flags
		for(var/i in GLOB.admin_datums+GLOB.deadmins)
			var/datum/admins/A = GLOB.admin_datums[i]
			if(!A)
				A = GLOB.deadmins[i]
				if (!A)
					continue
			if(A.rank.name != D.rank.name)
				continue
			var/client/C = GLOB.directory[A.target]
			A.disassociate()
			A.associate(C)
	else
		D.disassociate()
		if(!findtext(D.rank.name, "([admin_ckey])")) //not a modified subrank, need to duplicate the admin_rank datum to prevent modifying others too
			D.rank = new("[D.rank.name]([admin_ckey])", new_flags, new_exclude_flags, new_can_edit_flags) //duplicate our previous admin_rank but with a new name
			//we don't add this clone to the admin_ranks list, as it is unique to that ckey
		else
			D.rank.rights = new_flags &= ~new_exclude_flags
			D.rank.include_rights = new_flags
			D.rank.exclude_rights = new_exclude_flags
			D.rank.can_edit_rights = new_can_edit_flags
		var/client/C = GLOB.directory[admin_ckey] //find the client with the specified ckey (if they are logged in)
		D.associate(C) //link up with the client and add verbs
	message_admins(m1)
	log_admin(m2)

/datum/admins/proc/remove_rank(admin_rank)
	if(!admin_rank)
		return
	for(var/datum/admin_rank/R in GLOB.admin_ranks)
		if(R.name == admin_rank && (!(R.rights & usr.client.holder.rank.can_edit_rights) == R.rights))
			to_chat(usr, "<span class='admin prefix'>You don't have edit rights to all the rights this rank has, rank deletion not permitted.</span>")
			return
	if(!CONFIG_GET(flag/admin_legacy_system) && CONFIG_GET(flag/protect_legacy_ranks) && (admin_rank in GLOB.protected_ranks))
		to_chat(usr, "<span class='admin prefix'>Deletion of protected ranks is not permitted, it must be removed from admin_ranks.txt.</span>")
		return
	if(CONFIG_GET(flag/load_legacy_ranks_only))
		to_chat(usr, "<span class='admin prefix'>Rank deletion not permitted while database rank loading is disabled.</span>")
		return
	admin_rank = sanitizeSQL(admin_rank)
	var/datum/DBQuery/query_admins_with_rank = SSdbcore.NewQuery("SELECT 1 FROM [format_table_name("admin")] WHERE `rank` = '[admin_rank]'")
	if(!query_admins_with_rank.warn_execute())
		qdel(query_admins_with_rank)
		return
	if(query_admins_with_rank.NextRow())
		qdel(query_admins_with_rank)
		to_chat(usr, "<span class='danger'>Error: Rank deletion attempted while rank still used; Tell a coder, this shouldn't happen.</span>")
		return
	qdel(query_admins_with_rank)
	if(alert("Are you sure you want to remove [admin_rank]?","Confirm Removal","Do it","Cancel") == "Do it")
		var/m1 = "[key_name_admin(usr)] removed rank [admin_rank] permanently"
		var/m2 = "[key_name(usr)] removed rank [admin_rank] permanently"
		var/datum/DBQuery/query_add_rank = SSdbcore.NewQuery("DELETE FROM [format_table_name("admin_ranks")] WHERE `rank` = '[admin_rank]'")
		if(!query_add_rank.warn_execute())
			qdel(query_add_rank)
			return
		qdel(query_add_rank)
		var/datum/DBQuery/query_add_rank_log = SSdbcore.NewQuery("INSERT INTO [format_table_name("admin_log")] (datetime, round_id, adminckey, adminip, operation, target, log) VALUES ('[SQLtime()]', '[GLOB.round_id]', '[sanitizeSQL(usr.ckey)]', INET_ATON('[sanitizeSQL(usr.client.address)]'), 'remove rank', '[admin_rank]', 'Rank removed: [admin_rank]')")
		if(!query_add_rank_log.warn_execute())
			qdel(query_add_rank_log)
			return
		qdel(query_add_rank_log)
		message_admins(m1)
		log_admin(m2)

/datum/admins/proc/sync_lastadminrank(admin_ckey, admin_key, datum/admins/D)
	var/sqlrank = "Player"
	if (D)
		sqlrank = sanitizeSQL(D.rank.name)
	admin_ckey = sanitizeSQL(admin_ckey)
	var/datum/DBQuery/query_sync_lastadminrank = SSdbcore.NewQuery("UPDATE [format_table_name("player")] SET lastadminrank = '[sqlrank]' WHERE ckey = '[admin_ckey]'")
	if(!query_sync_lastadminrank.warn_execute())
		qdel(query_sync_lastadminrank)
		return
	qdel(query_sync_lastadminrank)
	to_chat(usr, "<span class='admin'>Sync of [admin_key] successful.</span>")
