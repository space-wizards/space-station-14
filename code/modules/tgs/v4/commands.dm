/datum/tgs_api/v4/proc/ListCustomCommands()
	var/results = list()
	custom_commands = list()
	for(var/I in typesof(/datum/tgs_chat_command) - /datum/tgs_chat_command)
		var/datum/tgs_chat_command/stc = new I
		var/command_name = stc.name
		if(!command_name || findtext(command_name, " ") || findtext(command_name, "'") || findtext(command_name, "\""))
			TGS_ERROR_LOG("Custom command [command_name] ([I]) can't be used as it is empty or contains illegal characters!")
			continue

		if(results[command_name])
			var/datum/other = custom_commands[command_name]
			TGS_ERROR_LOG("Custom commands [other.type] and [I] have the same name (\"[command_name]\"), only [other.type] will be available!")
			continue
		results += list(list("name" = command_name, "help_text" = stc.help_text, "admin_only" = stc.admin_only))
		custom_commands[command_name] = stc

	var/commands_file = chat_commands_json_path
	if(!commands_file)
		return
	text2file(json_encode(results), commands_file)

/datum/tgs_api/v4/proc/HandleCustomCommand(command_json)
	var/list/data = json_decode(command_json)
	var/command = data["command"]
	var/user = data["user"]
	var/params = data["params"]

	var/datum/tgs_chat_user/u = new
	u.id = user["id"]
	u.friendly_name = user["friendlyName"]
	u.mention = user["mention"]
	u.channel = DecodeChannel(user["channel"])

	var/datum/tgs_chat_command/sc = custom_commands[command]
	if(sc)
		var/result = sc.Run(u, params)
		if(result == null)
			result = ""
		return result
	return "Unknown command: [command]!"

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
