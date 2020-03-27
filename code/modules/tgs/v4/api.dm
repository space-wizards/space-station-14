#define TGS4_PARAM_INFO_JSON "tgs_json"

#define TGS4_INTEROP_ACCESS_IDENTIFIER "tgs_tok"

#define TGS4_RESPONSE_SUCCESS "tgs_succ"

#define TGS4_TOPIC_CHANGE_PORT "tgs_port"
#define TGS4_TOPIC_CHANGE_REBOOT_MODE "tgs_rmode"
#define TGS4_TOPIC_CHAT_COMMAND "tgs_chat_comm"
#define TGS4_TOPIC_EVENT "tgs_event"
#define TGS4_TOPIC_INTEROP_RESPONSE "tgs_interop"

#define TGS4_COMM_NEW_PORT "tgs_new_port"
#define TGS4_COMM_VALIDATE "tgs_validate"
#define TGS4_COMM_SERVER_PRIMED "tgs_prime"
#define TGS4_COMM_WORLD_REBOOT "tgs_reboot"
#define TGS4_COMM_END_PROCESS "tgs_kill"
#define TGS4_COMM_CHAT "tgs_chat_send"

#define TGS4_PARAMETER_COMMAND "tgs_com"
#define TGS4_PARAMETER_DATA "tgs_data"

#define TGS4_PORT_CRITFAIL_MESSAGE " Must exit to let watchdog reboot..."

#define EXPORT_TIMEOUT_DS 200

/datum/tgs_api/v4
	var/access_identifier
	var/instance_name
	var/json_path
	var/chat_channels_json_path
	var/chat_commands_json_path
	var/server_commands_json_path
	var/reboot_mode = TGS_REBOOT_MODE_NORMAL
	var/security_level

	var/requesting_new_port = FALSE
	
	var/list/intercepted_message_queue
	
	var/list/custom_commands

	var/list/cached_test_merges
	var/datum/tgs_revision_information/cached_revision

	var/datum/tgs_event_handler/event_handler

	var/export_lock = FALSE
	var/list/last_interop_response

/datum/tgs_api/v4/ApiVersion()
	return "4.0.0.0"

/datum/tgs_api/v4/OnWorldNew(datum/tgs_event_handler/event_handler, minimum_required_security_level)
	json_path = world.params[TGS4_PARAM_INFO_JSON]
	if(!json_path)
		TGS_ERROR_LOG("Missing [TGS4_PARAM_INFO_JSON] world parameter!")
		return
	var/json_file = file2text(json_path)
	if(!json_file)
		TGS_ERROR_LOG("Missing specified json file: [json_path]")
		return
	var/cached_json = json_decode(json_file)
	if(!cached_json)
		TGS_ERROR_LOG("Failed to decode info json: [json_file]")
		return

	access_identifier = cached_json["accessIdentifier"]
	server_commands_json_path = cached_json["serverCommandsJson"]

	if(cached_json["apiValidateOnly"])
		TGS_INFO_LOG("Validating API and exiting...")
		Export(TGS4_COMM_VALIDATE, list(TGS4_PARAMETER_DATA = "[minimum_required_security_level]"))
		del(world)

	security_level = cached_json["securityLevel"]
	chat_channels_json_path = cached_json["chatChannelsJson"]
	chat_commands_json_path = cached_json["chatCommandsJson"]
	src.event_handler = event_handler
	instance_name = cached_json["instanceName"]

	ListCustomCommands()

	var/list/revisionData = cached_json["revision"]
	if(revisionData)
		cached_revision = new
		cached_revision.commit = revisionData["commitSha"]
		cached_revision.origin_commit = revisionData["originCommitSha"]

	cached_test_merges = list()
	var/list/json = cached_json["testMerges"]
	for(var/entry in json)
		var/datum/tgs_revision_information/test_merge/tm = new
		tm.time_merged = text2num(entry["timeMerged"])

		var/list/revInfo = entry["revision"]
		if(revInfo)
			tm.commit = revInfo["commitSha"]
			tm.origin_commit = revInfo["originCommitSha"]

		tm.title = entry["titleAtMerge"]
		tm.body = entry["bodyAtMerge"]
		tm.url = entry["url"]
		tm.author = entry["author"]
		tm.number = entry["number"]
		tm.pull_request_commit = entry["pullRequestRevision"]
		tm.comment = entry["comment"]

		cached_test_merges += tm

	return TRUE

