/*
 * Holds procs designed to help with filtering text
 * Contains groups:
 *			SQL sanitization/formating
 *			Text sanitization
 *			Text searches
 *			Text modification
 *			Misc
 */


/*
 * SQL sanitization
 */

// Run all strings to be used in an SQL query through this proc first to properly escape out injection attempts.
/proc/sanitizeSQL(t)
	return SSdbcore.Quote("[t]")

/proc/format_table_name(table as text)
	return CONFIG_GET(string/feedback_tableprefix) + table

/*
 * Text sanitization
 */

//Simply removes < and > and limits the length of the message
/proc/strip_html_simple(t,limit=MAX_MESSAGE_LEN)
	var/list/strip_chars = list("<",">")
	t = copytext(t,1,limit)
	for(var/char in strip_chars)
		var/index = findtext(t, char)
		while(index)
			t = copytext(t, 1, index) + copytext(t, index+1)
			index = findtext(t, char)
	return t

//Removes a few problematic characters
/proc/sanitize_simple(t,list/repl_chars = list("\n"="#","\t"="#"))
	for(var/char in repl_chars)
		var/index = findtext(t, char)
		while(index)
			t = copytext(t, 1, index) + repl_chars[char] + copytext(t, index + length(char))
			index = findtext(t, char, index + length(char))
	return t

/proc/sanitize_filename(t)
	return sanitize_simple(t, list("\n"="", "\t"="", "/"="", "\\"="", "?"="", "%"="", "*"="", ":"="", "|"="", "\""="", "<"="", ">"=""))

///returns nothing with an alert instead of the message if it contains something in the ic filter, and sanitizes normally if the name is fine. It returns nothing so it backs out of the input the same way as if you had entered nothing.
/proc/sanitize_name(t,list/repl_chars = null)
	if(CHAT_FILTER_CHECK(t))
		alert("You cannot set a name that contains a word prohibited in IC chat!")
		return ""
	if(t == "space" || t == "floor" || t == "wall" || t == "r-wall" || t == "monkey" || t == "unknown" || t == "inactive ai")	//prevents these common metagamey names
		alert("Invalid name.")
		return ""
	return sanitize(t)

//Runs byond's sanitization proc along-side sanitize_simple
/proc/sanitize(t,list/repl_chars = null)
	return html_encode(sanitize_simple(t,repl_chars))

//Runs sanitize and strip_html_simple
//I believe strip_html_simple() is required to run first to prevent '<' from displaying as '&lt;' after sanitize() calls byond's html_encode()
/proc/strip_html(t,limit=MAX_MESSAGE_LEN)
	return copytext((sanitize(strip_html_simple(t))),1,limit)

//Runs byond's sanitization proc along-side strip_html_simple
//I believe strip_html_simple() is required to run first to prevent '<' from displaying as '&lt;' that html_encode() would cause
/proc/adminscrub(t,limit=MAX_MESSAGE_LEN)
	return copytext((html_encode(strip_html_simple(t))),1,limit)


//Returns null if there is any bad text in the string
/proc/reject_bad_text(text, max_length = 512, ascii_only = TRUE)
	var/char_count = 0
	var/non_whitespace = FALSE
	var/lenbytes = length(text)
	var/char = ""
	for(var/i = 1, i <= lenbytes, i += length(char))
		char = text[i]
		char_count++
		if(char_count > max_length)
			return
		switch(text2ascii(char))
			if(62, 60, 92, 47) // <, >, \, /
				return
			if(0 to 31)
				return
			if(32)
				continue
			if(127 to INFINITY)
				if(ascii_only)
					return
			else
				non_whitespace = TRUE
	if(non_whitespace)
		return text		//only accepts the text if it has some non-spaces

// Used to get a properly sanitized input, of max_length
// no_trim is self explanatory but it prevents the input from being trimed if you intend to parse newlines or whitespace.
/proc/stripped_input(mob/user, message = "", title = "", default = "", max_length=MAX_MESSAGE_LEN, no_trim=FALSE)
	var/name = input(user, message, title, default) as text|null
	if(no_trim)
		return copytext(html_encode(name), 1, max_length)
	else
		return trim(html_encode(name), max_length) //trim is "outside" because html_encode can expand single symbols into multiple symbols (such as turning < into &lt;)

