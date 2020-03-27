/datum/BSQL_Operation
	var/datum/BSQL_Connection/connection
	var/id

BSQL_PROTECT_DATUM(/datum/BSQL_Operation)

/datum/BSQL_Operation/New(datum/BSQL_Connection/connection, id)
	src.connection = connection
	src.id = id

BSQL_DEL_PROC(/datum/BSQL_Operation)
	var/error
	if(!BSQL_IS_DELETED(connection))
		error = world._BSQL_Internal_Call("ReleaseOperation", connection.id, id)
	. = ..()
	if(error)
		BSQL_ERROR(error)

/datum/BSQL_Operation/IsComplete()
	if(BSQL_IS_DELETED(connection))
		return TRUE
	var/result = world._BSQL_Internal_Call("OpComplete", connection.id, id)
	if(!result)
		BSQL_ERROR("Error fetching operation [id] for connection [connection.id]!")
		return
	return result == "DONE"

/datum/BSQL_Operation/GetError()
	if(BSQL_IS_DELETED(connection))
		return "Connection deleted!"
	return world._BSQL_Internal_Call("GetError", connection.id, id)

/datum/BSQL_Operation/GetErrorCode()
	if(BSQL_IS_DELETED(connection))
		return -2
	return text2num(world._BSQL_Internal_Call("GetErrorCode", connection.id, id))

/datum/BSQL_Operation/WaitForCompletion()
	if(BSQL_IS_DELETED(connection))
		return
	var/error = world._BSQL_Internal_Call("BlockOnOperation", connection.id, id)
	if(error)
		if(error == "Operation timed out!")	//match this with the implementation
			return FALSE
		BSQL_ERROR("Error waiting for operation [id] for connection [connection.id]! [error]")
		return
	return TRUE
