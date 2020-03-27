//I'm pretty sure that this is a recursive [s]descent[/s] ascent parser.


//Spec

//////////
//
//	query				:	select_query | delete_query | update_query | call_query | explain
//	explain				:	'EXPLAIN' query
//	select_query		:	'SELECT' object_selectors
//	delete_query		:	'DELETE' object_selectors
//	update_query		:	'UPDATE' object_selectors 'SET' assignments
//	call_query			:	'CALL' variable 'ON' object_selectors // Note here: 'variable' does function calls. This simplifies parsing.
//
//	select_item			:	'*' | object_type
//
//  object_selectors    :   select_item [('FROM' | 'IN') from_item] [modifier_list]
//  modifier_list       :   ('WHERE' bool_expression | 'MAP' expression) [modifier_list]
//
//	from_item			:	'world' | expression
//
//	call_function		:	<function name> '(' [expression_list] ')'
//
//	object_type			:	<type path>
//
//	assignments			:	assignment [',' assignments]
//	assignment			:	<variable name> '=' expression
//	variable			:	<variable name> | variable '.' variable | variable '[' <list index> ']' | '{' <ref as hex number> '}' | '(' expression ')' | call_function
//
//	bool_expression		:	expression comparitor expression  [bool_operator bool_expression]
//	expression			:	( unary_expression | '(' expression ')' | value ) [binary_operator expression]
//	expression_list		:	expression [',' expression_list]
//	unary_expression	:	unary_operator ( unary_expression | value )
//
//	comparitor			:	'=' | '==' | '!=' | '<>' | '<' | '<=' | '>' | '>='
//	value				:	variable | string | number | 'null' | object_type | array | selectors_array
//	unary_operator		:	'!' | '-' | '~'
//	binary_operator		:	comparitor | '+' | '-' | '/' | '*' | '&' | '|' | '^' | '%'
//	bool_operator		:	'AND' | '&&' | 'OR' | '||'
//
//	array				:	'[' expression_list ']'
//	selectors_array		:	'@[' object_selectors ']'
//
//	string				:	''' <some text> ''' | '"' <some text > '"'
//	number				:	<some digits>
//
//////////

/datum/SDQL_parser
	var/query_type
	var/error = 0

	var/list/query
	var/list/tree

	var/list/boolean_operators = list("and", "or", "&&", "||")
	var/list/unary_operators = list("!", "-", "~")
	var/list/binary_operators = list("+", "-", "/", "*", "&", "|", "^", "%")
	var/list/comparitors = list("=", "==", "!=", "<>", "<", "<=", ">", ">=")

/datum/SDQL_parser/New(query_list)
	query = query_list

/datum/SDQL_parser/proc/parse_error(error_message)
	error = 1
	to_chat(usr, "<span class='warning'>SQDL2 Parsing Error: [error_message]</span>")
	return query.len + 1

/datum/SDQL_parser/proc/parse()
	tree = list()
	query_options(1, tree)

	if(error)
		return list()
	else
		return tree

/datum/SDQL_parser/proc/token(i)
	if(i <= query.len)
		return query[i]

	else
		return null

/datum/SDQL_parser/proc/tokens(i, num)
	if(i + num <= query.len)
		return query.Copy(i, i + num)

	else
		return null

/datum/SDQL_parser/proc/tokenl(i)
	return lowertext(token(i))

/datum/SDQL_parser/proc/query_options(i, list/node)
	var/list/options = list()
	if(tokenl(i) == "using")
		i = option_assignments(i + 1, node, options)
	query(i, node)
	if(length(options))
		node["options"] = options

//option_assignment:	query_option '=' define
/datum/SDQL_parser/proc/option_assignment(i, list/node, list/assignment_list = list())
	var/type = tokenl(i)
	if(!(type in SDQL2_VALID_OPTION_TYPES))
		parse_error("Invalid option type: [type]")
	if(!(token(i + 1) == "="))
		parse_error("Invalid option assignment symbol: [token(i + 1)]")
	var/val = tokenl(i + 2)
	if(!(val in SDQL2_VALID_OPTION_VALUES))
		parse_error("Invalid optoin value: [val]")
	assignment_list[type] = val
	return (i + 3)

//option_assignments: option_assignment, [',' option_assignments]
/datum/SDQL_parser/proc/option_assignments(i, list/node, list/store)
	i = option_assignment(i, node, store)

	if(token(i) == ",")
		i = option_assignments(i + 1, node, store)

	return i

