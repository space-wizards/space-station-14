#define REBOOT_MODE_NORMAL 0
#define REBOOT_MODE_HARD 1
#define REBOOT_MODE_SHUTDOWN 2

#define SERVICE_WORLD_PARAM "server_service"
#define SERVICE_INSTANCE_PARAM "server_instance"
#define SERVICE_PR_TEST_JSON "prtestjob.json"
#define SERVICE_INTERFACE_DLL "TGDreamDaemonBridge.dll"
#define SERVICE_INTERFACE_FUNCTION "DDEntryPoint"

#define SERVICE_CMD_HARD_REBOOT "hard_reboot"
#define SERVICE_CMD_GRACEFUL_SHUTDOWN "graceful_shutdown"
#define SERVICE_CMD_WORLD_ANNOUNCE "world_announce"
#define SERVICE_CMD_LIST_CUSTOM "list_custom_commands"
#define SERVICE_CMD_API_COMPATIBLE "api_compat"
#define SERVICE_CMD_PLAYER_COUNT "client_count"

#define SERVICE_CMD_PARAM_KEY "serviceCommsKey"
#define SERVICE_CMD_PARAM_COMMAND "command"
#define SERVICE_CMD_PARAM_SENDER "sender"
#define SERVICE_CMD_PARAM_CUSTOM "custom"

#define SERVICE_REQUEST_KILL_PROCESS "killme"
#define SERVICE_REQUEST_IRC_BROADCAST "irc"
#define SERVICE_REQUEST_IRC_ADMIN_CHANNEL_MESSAGE "send2irc"
#define SERVICE_REQUEST_WORLD_REBOOT "worldreboot"
#define SERVICE_REQUEST_API_VERSION "api_ver"

#define SERVICE_RETURN_SUCCESS "SUCCESS"

/datum/tgs_api/v3210
	var/reboot_mode = REBOOT_MODE_NORMAL
	var/comms_key
	var/instance_name
	var/originmastercommit
	var/commit
	var/list/cached_custom_tgs_chat_commands
	var/warned_revison = FALSE
	var/warned_custom_commands = FALSE

/datum/tgs_api/v3210/ApiVersion()
	return "3.2.1.0"

/datum/tgs_api/v3210/proc/trim_left(text)
	for (var/i = 1 to length(text))
		if (text2ascii(text, i) > 32)
			return copytext(text, i)
	return ""

/datum/tgs_api/v3210/proc/trim_right(text)
	for (var/i = length(text), i > 0, i--)
		if (text2ascii(text, i) > 32)
			return copytext(text, 1, i + 1)
	return ""

/datum/tgs_api/v3210/proc/file2list(filename)
	return splittext(trim_left(trim_right(file2text(filename))), "\n")

/datum/tgs_api/v3210/OnWorldNew(datum/tgs_event_handler/event_handler, minimum_required_security_level)	//don't use event handling in this version
	. = FALSE

	comms_key = world.params[SERVICE_WORLD_PARAM]
	instance_name = world.params[SERVICE_INSTANCE_PARAM]
	if(!instance_name)
		instance_name = "TG Station Server"	//maybe just upgraded

	var/list/logs = file2list(".git/logs/HEAD")
	if(logs.len)
		logs = splittext(logs[logs.len - 1], " ")
		commit = logs[2]
	logs = file2list(".git/logs/refs/remotes/origin/master")
	if(logs.len)
		originmastercommit = splittext(logs[logs.len - 1], " ")[2]

	if(world.system_type != MS_WINDOWS)
		TGS_ERROR_LOG("This API version is only supported on Windows. Not running on Windows. Aborting initialization!")
		return
	ListServiceCustomCommands(TRUE)
	ExportService("[SERVICE_REQUEST_API_VERSION] [ApiVersion()]", TRUE)
	return TRUE

//nothing to do for v3
/datum/tgs_api/v3210/OnInitializationComplete()
	return

/datum/tgs_api/v3210/InstanceName()
	return world.params[SERVICE_INSTANCE_PARAM]

/datum/tgs_api/v3210/proc/ExportService(command, skip_compat_check = FALSE)
	. = FALSE
	if(skip_compat_check && !fexists(SERVICE_INTERFACE_DLL))
		TGS_ERROR_LOG("Service parameter present but no interface DLL detected. This is symptomatic of running a service less than version 3.1! Please upgrade.")
		return
	call(SERVICE_INTERFACE_DLL, SERVICE_INTERFACE_FUNCTION)(instance_name, command)	//trust no retval
	return TRUE

