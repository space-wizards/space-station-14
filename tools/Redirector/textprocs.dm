/*
	Written by contributor Doohl for the /tg/station Open Source project, hosted on Google Code.
	(2012)

	NOTE: The below functions are part of BYOND user Deadron's "TextHandling" library.
		[ http://www.byond.com/developer/Deadron/TextHandling ]
 */


proc
	///////////////////
	// Reading files //
	///////////////////
	dd_file2list(file_path, separator = "\n")
		var/file
		if (isfile(file_path))
			file = file_path
		else
			file = file(file_path)
		return dd_text2list(file2text(file), separator)


    ////////////////////
    // Replacing text //
    ////////////////////
	dd_replacetext(text, search_string, replacement_string)
		// A nice way to do this is to split the text into an array based on the search_string,
		// then put it back together into text using replacement_string as the new separator.
		var/list/textList = dd_text2list(text, search_string)
		return dd_list2text(textList, replacement_string)


	dd_replaceText(text, search_string, replacement_string)
		var/list/textList = dd_text2List(text, search_string)
		return dd_list2text(textList, replacement_string)


    /////////////////////
	// Prefix checking //
	/////////////////////
	dd_hasprefix(text, prefix)
		var/start = 1
		var/end = lentext(prefix) + 1
		return findtext(text, prefix, start, end)

	dd_hasPrefix(text, prefix)
		var/start = 1
		var/end = lentext(prefix) + 1
		return findtextEx(text, prefix, start, end)


    /////////////////////
	// Suffix checking //
	/////////////////////
	dd_hassuffix(text, suffix)
		var/start = length(text) - length(suffix)
		if (start)
			return findtext(text, suffix, start)

	dd_hasSuffix(text, suffix)
		var/start = length(text) - length(suffix)
		if (start)
			return findtextEx(text, suffix, start)

	/////////////////////////////
	// Turning text into lists //
	/////////////////////////////
	dd_text2list(text, separator)
		var/textlength      = lentext(text)
		var/separatorlength = lentext(separator)
		var/list/textList   = new /list()
		var/searchPosition  = 1
		var/findPosition    = 1
		var/buggyText
		while (1)															// Loop forever.
			findPosition = findtext(text, separator, searchPosition, 0)
			buggyText = copytext(text, searchPosition, findPosition)		// Everything from searchPosition to findPosition goes into a list element.
			textList += "[buggyText]"										// Working around weird problem where "text" != "text" after this copytext().

			searchPosition = findPosition + separatorlength					// Skip over separator.
			if (findPosition == 0)											// Didn't find anything at end of string so stop here.
				return textList
			else
				if (searchPosition > textlength)							// Found separator at very end of string.
					textList += ""											// So add empty element.
					return textList

	dd_text2List(text, separator)
		var/textlength      = lentext(text)
		var/separatorlength = lentext(separator)
		var/list/textList   = new /list()
		var/searchPosition  = 1
		var/findPosition    = 1
		var/buggyText
		while (1)															// Loop forever.
			findPosition = findtextEx(text, separator, searchPosition, 0)
			buggyText = copytext(text, searchPosition, findPosition)		// Everything from searchPosition to findPosition goes into a list element.
			textList += "[buggyText]"										// Working around weird problem where "text" != "text" after this copytext().

			searchPosition = findPosition + separatorlength					// Skip over separator.
			if (findPosition == 0)											// Didn't find anything at end of string so stop here.
				return textList
			else
				if (searchPosition > textlength)							// Found separator at very end of string.
					textList += ""											// So add empty element.
					return textList

	dd_list2text(list/the_list, separator)
		var/total = the_list.len
		if (total == 0)														// Nothing to work with.
			return

		var/newText = "[the_list[1]]"										// Treats any object/number as text also.
		var/count
		for (count = 2, count <= total, count++)
			if (separator)
				newText += separator
			newText += "[the_list[count]]"
		return newText

	dd_centertext(message, length)
		var/new_message = message
		var/size = length(message)
		if (size == length)
			return new_message
		if (size > length)
			return copytext(new_message, 1, length + 1)

		// Need to pad text to center it.
		var/delta = length - size
		if (delta == 1)
			// Add one space after it.
			return new_message + " "

		// Is this an odd number? If so, add extra space to front.
		if (delta % 2)
			new_message = " " + new_message
			delta--

		// Divide delta in 2, add those spaces to both ends.
		delta = delta / 2
		var/spaces = ""
		for (var/count = 1, count <= delta, count++)
			spaces += " "
		return spaces + new_message + spaces

	dd_limittext(message, length)
		// Truncates text to limit if necessary.
		var/size = length(message)
		if (size <= length)
			return message
		else
			return copytext(message, 1, length + 1)