//query:	select_query | delete_query | update_query
/datum/SDQL_parser/proc/query(i, list/node)
	query_type = tokenl(i)

	switch(query_type)
		if("select")
			select_query(i, node)

		if("delete")
			delete_query(i, node)

		if("update")
			update_query(i, node)

		if("call")
			call_query(i, node)

		if("explain")
			node += "explain"
			node["explain"] = list()
			query(i + 1, node["explain"])


//	select_query:	'SELECT' object_selectors
/datum/SDQL_parser/proc/select_query(i, list/node)
	var/list/select = list()
	i = object_selectors(i + 1, select)

	node["select"] = select
	return i


//delete_query:	'DELETE' object_selectors
/datum/SDQL_parser/proc/delete_query(i, list/node)
	var/list/select = list()
	i = object_selectors(i + 1, select)

	node["delete"] = select

	return i


//update_query:	'UPDATE' object_selectors 'SET' assignments
/datum/SDQL_parser/proc/update_query(i, list/node)
	var/list/select = list()
	i = object_selectors(i + 1, select)

	node["update"] = select

	if(tokenl(i) != "set")
		i = parse_error("UPDATE has misplaced SET")

	var/list/set_assignments = list()
	i = assignments(i + 1, set_assignments)

	node["set"] = set_assignments

	return i


//call_query:	'CALL' call_function ['ON' object_selectors]
/datum/SDQL_parser/proc/call_query(i, list/node)
	var/list/func = list()
	i = variable(i + 1, func) // Yes technically does anything variable() matches but I don't care, if admins fuck up this badly then they shouldn't be allowed near SDQL.

	node["call"] = func

	if(tokenl(i) != "on")
		return parse_error("You need to specify what to call ON.")

	var/list/select = list()
	i = object_selectors(i + 1, select)

	node["on"] = select

	return i

// object_selectors: select_item [('FROM' | 'IN') from_item] [modifier_list]
/datum/SDQL_parser/proc/object_selectors(i, list/node)
	i = select_item(i, node)

	if (tokenl(i) == "from" || tokenl(i) == "in")
		i++
		var/list/from = list()
		i = from_item(i, from)
		node[++node.len] = from

	else
		node[++node.len] = list("world")

	i = modifier_list(i, node)
	return i

// modifier_list: ('WHERE' bool_expression | 'MAP' expression) [modifier_list]
/datum/SDQL_parser/proc/modifier_list(i, list/node)
	while (TRUE)
		if (tokenl(i) == "where")
			i++
			node += "where"
			var/list/expr = list()
			i = bool_expression(i, expr)
			node[++node.len] = expr

		else if (tokenl(i) == "map")
			i++
			node += "map"
			var/list/expr = list()
			i = expression(i, expr)
			node[++node.len] = expr

		else
			return i

//select_list:select_item [',' select_list]
/datum/SDQL_parser/proc/select_list(i, list/node)
	i = select_item(i, node)

	if(token(i) == ",")
		i = select_list(i + 1, node)

	return i

//assignments:	assignment, [',' assignments]
/datum/SDQL_parser/proc/assignments(i, list/node)
	i = assignment(i, node)

	if(token(i) == ",")
		i = assignments(i + 1, node)

	return i


//select_item:	'*' | select_function | object_type
/datum/SDQL_parser/proc/select_item(i, list/node)
	if (token(i) == "*")
		node += "*"
		i++

	else if(token(i)[1] == "/")
		i = object_type(i, node)

	else
		i = parse_error("Expected '*' or type path for select item")

	return i

// Standardized method for handling the IN/FROM and WHERE options.
/datum/SDQL_parser/proc/selectors(i, list/node)
	while (token(i))
		var/tok = tokenl(i)
		if (tok in list("from", "in"))
			var/list/from = list()
			i = from_item(i + 1, from)

			node["from"] = from
			continue

		if (tok == "where")
			var/list/where = list()
			i = bool_expression(i + 1, where)

			node["where"] = where
			continue

		parse_error("Expected either FROM, IN or WHERE token, found [token(i)] instead.")
		return i + 1

	if (!node.Find("from"))
		node["from"] = list("world")

	return i

//from_item:	'world' | expression
/datum/SDQL_parser/proc/from_item(i, list/node)
	if(token(i) == "world")
		node += "world"
		i++

	else
		i = expression(i, node)

	return i