/datum/tgs_api/v4/OnInitializationComplete()
	Export(TGS4_COMM_SERVER_PRIMED)

	var/tgs4_secret_sleep_offline_sauce = 24051994
	var/old_sleep_offline = world.sleep_offline
	world.sleep_offline = tgs4_secret_sleep_offline_sauce
	sleep(1)
	if(world.sleep_offline == tgs4_secret_sleep_offline_sauce)	//if not someone changed it
		world.sleep_offline = old_sleep_offline

/datum/tgs_api/v4/OnTopic(T)
	var/list/params = params2list(T)
	var/their_sCK = params[TGS4_INTEROP_ACCESS_IDENTIFIER]
	if(!their_sCK)
		return FALSE	//continue world/Topic

	if(their_sCK != access_identifier)
		return "Invalid comms key!";

	var/command = params[TGS4_PARAMETER_COMMAND]
	if(!command)
		return "No command!"

	. = TGS4_RESPONSE_SUCCESS

	switch(command)
		if(TGS4_TOPIC_CHAT_COMMAND)
			var/result = HandleCustomCommand(params[TGS4_PARAMETER_DATA])
			if(result == null)
				result = "Error running chat command!"
			return result
		if(TGS4_TOPIC_EVENT)
			intercepted_message_queue = list()
			var/list/event_notification = json_decode(params[TGS4_PARAMETER_DATA])
			var/list/event_parameters = event_notification["Parameters"]

			var/list/event_call = list(event_notification["Type"])
			if(event_parameters)
				event_call += event_parameters

			if(event_handler != null)
				event_handler.HandleEvent(arglist(event_call))

			. = json_encode(intercepted_message_queue)
			intercepted_message_queue = null
			return
		if(TGS4_TOPIC_INTEROP_RESPONSE)
			last_interop_response = json_decode(params[TGS4_PARAMETER_DATA])
			return
		if(TGS4_TOPIC_CHANGE_PORT)
			var/new_port = text2num(params[TGS4_PARAMETER_DATA])
			if (!(new_port > 0))
				return "Invalid port: [new_port]"

			//the topic still completes, miraculously
			//I honestly didn't believe byond could do it
			if(event_handler != null)
				event_handler.HandleEvent(TGS_EVENT_PORT_SWAP, new_port)
			if(!world.OpenPort(new_port))
				return "Port change failed!"
			return
		if(TGS4_TOPIC_CHANGE_REBOOT_MODE)
			var/new_reboot_mode = text2num(params[TGS4_PARAMETER_DATA])
			if(event_handler != null)
				event_handler.HandleEvent(TGS_EVENT_REBOOT_MODE_CHANGE, reboot_mode, new_reboot_mode)
			reboot_mode = new_reboot_mode
			return

	return "Unknown command: [command]"

/datum/tgs_api/v4/proc/Export(command, list/data, override_requesting_new_port = FALSE)
	if(!data)
		data = list()
	data[TGS4_PARAMETER_COMMAND] = command
	var/json = json_encode(data)
	
	while(requesting_new_port && !override_requesting_new_port)
		sleep(1)

	//we need some port open at this point to facilitate return communication
	if(!world.port)
		requesting_new_port = TRUE
		if(!world.OpenPort(0)) //open any port
			TGS_ERROR_LOG("Unable to open random port to retrieve new port![TGS4_PORT_CRITFAIL_MESSAGE]")
			del(world)

		//request a new port
		export_lock = FALSE
		var/list/new_port_json = Export(TGS4_COMM_NEW_PORT, list(TGS4_PARAMETER_DATA = "[world.port]"), TRUE)	//stringify this on purpose
		
		if(!new_port_json)
			TGS_ERROR_LOG("No new port response from server![TGS4_PORT_CRITFAIL_MESSAGE]")
			del(world)

		var/new_port = new_port_json[TGS4_PARAMETER_DATA]
		if(!isnum(new_port) || new_port <= 0)
			TGS_ERROR_LOG("Malformed new port json ([json_encode(new_port_json)])![TGS4_PORT_CRITFAIL_MESSAGE]")
			del(world)

		if(new_port != world.port && !world.OpenPort(new_port))
			TGS_ERROR_LOG("Unable to open port [new_port]![TGS4_PORT_CRITFAIL_MESSAGE]")
			del(world)
		requesting_new_port = FALSE

	while(export_lock)
		sleep(1)
	export_lock = TRUE

	last_interop_response = null
	fdel(server_commands_json_path)
	text2file(json, server_commands_json_path)

	for(var/I = 0; I < EXPORT_TIMEOUT_DS && !last_interop_response; ++I)
		sleep(1)

	if(!last_interop_response)
		TGS_ERROR_LOG("Failed to get export result for: [json]")
	else
		. = last_interop_response

	export_lock = FALSE

