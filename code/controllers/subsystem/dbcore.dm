SUBSYSTEM_DEF(dbcore)
	name = "Database"
	flags = SS_BACKGROUND
	wait = 1 MINUTES
	init_order = INIT_ORDER_DBCORE
	var/const/FAILED_DB_CONNECTION_CUTOFF = 5
	var/failed_connection_timeout = 0

	var/schema_mismatch = 0
	var/db_minor = 0
	var/db_major = 0
	var/failed_connections = 0

	var/last_error
	var/list/active_queries = list()

	var/datum/BSQL_Connection/connection
	var/datum/BSQL_Operation/connectOperation

/datum/controller/subsystem/dbcore/Initialize()
	//We send warnings to the admins during subsystem init, as the clients will be New'd and messages
	//will queue properly with goonchat
	switch(schema_mismatch)
		if(1)
			message_admins("Database schema ([db_major].[db_minor]) doesn't match the latest schema version ([DB_MAJOR_VERSION].[DB_MINOR_VERSION]), this may lead to undefined behaviour or errors")
		if(2)
			message_admins("Could not get schema version from database")

	return ..()

/datum/controller/subsystem/dbcore/fire()
	for(var/I in active_queries)
		var/datum/DBQuery/Q = I
		if(world.time - Q.last_activity_time > (5 MINUTES))
			message_admins("Found undeleted query, please check the server logs and notify coders.")
			log_sql("Undeleted query: \"[Q.sql]\" LA: [Q.last_activity] LAT: [Q.last_activity_time]")
			qdel(Q)
		if(MC_TICK_CHECK)
			return

/datum/controller/subsystem/dbcore/Recover()
	connection = SSdbcore.connection
	connectOperation = SSdbcore.connectOperation

/datum/controller/subsystem/dbcore/Shutdown()
	//This is as close as we can get to the true round end before Disconnect() without changing where it's called, defeating the reason this is a subsystem
	if(SSdbcore.Connect())
		var/datum/DBQuery/query_round_shutdown = SSdbcore.NewQuery("UPDATE [format_table_name("round")] SET shutdown_datetime = Now(), end_state = '[sanitizeSQL(SSticker.end_state)]' WHERE id = [GLOB.round_id]")
		query_round_shutdown.Execute()
		qdel(query_round_shutdown)
	if(IsConnected())
		Disconnect()
	world.BSQL_Shutdown()

//nu
/datum/controller/subsystem/dbcore/can_vv_get(var_name)
	return var_name != NAMEOF(src, connection) && var_name != NAMEOF(src, active_queries) && var_name != NAMEOF(src, connectOperation) && ..()

/datum/controller/subsystem/dbcore/vv_edit_var(var_name, var_value)
	if(var_name == NAMEOF(src, connection) || var_name == NAMEOF(src, connectOperation))
		return FALSE
	return ..()

/datum/controller/subsystem/dbcore/proc/Connect()
	if(IsConnected())
		return TRUE

	if(failed_connection_timeout <= world.time) //it's been more than 5 seconds since we failed to connect, reset the counter
		failed_connections = 0

	if(failed_connections > FAILED_DB_CONNECTION_CUTOFF)	//If it failed to establish a connection more than 5 times in a row, don't bother attempting to connect for 5 seconds.
		failed_connection_timeout = world.time + 50
		return FALSE

	if(!CONFIG_GET(flag/sql_enabled))
		return FALSE

	var/user = CONFIG_GET(string/feedback_login)
	var/pass = CONFIG_GET(string/feedback_password)
	var/db = CONFIG_GET(string/feedback_database)
	var/address = CONFIG_GET(string/address)
	var/port = CONFIG_GET(number/port)

	connection = new /datum/BSQL_Connection(BSQL_CONNECTION_TYPE_MARIADB, CONFIG_GET(number/async_query_timeout), CONFIG_GET(number/blocking_query_timeout), CONFIG_GET(number/bsql_thread_limit))
	var/error
	if(QDELETED(connection))
		connection = null
		error = last_error
	else
		SSdbcore.last_error = null
		connectOperation = connection.BeginConnect(address, port, user, pass, db)
		if(SSdbcore.last_error)
			CRASH(SSdbcore.last_error)
		UNTIL(connectOperation.IsComplete())
		error = connectOperation.GetError()
	. = !error
	if (!.)
		last_error = error
		log_sql("Connect() failed | [error]")
		++failed_connections
		QDEL_NULL(connection)
		QDEL_NULL(connectOperation)