// Used to get a properly sanitized multiline input, of max_length
/proc/stripped_multiline_input(mob/user, message = "", title = "", default = "", max_length=MAX_MESSAGE_LEN, no_trim=FALSE)
	var/name = input(user, message, title, default) as message|null
	if(no_trim)
		return copytext(html_encode(name), 1, max_length)
	else
		return trim(html_encode(name), max_length)

#define NO_CHARS_DETECTED 0
#define SPACES_DETECTED 1
#define SYMBOLS_DETECTED 2
#define NUMBERS_DETECTED 3
#define LETTERS_DETECTED 4

//Filters out undesirable characters from names
/proc/reject_bad_name(t_in, allow_numbers = FALSE, max_length = MAX_NAME_LEN, ascii_only = TRUE)
	if(!t_in)
		return //Rejects the input if it is null

	var/number_of_alphanumeric = 0
	var/last_char_group = NO_CHARS_DETECTED
	var/t_out = ""
	var/t_len = length(t_in)
	var/charcount = 0
	var/char = ""


	for(var/i = 1, i <= t_len, i += length(char))
		char = t_in[i]

		switch(text2ascii(char))
			// A  .. Z
			if(65 to 90)			//Uppercase Letters
				number_of_alphanumeric++
				last_char_group = LETTERS_DETECTED

			// a  .. z
			if(97 to 122)			//Lowercase Letters
				if(last_char_group == NO_CHARS_DETECTED || last_char_group == SPACES_DETECTED || last_char_group == SYMBOLS_DETECTED) //start of a word
					char = uppertext(char)
				number_of_alphanumeric++
				last_char_group = LETTERS_DETECTED

			// 0  .. 9
			if(48 to 57)			//Numbers
				if(last_char_group == NO_CHARS_DETECTED || !allow_numbers) //suppress at start of string
					continue
				number_of_alphanumeric++
				last_char_group = NUMBERS_DETECTED

			// '  -  .
			if(39,45,46)			//Common name punctuation
				if(last_char_group == NO_CHARS_DETECTED)
					continue
				last_char_group = SYMBOLS_DETECTED

			// ~   |   @  :  #  $  %  &  *  +
			if(126,124,64,58,35,36,37,38,42,43)			//Other symbols that we'll allow (mainly for AI)
				if(last_char_group == NO_CHARS_DETECTED || !allow_numbers) //suppress at start of string
					continue
				last_char_group = SYMBOLS_DETECTED

			//Space
			if(32)
				if(last_char_group == NO_CHARS_DETECTED || last_char_group == SPACES_DETECTED) //suppress double-spaces and spaces at start of string
					continue
				last_char_group = SPACES_DETECTED

			if(127 to INFINITY)
				if(ascii_only)
					continue
				last_char_group = SYMBOLS_DETECTED //for now, we'll treat all non-ascii characters like symbols even though most are letters

			else
				continue

		t_out += char
		charcount++
		if(charcount >= max_length)
			break

	if(number_of_alphanumeric < 2)
		return		//protects against tiny names like "A" and also names like "' ' ' ' ' ' ' '"

	if(last_char_group == SPACES_DETECTED)
		t_out = copytext_char(t_out, 1, -1) //removes the last character (in this case a space)

	for(var/bad_name in list("space","floor","wall","r-wall","monkey","unknown","inactive ai"))	//prevents these common metagamey names
		if(cmptext(t_out,bad_name))
			return	//(not case sensitive)

	return t_out

#undef NO_CHARS_DETECTED
#undef SPACES_DETECTED
#undef NUMBERS_DETECTED
#undef LETTERS_DETECTED



//html_encode helper proc that returns the smallest non null of two numbers
//or 0 if they're both null (needed because of findtext returning 0 when a value is not present)
/proc/non_zero_min(a, b)
	if(!a)
		return b
	if(!b)
		return a
	return (a < b ? a : b)