/datum/tgs_api/v4/OnReboot()
	var/list/result = Export(TGS4_COMM_WORLD_REBOOT)
	if(!result)
		return
	
	//okay so the standard TGS4 proceedure is: right before rebooting change the port to whatever was sent to us in the above json's data parameter

	var/port = result[TGS4_PARAMETER_DATA]
	if(!isnum(port))
		return	//this is valid, server may just want use to reboot

	if(port == 0)
		//to byond 0 means any port and "none" means close vOv
		port = "none"

	if(!world.OpenPort(port))
		TGS_ERROR_LOG("Unable to set port to [port]!")

/datum/tgs_api/v4/InstanceName()
	return instance_name

/datum/tgs_api/v4/TestMerges()
	return cached_test_merges
	
/datum/tgs_api/v4/EndProcess()
	Export(TGS4_COMM_END_PROCESS)

/datum/tgs_api/v4/Revision()
	return cached_revision

/datum/tgs_api/v4/ChatBroadcast(message, list/channels)
	var/list/ids
	if(length(channels))
		ids = list()
		for(var/I in channels)
			var/datum/tgs_chat_channel/channel = I
			ids += channel.id
	message = list("message" = message, "channelIds" = ids)
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export(TGS4_COMM_CHAT, message)

/datum/tgs_api/v4/ChatTargetedBroadcast(message, admin_only)
	var/list/channels = list()
	for(var/I in ChatChannelInfo())
		var/datum/tgs_chat_channel/channel = I
		if (!channel.is_private_channel && ((channel.is_admin_channel && admin_only) || (!channel.is_admin_channel && !admin_only)))
			channels += channel.id
	message = list("message" = message, "channelIds" = channels)
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export(TGS4_COMM_CHAT, message)

/datum/tgs_api/v4/ChatPrivateMessage(message, datum/tgs_chat_user/user)
	message = list("message" = message, "channelIds" = list(user.channel.id))
	if(intercepted_message_queue)
		intercepted_message_queue += list(message)
	else
		Export(TGS4_COMM_CHAT, message)

/datum/tgs_api/v4/ChatChannelInfo()
	. = list()
	//no caching cause tgs may change this
	var/list/json = json_decode(file2text(chat_channels_json_path))
	for(var/I in json)
		. += DecodeChannel(I)

/datum/tgs_api/v4/proc/DecodeChannel(channel_json)
	var/datum/tgs_chat_channel/channel = new
	channel.id = channel_json["id"]
	channel.friendly_name = channel_json["friendlyName"]
	channel.connection_name = channel_json["connectionName"]
	channel.is_admin_channel = channel_json["isAdminChannel"]
	channel.is_private_channel = channel_json["isPrivateChannel"]
	channel.custom_tag = channel_json["tag"]
	return channel

/datum/tgs_api/v4/SecurityLevel()
	return security_level

/*
The MIT License

Copyright (c) 2017 Jordan Brown

Permission is hereby granted, free of charge,
to any person obtaining a copy of this software and
associated documentation files (the "Software"), to
deal in the Software without restriction, including
without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom
the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR
ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
