#define SERVICE_JSON_PARAM_HELPTEXT "help_text"
#define SERVICE_JSON_PARAM_ADMINONLY "admin_only"
#define SERVICE_JSON_PARAM_REQUIREDPARAMETERS "required_parameters"

/datum/tgs_api/v3210/proc/ListServiceCustomCommands(warnings_only)
	if(!warnings_only)
		. = list()
	var/list/command_name_types = list()
	var/list/warned_command_names = warnings_only ? list() : null
	var/warned_about_the_dangers_of_robutussin = !warnings_only
	for(var/I in typesof(/datum/tgs_chat_command) - /datum/tgs_chat_command)
		if(!warned_about_the_dangers_of_robutussin)
			TGS_ERROR_LOG("Custom chat commands in [ApiVersion()] lacks the /datum/tgs_chat_user/sender.channel field!")
			warned_about_the_dangers_of_robutussin = TRUE
		var/datum/tgs_chat_command/stc = I
		var/command_name = initial(stc.name)
		if(!command_name || findtext(command_name, " ") || findtext(command_name, "'") || findtext(command_name, "\""))
			if(warnings_only && !warned_command_names[command_name])
				TGS_ERROR_LOG("Custom command [command_name] can't be used as it is empty or contains illegal characters!")
				warned_command_names[command_name] = TRUE
			continue
		
		if(command_name_types[command_name])
			if(warnings_only)
				TGS_ERROR_LOG("Custom commands [command_name_types[command_name]] and [stc] have the same name, only [command_name_types[command_name]] will be available!")
			continue
		command_name_types[stc] = command_name

		if(!warnings_only)
			.[command_name] = list(SERVICE_JSON_PARAM_HELPTEXT = initial(stc.help_text), SERVICE_JSON_PARAM_ADMINONLY = initial(stc.admin_only), SERVICE_JSON_PARAM_REQUIREDPARAMETERS = 0)

/datum/tgs_api/v3210/proc/HandleServiceCustomCommand(command, sender, params)
	if(!cached_custom_tgs_chat_commands)
		cached_custom_tgs_chat_commands = list()
		for(var/I in typesof(/datum/tgs_chat_command) - /datum/tgs_chat_command)
			var/datum/tgs_chat_command/stc = I
			cached_custom_tgs_chat_commands[lowertext(initial(stc.name))] = stc

	var/command_type = cached_custom_tgs_chat_commands[command]
	if(!command_type)
		return FALSE
	var/datum/tgs_chat_command/stc = new command_type
	var/datum/tgs_chat_user/user = new
	user.friendly_name = sender
	user.mention = sender
	return stc.Run(user, params) || TRUE

/*

#undef SERVICE_JSON_PARAM_HELPTEXT
#undef SERVICE_JSON_PARAM_ADMINONLY
#undef SERVICE_JSON_PARAM_REQUIREDPARAMETERS

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