//Checks if any of a given list of needles is in the haystack
/proc/text_in_list(haystack, list/needle_list, start=1, end=0)
	for(var/needle in needle_list)
		if(findtext(haystack, needle, start, end))
			return 1
	return 0

//Like above, but case sensitive
/proc/text_in_list_case(haystack, list/needle_list, start=1, end=0)
	for(var/needle in needle_list)
		if(findtextEx(haystack, needle, start, end))
			return 1
	return 0

//Adds 'char' ahead of 'text' until there are 'count' characters total
/proc/add_leading(text, count, char = " ")
	var/charcount = count - length_char(text)
	var/list/chars_to_add[max(charcount + 1, 0)]
	return jointext(chars_to_add, char) + text

//Adds 'char' behind 'text' until there are 'count' characters total
/proc/add_trailing(text, count, char = " ")
	var/charcount = count - length_char(text)
	var/list/chars_to_add[max(charcount + 1, 0)]
	return text + jointext(chars_to_add, char)

//Returns a string with reserved characters and spaces before the first letter removed
/proc/trim_left(text)
	for (var/i = 1 to length(text))
		if (text2ascii(text, i) > 32)
			return copytext(text, i)
	return ""

//Returns a string with reserved characters and spaces after the last letter removed
/proc/trim_right(text)
	for (var/i = length(text), i > 0, i--)
		if (text2ascii(text, i) > 32)
			return copytext(text, 1, i + 1)
	return ""

//Returns a string with reserved characters and spaces before the first word and after the last word removed.
/proc/trim(text, max_length)
	if(max_length)
		text = copytext_char(text, 1, max_length)
	return trim_left(trim_right(text))

//Returns a string with the first element of the string capitalized.
/proc/capitalize(t)
	. = t
	if(t)
		. = t[1]
		return uppertext(.) + copytext(t, 1 + length(.))

/proc/stringmerge(text,compare,replace = "*")
//This proc fills in all spaces with the "replace" var (* by default) with whatever
//is in the other string at the same spot (assuming it is not a replace char).
//This is used for fingerprints
	var/newtext = text
	var/text_it = 1 //iterators
	var/comp_it = 1
	var/newtext_it = 1
	var/text_length = length(text)
	var/comp_length = length(compare)
	while(comp_it <= comp_length && text_it <= text_length)
		var/a = text[text_it]
		var/b = compare[comp_it]
//if it isn't both the same letter, or if they are both the replacement character
//(no way to know what it was supposed to be)
		if(a != b)
			if(a == replace) //if A is the replacement char
				newtext = copytext(newtext, 1, newtext_it) + b + copytext(newtext, newtext_it + length(newtext[newtext_it]))
			else if(b == replace) //if B is the replacement char
				newtext = copytext(newtext, 1, newtext_it) + a + copytext(newtext, newtext_it + length(newtext[newtext_it]))
			else //The lists disagree, Uh-oh!
				return 0
		text_it += length(a)
		comp_it += length(b)
		newtext_it += length(newtext[newtext_it])

	return newtext

/proc/stringpercent(text,character = "*")
//This proc returns the number of chars of the string that is the character
//This is used for detective work to determine fingerprint completion.
	if(!text || !character)
		return 0
	var/count = 0
	var/lentext = length(text)
	var/a = ""
	for(var/i = 1, i <= lentext, i += length(a))
		a = text[i]
		if(a == character)
			count++
	return count

/proc/reverse_text(text = "")
	var/new_text = ""
	var/lentext = length(text)
	var/letter = ""
	for(var/i = 1, i <= lentext, i += length(letter))
		letter = text[i]
		new_text = letter + new_text
	return new_text

GLOBAL_LIST_INIT(zero_character_only, list("0"))
GLOBAL_LIST_INIT(hex_characters, list("0","1","2","3","4","5","6","7","8","9","a","b","c","d","e","f"))
GLOBAL_LIST_INIT(alphabet, list("a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"))
GLOBAL_LIST_INIT(binary, list("0","1"))
/proc/random_string(length, list/characters)
	. = ""
	for(var/i=1, i<=length, i++)
		. += pick(characters)

