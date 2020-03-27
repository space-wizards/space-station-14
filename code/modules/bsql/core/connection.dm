/datum/BSQL_Connection
	var/id
	var/connection_type

BSQL_PROTECT_DATUM(/datum/BSQL_Connection)

/datum/BSQL_Connection/New(connection_type, asyncTimeout, blockingTimeout, threadLimit)
	if(asyncTimeout == null)
		asyncTimeout = BSQL_DEFAULT_TIMEOUT
	if(blockingTimeout == null)
		blockingTimeout = asyncTimeout
	if(threadLimit == null)
		threadLimit = BSQL_DEFAULT_THREAD_LIMIT

	src.connection_type = connection_type

	world._BSQL_InitCheck(src)

	var/error = world._BSQL_Internal_Call("CreateConnection", connection_type, "[asyncTimeout]", "[blockingTimeout]", "[threadLimit]")
	if(error)
		BSQL_ERROR(error)
		return

	id = world._BSQL_Internal_Call("GetConnection")
	if(!id)
		BSQL_ERROR("BSQL library failed to provide connect operation for connection id [id]([connection_type])!")

BSQL_DEL_PROC(/datum/BSQL_Connection)
	var/error
	if(id)
		error = world._BSQL_Internal_Call("ReleaseConnection", id)
	. = ..()
	if(error)
		BSQL_ERROR(error)

/datum/BSQL_Connection/BeginConnect(ipaddress, port, username, password, database)
	var/error = world._BSQL_Internal_Call("OpenConnection", id, ipaddress, "[port]", username, password, database)
	if(error)
		BSQL_ERROR(error)
		return

	var/op_id = world._BSQL_Internal_Call("GetOperation")
	if(!op_id)
		BSQL_ERROR("Library failed to provide connect operation for connection id [id]([connection_type])!")
		return

	return new /datum/BSQL_Operation(src, op_id)


/datum/BSQL_Connection/BeginQuery(query)
	var/error = world._BSQL_Internal_Call("NewQuery", id, query)
	if(error)
		BSQL_ERROR(error)
		return

	var/op_id = world._BSQL_Internal_Call("GetOperation")
	if(!op_id)
		BSQL_ERROR("Library failed to provide query operation for connection id [id]([connection_type])!")
		return

	return new /datum/BSQL_Operation/Query(src, op_id)
	
/datum/BSQL_Connection/Quote(str)
	if(!str)
		return null;
	. = world._BSQL_Internal_Call("QuoteString", id, "[str]")
	if(!.)
		BSQL_ERROR("Library failed to provide quote for [str]!")