/datum/controller/subsystem/dbcore/proc/CheckSchemaVersion()
	if(CONFIG_GET(flag/sql_enabled))
		if(Connect())
			log_world("Database connection established.")
			var/datum/DBQuery/query_db_version = NewQuery("SELECT major, minor FROM [format_table_name("schema_revision")] ORDER BY date DESC LIMIT 1")
			query_db_version.Execute()
			if(query_db_version.NextRow())
				db_major = text2num(query_db_version.item[1])
				db_minor = text2num(query_db_version.item[2])
				if(db_major != DB_MAJOR_VERSION || db_minor != DB_MINOR_VERSION)
					schema_mismatch = 1 // flag admin message about mismatch
					log_sql("Database schema ([db_major].[db_minor]) doesn't match the latest schema version ([DB_MAJOR_VERSION].[DB_MINOR_VERSION]), this may lead to undefined behaviour or errors")
			else
				schema_mismatch = 2 //flag admin message about no schema version
				log_sql("Could not get schema version from database")
			qdel(query_db_version)
		else
			log_sql("Your server failed to establish a connection with the database.")
	else
		log_sql("Database is not enabled in configuration.")

/datum/controller/subsystem/dbcore/proc/SetRoundID()
	if(!Connect())
		return
	var/datum/DBQuery/query_round_initialize = SSdbcore.NewQuery("INSERT INTO [format_table_name("round")] (initialize_datetime, server_ip, server_port) VALUES (Now(), INET_ATON(IF('[world.internet_address]' LIKE '', '0', '[world.internet_address]')), '[world.port]')")
	query_round_initialize.Execute(async = FALSE)
	qdel(query_round_initialize)
	var/datum/DBQuery/query_round_last_id = SSdbcore.NewQuery("SELECT LAST_INSERT_ID()")
	query_round_last_id.Execute(async = FALSE)
	if(query_round_last_id.NextRow(async = FALSE))
		GLOB.round_id = query_round_last_id.item[1]
	qdel(query_round_last_id)

/datum/controller/subsystem/dbcore/proc/SetRoundStart()
	if(!Connect())
		return
	var/datum/DBQuery/query_round_start = SSdbcore.NewQuery("UPDATE [format_table_name("round")] SET start_datetime = Now() WHERE id = [GLOB.round_id]")
	query_round_start.Execute()
	qdel(query_round_start)

/datum/controller/subsystem/dbcore/proc/SetRoundEnd()
	if(!Connect())
		return
	var/sql_station_name = sanitizeSQL(station_name())
	var/datum/DBQuery/query_round_end = SSdbcore.NewQuery("UPDATE [format_table_name("round")] SET end_datetime = Now(), game_mode_result = '[sanitizeSQL(SSticker.mode_result)]', station_name = '[sql_station_name]' WHERE id = [GLOB.round_id]")
	query_round_end.Execute()
	qdel(query_round_end)

/datum/controller/subsystem/dbcore/proc/Disconnect()
	failed_connections = 0
	QDEL_NULL(connectOperation)
	QDEL_NULL(connection)

/datum/controller/subsystem/dbcore/proc/IsConnected()
	if(!CONFIG_GET(flag/sql_enabled))
		return FALSE
	//block until any connect operations finish
	var/datum/BSQL_Connection/_connection = connection
	var/datum/BSQL_Operation/op = connectOperation
	UNTIL(QDELETED(_connection) || op.IsComplete())
	return !QDELETED(connection) && !op.GetError()

/datum/controller/subsystem/dbcore/proc/Quote(str)
	if(connection)
		return connection.Quote(str)

