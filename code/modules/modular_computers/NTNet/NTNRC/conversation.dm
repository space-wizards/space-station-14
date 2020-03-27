#define MAX_CHANNELS 1000

/datum/ntnet_conversation
	var/id = null
	var/title = "Untitled Conversation"
	var/datum/computer_file/program/chatclient/operator // "Administrator" of this channel. Creator starts as channel's operator,
	var/list/messages = list()
	var/list/clients = list()
	var/password
	var/static/ntnrc_uid = 0

/datum/ntnet_conversation/New()
	id = ntnrc_uid + 1
	if(id > MAX_CHANNELS)
		qdel(src)
		return
	ntnrc_uid = id
	if(SSnetworks.station_network)
		SSnetworks.station_network.chat_channels.Add(src)
	..()

/datum/ntnet_conversation/Destroy()
	if(SSnetworks.station_network)
		SSnetworks.station_network.chat_channels.Remove(src)
	return ..()

/datum/ntnet_conversation/proc/add_message(message, username)
	message = "[station_time_timestamp()] [username]: [message]"
	messages.Add(message)
	trim_message_list()

/datum/ntnet_conversation/proc/add_status_message(message)
	messages.Add("[station_time_timestamp()] -!- [message]")
	trim_message_list()

/datum/ntnet_conversation/proc/trim_message_list()
	if(messages.len <= 50)
		return
	messages = messages.Copy(messages.len-50 ,0)

/datum/ntnet_conversation/proc/add_client(datum/computer_file/program/chatclient/C)
	if(!istype(C))
		return
	clients.Add(C)
	add_status_message("[C.username] has joined the channel.")
	// No operator, so we assume the channel was empty. Assign this user as operator.
	if(!operator)
		changeop(C)

/datum/ntnet_conversation/proc/remove_client(datum/computer_file/program/chatclient/C)
	if(!istype(C) || !(C in clients))
		return
	clients.Remove(C)
	add_status_message("[C.username] has left the channel.")

	// Channel operator left, pick new operator
	if(C == operator)
		operator = null
		if(clients.len)
			var/datum/computer_file/program/chatclient/newop = pick(clients)
			changeop(newop)


/datum/ntnet_conversation/proc/changeop(datum/computer_file/program/chatclient/newop)
	if(istype(newop))
		operator = newop
		add_status_message("Channel operator status transferred to [newop.username].")

/datum/ntnet_conversation/proc/change_title(newtitle, datum/computer_file/program/chatclient/client)
	if(operator != client)
		return FALSE // Not Authorised

	add_status_message("[client.username] has changed channel title from [title] to [newtitle]")
	title = newtitle

#undef MAX_CHANNELS