//bool_expression:	expression [bool_operator bool_expression]
/datum/SDQL_parser/proc/bool_expression(i, list/node)

	var/list/bool = list()
	i = expression(i, bool)

	node[++node.len] = bool

	if(tokenl(i) in boolean_operators)
		i = bool_operator(i, node)
		i = bool_expression(i, node)

	return i


//assignment:	<variable name> '=' expression
/datum/SDQL_parser/proc/assignment(i, list/node, list/assignment_list = list())
	assignment_list += token(i)

	if(token(i + 1) == ".")
		i = assignment(i + 2, node, assignment_list)

	else if(token(i + 1) == "=")
		var/exp_list = list()
		node[assignment_list] = exp_list

		i = expression(i + 2, exp_list)

	else
		parse_error("Assignment expected, but no = found")

	return i


//variable:	<variable name> | variable '.' variable | variable '[' <list index> ']' | '{' <ref as hex number> '}' | '(' expression ')' | call_function
/datum/SDQL_parser/proc/variable(i, list/node)
	var/list/L = list(token(i))
	node[++node.len] = L

	if(token(i) == "{")
		L += token(i + 1)
		i += 2

		if(token(i) != "}")
			parse_error("Missing } at end of pointer.")

	else if(token(i) == "(") // not a proc but an expression
		var/list/sub_expression = list()

		i = expression(i + 1, sub_expression)

		if(token(i) != ")")
			parse_error("Missing ) at end of expression.")

		L[++L.len] = sub_expression

	if(token(i + 1) == ".")
		L += "."
		i = variable(i + 2, L)

	else if (token(i + 1) == "(") // OH BOY PROC
		var/list/arguments = list()
		i = call_function(i, null, arguments)
		L += ":"
		L[++L.len] = arguments

	else if (token(i + 1) == "\[")
		var/list/expression = list()
		i = expression(i + 2, expression)
		if (token(i) != "]")
			parse_error("Missing ] at the end of list access.")

		L += "\["
		L[++L.len] = expression
		i++

	else
		i++

	return i


//object_type:	<type path>
/datum/SDQL_parser/proc/object_type(i, list/node)

	if(token(i)[1] != "/")
		return parse_error("Expected type, but it didn't begin with /")

	var/path = text2path(token(i))
	if (path == null)
		return parse_error("Nonexistant type path: [token(i)]")

	node += path

	return i + 1


//comparitor:	'=' | '==' | '!=' | '<>' | '<' | '<=' | '>' | '>='
/datum/SDQL_parser/proc/comparitor(i, list/node)

	if(token(i) in list("=", "==", "!=", "<>", "<", "<=", ">", ">="))
		node += token(i)

	else
		parse_error("Unknown comparitor [token(i)]")

	return i + 1


//bool_operator:	'AND' | '&&' | 'OR' | '||'
/datum/SDQL_parser/proc/bool_operator(i, list/node)

	if(tokenl(i) in list("and", "or", "&&", "||"))
		node += token(i)

	else
		parse_error("Unknown comparitor [token(i)]")

	return i + 1


//string:	''' <some text> ''' | '"' <some text > '"'
/datum/SDQL_parser/proc/string(i, list/node)

	if(token(i)[1] in list("'", "\""))
		node += token(i)

	else
		parse_error("Expected string but found '[token(i)]'")

	return i + 1

//array:	'[' expression_list ']'
/datum/SDQL_parser/proc/array(i, list/node)
	// Arrays get turned into this: list("[", list(exp_1a = exp_1b, ...), ...), "[" is to mark the next node as an array.
	if(token(i)[1] != "\[")
		parse_error("Expected an array but found '[token(i)]'")
		return i + 1

	node += token(i) // Add the "["

	var/list/expression_list = list()

	i++
	if(token(i) != "]")
		var/list/temp_expression_list = list()
		var/tok
		do
			tok = token(i)
			if (tok == "," || tok == ":")
				if (temp_expression_list == null)
					parse_error("Found ',' or ':' without expression in an array.")
					return i + 1

				expression_list[++expression_list.len] = temp_expression_list
				temp_expression_list = null
				if (tok == ":")
					temp_expression_list = list()
					i = expression(i + 1, temp_expression_list)
					expression_list[expression_list[expression_list.len]] = temp_expression_list
					temp_expression_list = null
					tok = token(i)
					if (tok != ",")
						if (tok == "]")
							break

						parse_error("Expected ',' or ']' after array assoc value, but found '[token(i)]'")
						return i


				i++
				continue

			temp_expression_list = list()
			i = expression(i, temp_expression_list)