/proc/repeat_string(times, string="")
	. = ""
	for(var/i=1, i<=times, i++)
		. += string

/proc/random_short_color()
	return random_string(3, GLOB.hex_characters)

/proc/random_color()
	return random_string(6, GLOB.hex_characters)

//merges non-null characters (3rd argument) from "from" into "into". Returns result
//e.g. into = "Hello World"
//     from = "Seeya______"
//     returns"Seeya World"
//The returned text is always the same length as into
//This was coded to handle DNA gene-splicing.
/proc/merge_text(into, from, null_char="_")
	. = ""
	if(!istext(into))
		into = ""
	if(!istext(from))
		from = ""
	var/null_ascii = istext(null_char) ? text2ascii(null_char, 1) : null_char
	var/copying_into = FALSE
	var/char = ""
	var/start = 1
	var/end_from = length(from)
	var/end_into = length(into)
	var/into_it = 1
	var/from_it = 1
	while(from_it <= end_from && into_it <= end_into)
		char = from[from_it]
		if(text2ascii(char) == null_ascii)
			if(!copying_into)
				. += copytext(from, start, from_it)
				start = into_it
				copying_into = TRUE
		else
			if(copying_into)
				. += copytext(into, start, into_it)
				start = from_it
				copying_into = FALSE
		into_it += length(into[into_it])
		from_it += length(char)

	if(copying_into)
		. += copytext(into, start)
	else
		. += copytext(from, start, from_it)
		if(into_it <= end_into)
			. += copytext(into, into_it)

//finds the first occurrence of one of the characters from needles argument inside haystack
//it may appear this can be optimised, but it really can't. findtext() is so much faster than anything you can do in byondcode.
//stupid byond :(
/proc/findchar(haystack, needles, start=1, end=0)
	var/char = ""
	var/len = length(needles)
	for(var/i = 1, i <= len, i += length(char))
		char = needles[i]
		. = findtextEx(haystack, char, start, end)
		if(.)
			return
	return 0

/proc/parsemarkdown_basic_step1(t, limited=FALSE)
	if(length(t) <= 0)
		return

	// This parses markdown with no custom rules

	// Escape backslashed

	t = replacetext(t, "$", "$-")
	t = replacetext(t, "\\\\", "$1")
	t = replacetext(t, "\\**", "$2")
	t = replacetext(t, "\\*", "$3")
	t = replacetext(t, "\\__", "$4")
	t = replacetext(t, "\\_", "$5")
	t = replacetext(t, "\\^", "$6")
	t = replacetext(t, "\\((", "$7")
	t = replacetext(t, "\\))", "$8")
	t = replacetext(t, "\\|", "$9")
	t = replacetext(t, "\\%", "$0")

	// Escape  single characters that will be used

	t = replacetext(t, "!", "$a")

	// Parse hr and small

	if(!limited)
		t = replacetext(t, "((", "<font size=\"1\">")
		t = replacetext(t, "))", "</font>")
		t = replacetext(t, regex("(-){3,}", "gm"), "<hr>")
		t = replacetext(t, regex("^\\((-){3,}\\)$", "gm"), "$1")

		// Parse lists

		var/list/tlist = splittext(t, "\n")
		var/tlistlen = tlist.len
		var/listlevel = -1
		var/singlespace = -1 // if 0, double spaces are used before asterisks, if 1, single are
		for(var/i = 1, i <= tlistlen, i++)
			var/line = tlist[i]
			var/count_asterisk = length(replacetext(line, regex("\[^\\*\]+", "g"), ""))
			if(count_asterisk % 2 == 1 && findtext(line, regex("^\\s*\\*", "g"))) // there is an extra asterisk in the beggining

				var/count_w = length(replacetext(line, regex("^( *)\\*.*$", "g"), "$1")) // whitespace before asterisk
				line = replacetext(line, regex("^ *(\\*.*)$", "g"), "$1")

				if(singlespace == -1 && count_w == 2)
					if(listlevel == 0)
						singlespace = 0
					else
						singlespace = 1

				if(singlespace == 0)
					count_w = count_w % 2 ? round(count_w / 2 + 0.25) : count_w / 2

				line = replacetext(line, regex("\\*", ""), "<li>")
				while(listlevel < count_w)
					line = "<ul>" + line
					listlevel++
				while(listlevel > count_w)
					line = "</ul>" + line
					listlevel--

			else while(listlevel >= 0)
				line = "</ul>" + line
				listlevel--

			tlist[i] = line
		// end for

		t = tlist[1]
		for(var/i = 2, i <= tlistlen, i++)
			t += "\n" + tlist[i]

		while(listlevel >= 0)
			t += "</ul>"
			listlevel--

	else
		t = replacetext(t, "((", "")
		t = replacetext(t, "))", "")

	// Parse headers

	t = replacetext(t, regex("^#(?!#) ?(.+)$", "gm"), "<h2>$1</h2>")
	t = replacetext(t, regex("^##(?!#) ?(.+)$", "gm"), "<h3>$1</h3>")
	t = replacetext(t, regex("^###(?!#) ?(.+)$", "gm"), "<h4>$1</h4>")
	t = replacetext(t, regex("^#### ?(.+)$", "gm"), "<h5>$1</h5>")

	// Parse most rules

	t = replacetext(t, regex("\\*(\[^\\*\]*)\\*", "g"), "<i>$1</i>")
	t = replacetext(t, regex("_(\[^_\]*)_", "g"), "<i>$1</i>")
	t = replacetext(t, "<i></i>", "!")
	t = replacetext(t, "</i><i>", "!")
	t = replacetext(t, regex("\\!(\[^\\!\]+)\\!", "g"), "<b>$1</b>")
	t = replacetext(t, regex("\\^(\[^\\^\]+)\\^", "g"), "<font size=\"4\">$1</font>")
	t = replacetext(t, regex("\\|(\[^\\|\]+)\\|", "g"), "<center>$1</center>")
	t = replacetext(t, "!", "</i><i>")

	return t