/datum/controller/subsystem/dbcore/proc/ErrorMsg()
	if(!CONFIG_GET(flag/sql_enabled))
		return "Database disabled by configuration"
	return last_error

/datum/controller/subsystem/dbcore/proc/ReportError(error)
	last_error = error

/datum/controller/subsystem/dbcore/proc/NewQuery(sql_query)
	if(IsAdminAdvancedProcCall())
		log_admin_private("ERROR: Advanced admin proc call led to sql query: [sql_query]. Query has been blocked")
		message_admins("ERROR: Advanced admin proc call led to sql query. Query has been blocked")
		return FALSE
	return new /datum/DBQuery(sql_query, connection)

/datum/controller/subsystem/dbcore/proc/QuerySelect(list/querys, warn = FALSE, qdel = FALSE)
	if (!islist(querys))
		if (!istype(querys, /datum/DBQuery))
			CRASH("Invalid query passed to QuerySelect: [querys]")
		querys = list(querys)

	for (var/thing in querys)
		var/datum/DBQuery/query = thing
		if (warn)
			INVOKE_ASYNC(query, /datum/DBQuery.proc/warn_execute)
		else
			INVOKE_ASYNC(query, /datum/DBQuery.proc/Execute)

	for (var/thing in querys)
		var/datum/DBQuery/query = thing
		UNTIL(!query.in_progress)
		if (qdel)
			qdel(query)



/*
Takes a list of rows (each row being an associated list of column => value) and inserts them via a single mass query.
Rows missing columns present in other rows will resolve to SQL NULL
You are expected to do your own escaping of the data, and expected to provide your own quotes for strings.
The duplicate_key arg can be true to automatically generate this part of the query
	or set to a string that is appended to the end of the query
Ignore_errors instructes mysql to continue inserting rows if some of them have errors.
	 the erroneous row(s) aren't inserted and there isn't really any way to know why or why errored
Delayed insert mode was removed in mysql 7 and only works with MyISAM type tables,
	It was included because it is still supported in mariadb.
	It does not work with duplicate_key and the mysql server ignores it in those cases
*/
/datum/controller/subsystem/dbcore/proc/MassInsert(table, list/rows, duplicate_key = FALSE, ignore_errors = FALSE, delayed = FALSE, warn = FALSE, async = TRUE)
	if (!table || !rows || !istype(rows))
		return
	var/list/columns = list()
	var/list/sorted_rows = list()

	for (var/list/row in rows)
		var/list/sorted_row = list()
		sorted_row.len = columns.len
		for (var/column in row)
			var/idx = columns[column]
			if (!idx)
				idx = columns.len + 1
				columns[column] = idx
				sorted_row.len = columns.len

			sorted_row[idx] = row[column]
		sorted_rows[++sorted_rows.len] = sorted_row

	if (duplicate_key == TRUE)
		var/list/column_list = list()
		for (var/column in columns)
			column_list += "[column] = VALUES([column])"
		duplicate_key = "ON DUPLICATE KEY UPDATE [column_list.Join(", ")]\n"
	else if (duplicate_key == FALSE)
		duplicate_key = null

	if (ignore_errors)
		ignore_errors = " IGNORE"
	else
		ignore_errors = null

	if (delayed)
		delayed = " DELAYED"
	else
		delayed = null

	var/list/sqlrowlist = list()
	var/len = columns.len
	for (var/list/row in sorted_rows)
		if (length(row) != len)
			row.len = len
		for (var/value in row)
			if (value == null)
				value = "NULL"
		sqlrowlist += "([row.Join(", ")])"

	sqlrowlist = "	[sqlrowlist.Join(",\n	")]"
	var/datum/DBQuery/Query = NewQuery("INSERT[delayed][ignore_errors] INTO [table]\n([columns.Join(", ")])\nVALUES\n[sqlrowlist]\n[duplicate_key]")
	if (warn)
		. = Query.warn_execute(async)
	else
		. = Query.Execute(async)
	qdel(Query)