#if MIN_COMPILER_VERSION > 512
#warn Remove this outdated workaround
#elif DM_BUILD < 1467
			// http://www.byond.com/forum/post/2445083
			var/dummy = src.type
			dummy = dummy
#endif

		while(token(i) && token(i) != "]")

		if (temp_expression_list)
			expression_list[++expression_list.len] = temp_expression_list

	node[++node.len] = expression_list

	return i + 1

//selectors_array:	'@[' object_selectors ']'
/datum/SDQL_parser/proc/selectors_array(i, list/node)
	if(token(i) == "@\[")
		node += token(i++)
		if(token(i) != "]")
			var/list/select = list()
			i = object_selectors(i, select)
			node[++node.len] = select
			if(token(i) != "]")
				parse_error("Expected ']' to close selector array, but found '[token(i)]'")
		else
			parse_error("Selector array expected a selector, but found nothing")
	else
		parse_error("Expected '@\[' but found '[token(i)]'")

	return i + 1

//call_function:	<function name> ['(' [arguments] ')']
/datum/SDQL_parser/proc/call_function(i, list/node, list/arguments)
	if(length(tokenl(i)))
		var/procname = ""
		if(tokenl(i) == "global" && token(i + 1) == ".") // Global proc.
			i += 2
			procname = "global."
		node += procname + token(i++)
		if(token(i) != "(")
			parse_error("Expected ( but found '[token(i)]'")

		else if(token(i + 1) != ")")
			var/list/temp_expression_list = list()
			do
				i = expression(i + 1, temp_expression_list)
				if(token(i) == ",")
					arguments[++arguments.len] = temp_expression_list
					temp_expression_list = list()
					continue

			while(token(i) && token(i) != ")")

			arguments[++arguments.len] = temp_expression_list // The code this is copy pasted from won't be executed when it's the last param, this fixes that.
		else
			i++
	else
		parse_error("Expected a function but found nothing")
	return i + 1


//expression:	( unary_expression | value ) [binary_operator expression]
/datum/SDQL_parser/proc/expression(i, list/node)

	if(token(i) in unary_operators)
		i = unary_expression(i, node)

	else
		i = value(i, node)

	if(token(i) in binary_operators)
		i = binary_operator(i, node)
		i = expression(i, node)

	else if(token(i) in comparitors)
		i = binary_operator(i, node)

		var/list/rhs = list()
		i = expression(i, rhs)

		node[++node.len] = rhs


	return i


//unary_expression:	unary_operator ( unary_expression | value )
/datum/SDQL_parser/proc/unary_expression(i, list/node)

	if(token(i) in unary_operators)
		var/list/unary_exp = list()

		unary_exp += token(i)
		i++

		if(token(i) in unary_operators)
			i = unary_expression(i, unary_exp)

		else
			i = value(i, unary_exp)

		node[++node.len] = unary_exp


	else
		parse_error("Expected unary operator but found '[token(i)]'")

	return i


//binary_operator:	comparitor | '+' | '-' | '/' | '*' | '&' | '|' | '^' | '%'
/datum/SDQL_parser/proc/binary_operator(i, list/node)

	if(token(i) in (binary_operators + comparitors))
		node += token(i)

	else
		parse_error("Unknown binary operator [token(i)]")

	return i + 1


//value:	variable | string | number | 'null' | object_type | array | selectors_array
/datum/SDQL_parser/proc/value(i, list/node)
	if(token(i) == "null")
		node += "null"
		i++

	else if(lowertext(copytext(token(i), 1, 3)) == "0x" && isnum(hex2num(copytext(token(i), 3))))//3 == length("0x") + 1
		node += hex2num(copytext(token(i), 3))
		i++

	else if(isnum(text2num(token(i))))
		node += text2num(token(i))
		i++

	else if(token(i)[1] in list("'", "\""))
		i = string(i, node)

	else if(token(i)[1] == "\[") // Start a list.
		i = array(i, node)

	else if(copytext(token(i), 1, 3) == "@\[")//3 == length("@\[") + 1
		i = selectors_array(i, node)

	else if(token(i)[1] == "/")
		i = object_type(i, node)

	else
		i = variable(i, node)

	return i