/proc/parsemarkdown_basic_step2(t)
	if(length(t) <= 0)
		return

	// Restore the single characters used

	t = replacetext(t, "$a", "!")

	// Redo the escaping

	t = replacetext(t, "$1", "\\")
	t = replacetext(t, "$2", "**")
	t = replacetext(t, "$3", "*")
	t = replacetext(t, "$4", "__")
	t = replacetext(t, "$5", "_")
	t = replacetext(t, "$6", "^")
	t = replacetext(t, "$7", "((")
	t = replacetext(t, "$8", "))")
	t = replacetext(t, "$9", "|")
	t = replacetext(t, "$0", "%")
	t = replacetext(t, "$-", "$")

	return t

/proc/parsemarkdown_basic(t, limited=FALSE)
	t = parsemarkdown_basic_step1(t, limited)
	t = parsemarkdown_basic_step2(t)
	return t

/proc/parsemarkdown(t, mob/user=null, limited=FALSE)
	if(length(t) <= 0)
		return

	// Premanage whitespace

	t = replacetext(t, regex("\[^\\S\\r\\n \]", "g"), "  ")

	t = parsemarkdown_basic_step1(t)

	t = replacetext(t, regex("%s(?:ign)?(?=\\s|$)", "igm"), user ? "<font face=\"[SIGNFONT]\"><i>[user.real_name]</i></font>" : "<span class=\"paper_field\"></span>")
	t = replacetext(t, regex("%f(?:ield)?(?=\\s|$)", "igm"), "<span class=\"paper_field\"></span>")

	t = parsemarkdown_basic_step2(t)

	// Manage whitespace

	t = replacetext(t, regex("(?:\\r\\n?|\\n)", "g"), "<br>")

	t = replacetext(t, "  ", "&nbsp;&nbsp;")

	// Done

	return t

/proc/text2charlist(text)
	var/char = ""
	var/lentext = length(text)
	. = list()
	for(var/i = 1, i <= lentext, i += length(char))
		char = text[i]
		. += char

