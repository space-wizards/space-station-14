#define BSQL_EXTERNAL_CONFIGURATION
#define BSQL_DEL_PROC(path) ##path/Destroy()
#define BSQL_DEL_CALL(obj) qdel(##obj)
#define BSQL_IS_DELETED(obj) (QDELETED(obj))
#define BSQL_PROTECT_DATUM(path) GENERAL_PROTECT_DATUM(##path)
#define BSQL_ERROR(message) SSdbcore.ReportError(message)