/datum/DBQuery
	var/sql // The sql query being executed.
	var/list/item  //list of data values populated by NextRow()

	var/last_activity
	var/last_activity_time

	var/last_error
	var/skip_next_is_complete
	var/in_progress
	var/datum/BSQL_Connection/connection
	var/datum/BSQL_Operation/Query/query

/datum/DBQuery/New(sql_query, datum/BSQL_Connection/connection)
	SSdbcore.active_queries[src] = TRUE
	Activity("Created")
	item = list()
	src.connection = connection
	sql = sql_query

/datum/DBQuery/Destroy()
	Close()
	SSdbcore.active_queries -= src
	return ..()

/datum/DBQuery/CanProcCall(proc_name)
	//fuck off kevinz
	return FALSE

/datum/DBQuery/proc/SetQuery(new_sql)
	if(in_progress)
		CRASH("Attempted to set new sql while waiting on active query")
	Close()
	sql = new_sql

/datum/DBQuery/proc/Activity(activity)
	last_activity = activity
	last_activity_time = world.time

/datum/DBQuery/proc/warn_execute(async = TRUE)
	. = Execute(async)
	if(!.)
		to_chat(usr, "<span class='danger'>A SQL error occurred during this operation, check the server logs.</span>")

/datum/DBQuery/proc/Execute(async = TRUE, log_error = TRUE)
	Activity("Execute")
	if(in_progress)
		CRASH("Attempted to start a new query while waiting on the old one")

	if(QDELETED(connection))
		last_error = "No connection!"
		return FALSE

	var/start_time
	var/timed_out
	if(!async)
		start_time = REALTIMEOFDAY
	Close()
	timed_out = run_query(async)
	if(query.GetErrorCode() == 2006) //2006 is the return code for "MySQL server has gone away" time-out error, meaning the connection has been lost to the server (if it's still alive)
		log_sql("Executing query encountered returned a lost database connection (2006).")
		SSdbcore.Disconnect()
		if(SSdbcore.Connect()) //connection was restablished, reattempt the query
			log_sql("Connection restablished")
			timed_out = run_query(async)
		else
			log_sql("Executing query failed to restablish database connection.")
	skip_next_is_complete = TRUE
	var/error = QDELETED(query) ? "Query object deleted!" : query.GetError()
	last_error = error
	. = !error
	if(!. && log_error)
		log_sql("[error] | Query used: [sql]")
	if(!async && timed_out)
		log_query_debug("Query execution started at [start_time]")
		log_query_debug("Query execution ended at [REALTIMEOFDAY]")
		log_query_debug("Slow query timeout detected.")
		log_query_debug("Query used: [sql]")
		slow_query_check()

/datum/DBQuery/proc/run_query(async)
	query = connection.BeginQuery(sql)
	if(!async)
		. = !query.WaitForCompletion()
	else
		in_progress = TRUE
		UNTIL(query.IsComplete())
		in_progress = FALSE

/datum/DBQuery/proc/slow_query_check()
	message_admins("HEY! A database query timed out. Did the server just hang? <a href='?_src_=holder;[HrefToken()];slowquery=yes'>\[YES\]</a>|<a href='?_src_=holder;[HrefToken()];slowquery=no'>\[NO\]</a>")

/datum/DBQuery/proc/NextRow(async = TRUE)
	Activity("NextRow")
	UNTIL(!in_progress)
	if(!skip_next_is_complete)
		if(!async)
			query.WaitForCompletion()
		else
			in_progress = TRUE
			UNTIL(query.IsComplete())
			in_progress = FALSE
	else
		skip_next_is_complete = FALSE

	last_error = query.GetError()
	var/list/results = query.CurrentRow()
	. = results != null

	item.Cut()
	//populate item array
	for(var/I in results)
		item += results[I]

/datum/DBQuery/proc/ErrorMsg()
	return last_error

/datum/DBQuery/proc/Close()
	item.Cut()
	QDEL_NULL(query)

/world/BSQL_Debug(message)
	if(!CONFIG_GET(flag/bsql_debug))
		return

	//strip sensitive stuff
	if(findtext(message, ": OpenConnection("))
		message = "OpenConnection CENSORED"

	log_sql("BSQL_DEBUG: [message]")