/proc/rot13(text = "")
	var/lentext = length(text)
	var/char = ""
	var/ascii = 0
	. = ""
	for(var/i = 1, i <= lentext, i += length(char))
		char = text[i]
		ascii = text2ascii(char)
		switch(ascii)
			if(65 to 77, 97 to 109) //A to M, a to m
				ascii += 13
			if(78 to 90, 110 to 122) //N to Z, n to z
				ascii -= 13
		. += ascii2text(ascii)

//Takes a list of values, sanitizes it down for readability and character count,
//then exports it as a json file at data/npc_saves/[filename].json.
//As far as SS13 is concerned this is write only data. You can't change something
//in the json file and have it be reflected in the in game item/mob it came from.
//(That's what things like savefiles are for) Note that this list is not shuffled.
/proc/twitterize(list/proposed, filename, cullshort = 1, storemax = 1000)
	if(!islist(proposed) || !filename || !CONFIG_GET(flag/log_twitter))
		return

	//Regular expressions are, as usual, absolute magic
	//Any characters outside of 32 (space) to 126 (~) because treating things you don't understand as "magic" is really stupid
	var/regex/all_invalid_symbols = new(@"[^ -~]{1}")

	var/list/accepted = list()
	for(var/string in proposed)
		if(findtext(string,GLOB.is_website) || findtext(string,GLOB.is_email) || findtext(string,all_invalid_symbols) || !findtext(string,GLOB.is_alphanumeric))
			continue
		var/buffer = ""
		var/early_culling = TRUE
		var/lentext = length(string)
		var/let = ""

		for(var/pos = 1, pos <= lentext, pos += length(let))
			let = string[pos]
			if(!findtext(let, GLOB.is_alphanumeric))
				continue
			early_culling = FALSE
			buffer = copytext(string, pos)
			break
		if(early_culling) //Never found any letters! Bail!
			continue

		var/punctbuffer = ""
		var/cutoff = 0
		lentext = length_char(buffer)
		for(var/pos = 1, pos <= lentext, pos++)
			let = copytext_char(buffer, -pos, -pos + 1)
			if(!findtext(let, GLOB.is_punctuation)) //This won't handle things like Nyaaaa!~ but that's fine
				break
			punctbuffer += let
			cutoff += length(let)
		if(punctbuffer) //We clip down excessive punctuation to get the letter count lower and reduce repeats. It's not perfect but it helps.
			var/exclaim = FALSE
			var/question = FALSE
			var/periods = 0
			lentext = length(punctbuffer)
			for(var/pos = 1, pos <= lentext, pos += length(let))
				let = punctbuffer[pos]
				if(!exclaim && findtext(let, "!"))
					exclaim = TRUE
					if(question)
						break
				if(!question && findtext(let, "?"))
					question = TRUE
					if(exclaim)
						break
				if(!exclaim && !question && findtext(let, ".")) //? and ! take priority over periods
					periods += 1
			if(exclaim)
				if(question)
					punctbuffer = "?!"
				else
					punctbuffer = "!"
			else if(question)
				punctbuffer = "?"
			else if(periods > 1)
				punctbuffer = "..."
			else
				punctbuffer = "" //Grammer nazis be damned
			buffer = copytext(buffer, 1, -cutoff) + punctbuffer
		lentext = length_char(buffer)
		if(!buffer || lentext > 280 || lentext <= cullshort || (buffer in accepted))
			continue

		accepted += buffer

	var/log = file("data/npc_saves/[filename].json") //If this line ever shows up as changed in a PR be very careful you aren't being memed on
	var/list/oldjson = list()
	var/list/oldentries = list()
	if(fexists(log))
		oldjson = json_decode(file2text(log))
		oldentries = oldjson["data"]
	if(!isemptylist(oldentries))
		for(var/string in accepted)
			for(var/old in oldentries)
				if(string == old)
					oldentries.Remove(old) //Line's position in line is "refreshed" until it falls off the in game radar
					break

	var/list/finalized = list()
	finalized = accepted.Copy() + oldentries.Copy() //we keep old and unreferenced phrases near the bottom for culling
	listclearnulls(finalized)
	if(!isemptylist(finalized) && length(finalized) > storemax)
		finalized.Cut(storemax + 1)
	fdel(log)

	var/list/tosend = list()
	tosend["data"] = finalized
	WRITE_FILE(log, json_encode(tosend))

