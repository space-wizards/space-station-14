// Verb to toggle restart notifications
/client/verb/notify_restart()
	set category = "Special Verbs"
	set name = "Notify Restart"
	set desc = "Notifies you on Discord when the server restarts."

	// Safety checks
	if(!CONFIG_GET(flag/sql_enabled))
		to_chat(src, "<span class='warning'>This feature requires the SQL backend to be running.</span>")
		return

	if(!SSdiscord) // SS is still starting
		to_chat(src, "<span class='notice'>The server is still starting up. Please wait before attempting to link your account </span>")
		return

	if(!SSdiscord.enabled)
		to_chat(src, "<span class='warning'>This feature requires the server is running on the TGS toolkit</span>")
		return

	var/stored_id = SSdiscord.lookup_id(usr.ckey)
	if(!stored_id) // Account is not linked
		to_chat(src, "<span class='warning'>This requires you to link your Discord account with the \"Link Discord Account\" verb.</span>")
		return

	else // Linked
		for(var/member in SSdiscord.notify_members) // If they are in the list, take them out
			if(member == "[stored_id]")
				SSdiscord.notify_members -= "[stored_id]" // The list uses strings because BYOND cannot handle a 17 digit integer
				to_chat(src, "<span class='notice'>You will no longer be notified when the server restarts</span>")
				return // This is necassary so it doesnt get added again, as it relies on the for loop being unsuccessful to tell us if they are in the list or not
		
		// If we got here, they arent in the list. Chuck 'em in!
		to_chat(src, "<span class='notice'>You will now be notified when the server restarts</span>")
		SSdiscord.notify_members += "[stored_id]" // The list uses strings because BYOND cannot handle a 17 digit integer