/datum/tgs_api/v3210/OnTopic(T)
	var/list/params = params2list(T)
	var/their_sCK = params[SERVICE_CMD_PARAM_KEY]
	if(!their_sCK)
		return FALSE	//continue world/Topic

	if(their_sCK != comms_key)
		return "Invalid comms key!";

	var/command = params[SERVICE_CMD_PARAM_COMMAND]
	if(!command)
		return "No command!"

	switch(command)
		if(SERVICE_CMD_API_COMPATIBLE)
			return SERVICE_RETURN_SUCCESS
		if(SERVICE_CMD_HARD_REBOOT)
			if(reboot_mode != REBOOT_MODE_HARD)
				reboot_mode = REBOOT_MODE_HARD
				TGS_INFO_LOG("Hard reboot requested by service")
				TGS_NOTIFY_ADMINS("The world will hard reboot at the end of the game. Requested by TGS.")
		if(SERVICE_CMD_GRACEFUL_SHUTDOWN)
			if(reboot_mode != REBOOT_MODE_SHUTDOWN)
				reboot_mode = REBOOT_MODE_SHUTDOWN
				TGS_INFO_LOG("Shutdown requested by service")
				TGS_NOTIFY_ADMINS("The world will shutdown at the end of the game. Requested by TGS.")
		if(SERVICE_CMD_WORLD_ANNOUNCE)
			var/msg = params["message"]
			if(!istext(msg) || !msg)
				return "No message set!"
			TGS_WORLD_ANNOUNCE(msg)
			return SERVICE_RETURN_SUCCESS
		if(SERVICE_CMD_PLAYER_COUNT)
			return "[TGS_CLIENT_COUNT]"
		if(SERVICE_CMD_LIST_CUSTOM)
			return json_encode(ListServiceCustomCommands(FALSE))
		else
			var/custom_command_result = HandleServiceCustomCommand(lowertext(command), params[SERVICE_CMD_PARAM_SENDER], params[SERVICE_CMD_PARAM_CUSTOM])
			if(custom_command_result)
				return istext(custom_command_result) ? custom_command_result : SERVICE_RETURN_SUCCESS
	return "Unknown command: [command]"

/datum/tgs_api/v3210/OnReboot()
	switch(reboot_mode)
		if(REBOOT_MODE_HARD)
			TGS_WORLD_ANNOUNCE("Hard reboot triggered, you will automatically reconnect...")
			EndProcess()
		if(REBOOT_MODE_SHUTDOWN)
			TGS_WORLD_ANNOUNCE("The server is shutting down...")
			EndProcess()
		else
			ExportService(SERVICE_REQUEST_WORLD_REBOOT) //just let em know

/datum/tgs_api/v3210/TestMerges()
	//do the best we can here as the datum can't be completed using the v3 api
	. = list()
	if(!fexists(SERVICE_PR_TEST_JSON))
		return
	var/list/json = json_decode(file2text(SERVICE_PR_TEST_JSON))
	if(!json)
		return
	for(var/I in json)
		var/datum/tgs_revision_information/test_merge/tm = new
		tm.number = text2num(I)
		var/list/entry = json[I]
		tm.pull_request_commit = entry["commit"]
		tm.author = entry["author"]
		tm.title = entry["title"]
		. += tm

/datum/tgs_api/v3210/Revision()
	if(!warned_revison)
		TGS_ERROR_LOG("Use of TgsRevision on [ApiVersion()] origin_commit only points to master!")
		warned_revison = TRUE
	var/datum/tgs_revision_information/ri = new
	ri.commit = commit
	ri.origin_commit = originmastercommit
	return ri

/datum/tgs_api/v3210/EndProcess()
	sleep(world.tick_lag)	//flush the buffers
	ExportService(SERVICE_REQUEST_KILL_PROCESS)

/datum/tgs_api/v3210/ChatChannelInfo()
	return list()

/datum/tgs_api/v3210/ChatBroadcast(message, list/channels)
	if(channels)
		return TGS_UNIMPLEMENTED
	ChatTargetedBroadcast(message, TRUE)
	ChatTargetedBroadcast(message, FALSE)

/datum/tgs_api/v3210/ChatTargetedBroadcast(message, admin_only)
	ExportService("[admin_only ? SERVICE_REQUEST_IRC_ADMIN_CHANNEL_MESSAGE : SERVICE_REQUEST_IRC_BROADCAST] [message]")

/datum/tgs_api/v3210/ChatPrivateMessage(message, datum/tgs_chat_user/user)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/v3210/SecurityLevel()
	return TGS_SECURITY_TRUSTED

#undef REBOOT_MODE_NORMAL
#undef REBOOT_MODE_HARD
#undef REBOOT_MODE_SHUTDOWN

#undef SERVICE_WORLD_PARAM
#undef SERVICE_INSTANCE_PARAM
#undef SERVICE_PR_TEST_JSON
#undef SERVICE_INTERFACE_DLL
#undef SERVICE_INTERFACE_FUNCTION

#undef SERVICE_CMD_HARD_REBOOT
#undef SERVICE_CMD_GRACEFUL_SHUTDOWN
#undef SERVICE_CMD_WORLD_ANNOUNCE
#undef SERVICE_CMD_LIST_CUSTOM
#undef SERVICE_CMD_API_COMPATIBLE
#undef SERVICE_CMD_PLAYER_COUNT

#undef SERVICE_CMD_PARAM_KEY
#undef SERVICE_CMD_PARAM_COMMAND
#undef SERVICE_CMD_PARAM_SENDER
#undef SERVICE_CMD_PARAM_CUSTOM

#undef SERVICE_REQUEST_KILL_PROCESS
#undef SERVICE_REQUEST_IRC_BROADCAST
#undef SERVICE_REQUEST_IRC_ADMIN_CHANNEL_MESSAGE
#undef SERVICE_REQUEST_WORLD_REBOOT
#undef SERVICE_REQUEST_API_VERSION

#undef SERVICE_RETURN_SUCCESS

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