//Used for applying byonds text macros to strings that are loaded at runtime
/proc/apply_text_macros(string)
	var/next_backslash = findtext(string, "\\")
	if(!next_backslash)
		return string

	var/leng = length(string)

	var/next_space = findtext(string, " ", next_backslash + length(string[next_backslash]))
	if(!next_space)
		next_space = leng - next_backslash

	if(!next_space)	//trailing bs
		return string

	var/base = next_backslash == 1 ? "" : copytext(string, 1, next_backslash)
	var/macro = lowertext(copytext(string, next_backslash + length(string[next_backslash]), next_space))
	var/rest = next_backslash > leng ? "" : copytext(string, next_space + length(string[next_space]))

	//See https://secure.byond.com/docs/ref/info.html#/DM/text/macros
	switch(macro)
		//prefixes/agnostic
		if("the")
			rest = text("\the []", rest)
		if("a")
			rest = text("\a []", rest)
		if("an")
			rest = text("\an []", rest)
		if("proper")
			rest = text("\proper []", rest)
		if("improper")
			rest = text("\improper []", rest)
		if("roman")
			rest = text("\roman []", rest)
		//postfixes
		if("th")
			base = text("[]\th", rest)
		if("s")
			base = text("[]\s", rest)
		if("he")
			base = text("[]\he", rest)
		if("she")
			base = text("[]\she", rest)
		if("his")
			base = text("[]\his", rest)
		if("himself")
			base = text("[]\himself", rest)
		if("herself")
			base = text("[]\herself", rest)
		if("hers")
			base = text("[]\hers", rest)

	. = base
	if(rest)
		. += .(rest)

//Replacement for the \th macro when you want the whole word output as text (first instead of 1st)
/proc/thtotext(number)
	if(!isnum(number))
		return
	switch(number)
		if(1)
			return "first"
		if(2)
			return "second"
		if(3)
			return "third"
		if(4)
			return "fourth"
		if(5)
			return "fifth"
		if(6)
			return "sixth"
		if(7)
			return "seventh"
		if(8)
			return "eighth"
		if(9)
			return "ninth"
		if(10)
			return "tenth"
		if(11)
			return "eleventh"
		if(12)
			return "twelfth"
		else
			return "[number]\th"


/proc/random_capital_letter()
	return uppertext(pick(GLOB.alphabet))

/proc/unintelligize(message)
	var/regex/word_boundaries = regex(@"\b[\S]+\b", "g")
	var/prefix = message[1]
	if(prefix == ";")
		message = copytext(message, 1 + length(prefix))
	else if(prefix in list(":", "#"))
		prefix += message[1 + length(prefix)]
		message = copytext(message, length(prefix))
	else
		prefix = ""

	var/list/rearranged = list()
	while(word_boundaries.Find(message))
		var/cword = word_boundaries.match
		if(length(cword))
			rearranged += cword
	shuffle_inplace(rearranged)
	return "[prefix][jointext(rearranged, " ")]"


/proc/readable_corrupted_text(text)
	var/list/corruption_options = list("..", "£%", "~~\"", "!!", "*", "^", "$!", "-", "}", "?")
	var/corrupted_text = ""

	var/lentext = length(text)
	var/letter = ""
	// Have every letter have a chance of creating corruption on either side
	// Small chance of letters being removed in place of corruption - still overall readable
	for(var/letter_index = 1, letter_index <= lentext, letter_index += length(letter))
		letter = text[letter_index]

		if (prob(15))
			corrupted_text += pick(corruption_options)

		if (prob(95))
			corrupted_text += letter
		else
			corrupted_text += pick(corruption_options)

	if (prob(15))
		corrupted_text += pick(corruption_options)

	return corrupted_text

#define is_alpha(X) ((text2ascii(X) <= 122) && (text2ascii(X) >= 97))
#define is_digit(X) ((length(X) == 1) && (length(text2num(X)) == 1))

//json decode that will return null on parse error instead of runtiming.
/proc/safe_json_decode(data)
	try
		return json_decode(data)
	catch
		return
