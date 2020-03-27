/world/proc/_BSQL_Internal_Call(func, ...)
	var/list/call_args = args.Copy(2)
	BSQL_Debug("_BSQL_Internal_Call: [args[1]]([call_args.Join(", ")])")
	. = call(_BSQL_Library_Path(), func)(arglist(call_args))
	BSQL_Debug("Result: [. == null ? "NULL" : "\"[.]\""]")

/world/proc/_BSQL_Library_Path()
	return system_type == MS_WINDOWS ? "BSQL.dll" : "libBSQL.so"

/world/proc/_BSQL_InitCheck(datum/BSQL_Connection/caller)
	var/static/library_initialized = FALSE
	if(_BSQL_Initialized())
		return
	var/libPath = _BSQL_Library_Path()
	if(!fexists(libPath))
		BSQL_DEL_CALL(caller)
		BSQL_ERROR("Could not find [libPath]!")
		return

	var/version = _BSQL_Internal_Call("Version")
	if(version != BSQL_VERSION)
		BSQL_DEL_CALL(caller)
		BSQL_ERROR("BSQL DMAPI version mismatch! Expected [BSQL_VERSION], got [version == null ? "NULL" : version]!")
		return

	var/result = _BSQL_Internal_Call("Initialize")
	if(result)
		BSQL_DEL_CALL(caller)
		BSQL_ERROR(result)
		return
	_BSQL_Initialized(TRUE)

/world/proc/_BSQL_Initialized(new_val)
	var/static/bsql_library_initialized = FALSE
	if(new_val != null)
		bsql_library_initialized = new_val
	return bsql_library_initialized

/world/BSQL_Shutdown()
	if(!_BSQL_Initialized())
		return
	_BSQL_Internal_Call("Shutdown")
	_BSQL_Initialized(FALSE)
