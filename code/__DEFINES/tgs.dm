//tgstation-server DMAPI

//All functions and datums outside this document are subject to change with any version and should not be relied on

//CONFIGURATION

//create this define if you want to do configuration outside of this file
#ifndef TGS_EXTERNAL_CONFIGURATION

//Comment this out once you've filled in the below
#error TGS API unconfigured

//Uncomment this if you wish to allow the game to interact with TGS 3
//This will raise the minimum required security level of your game to TGS_SECURITY_TRUSTED due to it utilizing call()()
//#define TGS_V3_API

//Required interfaces (fill in with your codebase equivalent):

//create a global variable named `Name` and set it to `Value`
//These globals must not be modifiable from anywhere outside of the server tools
#define TGS_DEFINE_AND_SET_GLOBAL(Name, Value)

//Read the value in the global variable `Name`
#define TGS_READ_GLOBAL(Name)

//Set the value in the global variable `Name` to `Value`
#define TGS_WRITE_GLOBAL(Name, Value)

//Disallow ANYONE from reflecting a given `path`, security measure to prevent in-game priveledge escalation
#define TGS_PROTECT_DATUM(Path)

//display an announcement `message` from the server to all players
#define TGS_WORLD_ANNOUNCE(message)

//Notify current in-game administrators of a string `event`
#define TGS_NOTIFY_ADMINS(event)

//Write an info `message` to a server log
#define TGS_INFO_LOG(message)

//Write an error `message` to a server log
#define TGS_ERROR_LOG(message)

//Get the number of connected /clients
#define TGS_CLIENT_COUNT

#endif

//EVENT CODES

#define TGS_EVENT_PORT_SWAP -2	//before a port change is about to happen, extra parameter is new port
#define TGS_EVENT_REBOOT_MODE_CHANGE -1	//before a reboot mode change, extras parameters are the current and new reboot mode enums

//See the descriptions for these codes here: https://github.com/tgstation/tgstation-server/blob/master/src/Tgstation.Server.Host/Components/EventType.cs
#define TGS_EVENT_REPO_RESET_ORIGIN 0
#define TGS_EVENT_REPO_CHECKOUT 1
#define TGS_EVENT_REPO_FETCH 2
#define TGS_EVENT_REPO_MERGE_PULL_REQUEST 3
#define TGS_EVENT_REPO_PRE_SYNCHRONIZE 4
#define TGS_EVENT_BYOND_INSTALL_START 5
#define TGS_EVENT_BYOND_INSTALL_FAIL 6
#define TGS_EVENT_BYOND_ACTIVE_VERSION_CHANGE 7
#define TGS_EVENT_COMPILE_START 8
#define TGS_EVENT_COMPILE_CANCELLED 9
#define TGS_EVENT_COMPILE_FAILURE 10
#define TGS_EVENT_COMPILE_COMPLETE 11
#define TGS_EVENT_INSTANCE_AUTO_UPDATE_START 12
#define TGS_EVENT_REPO_MERGE_CONFLICT 13

//OTHER ENUMS

#define TGS_REBOOT_MODE_NORMAL 0
#define TGS_REBOOT_MODE_SHUTDOWN 1
#define TGS_REBOOT_MODE_RESTART 2

#define TGS_SECURITY_TRUSTED 0
#define TGS_SECURITY_SAFE 1
#define TGS_SECURITY_ULTRASAFE 2

//REQUIRED HOOKS

//Call this somewhere in /world/New() that is always run
//event_handler: optional user defined event handler. The default behaviour is to broadcast the event in english to all connected admin channels
//minimum_required_security_level: The minimum required security level to run the game in which the DMAPI is integrated
/world/proc/TgsNew(datum/tgs_event_handler/event_handler, minimum_required_security_level = TGS_SECURITY_ULTRASAFE)
	return

//Call this when your initializations are complete and your game is ready to play before any player interactions happen
//This may use world.sleep_offline to make this happen so ensure no changes are made to it while this call is running
//Most importantly, before this point, note that any static files or directories may be in use by another server. Your code should account for this
//This function should not be called before ..() in /world/New()
/world/proc/TgsInitializationComplete()
	return

//Put this at the start of /world/Topic()
#define TGS_TOPIC var/tgs_topic_return = TgsTopic(args[1]); if(tgs_topic_return) return tgs_topic_return

//Call this at the beginning of world/Reboot(reason)
/world/proc/TgsReboot()
	return

//DATUM DEFINITIONS
//unless otherwise specified all datums defined here should be considered read-only, warranty void if written

//represents git revision information about the current world build
/datum/tgs_revision_information
	var/commit			//full sha of compiled commit
	var/origin_commit	//full sha of last known remote commit. This may be null if the TGS repository is not currently tracking a remote branch

//represents a version of tgstation-server
/datum/tgs_version
	var/suite			//The suite version, can be >=3

	//this group of variables can be null to represent a wild card
	var/major					//The major version
	var/minor					//The minor version
	var/patch					//The patch version

	var/raw_parameter			//The unparsed parameter
	var/deprefixed_parameter	//The version only bit of raw_parameter

//if the tgs_version is a wildcard version
/datum/tgs_version/proc/Wildcard()
	return

//represents a merge of a GitHub pull request
/datum/tgs_revision_information/test_merge
	var/number				//pull request number
	var/title				//pull request title
	var/body				//pull request body
	var/author				//pull request github author
	var/url					//link to pull request html
	var/pull_request_commit	//commit of the pull request when it was merged
	var/time_merged			//timestamp of when the merge commit for the pull request was created
	var/comment				//optional comment left by the one who initiated the test merge

//represents a connected chat channel
/datum/tgs_chat_channel
	var/id					//internal channel representation
	var/friendly_name		//user friendly channel name
	var/connection_name		//the name of the configured chat connection
	var/is_admin_channel	//if the server operator has marked this channel for game admins only
	var/is_private_channel	//if this is a private chat channel
	var/custom_tag					//user defined string associated with channel

//represents a chat user
/datum/tgs_chat_user
	var/id							//Internal user representation, requires channel to be unique
	var/friendly_name				//The user's public name
	var/mention						//The text to use to ping this user in a message
	var/datum/tgs_chat_channel/channel	//The /datum/tgs_chat_channel this user was from

//user definable callback for handling events
//extra parameters may be specified depending on the event
/datum/tgs_event_handler/proc/HandleEvent(event_code, ...)
	set waitfor = FALSE
	return

//user definable chat command
/datum/tgs_chat_command
	var/name = ""			//the string to trigger this command on a chat bot. e.g. TGS3_BOT: do_this_command
	var/help_text = ""		//help text for this command
	var/admin_only = FALSE	//set to TRUE if this command should only be usable by registered chat admins

//override to implement command
//sender: The tgs_chat_user who send to command
//params: The trimmed string following the command name
//The return value will be stringified and sent to the appropriate chat
/datum/tgs_chat_command/proc/Run(datum/tgs_chat_user/sender, params)
	CRASH("[type] has no implementation for Run()")

//FUNCTIONS

//Returns the respective supported /datum/tgs_version of the API
/world/proc/TgsMaximumAPIVersion()
	return

/world/proc/TgsMinimumAPIVersion()
	return

//Returns TRUE if the world was launched under the server tools and the API matches, FALSE otherwise
//No function below this succeeds if it returns FALSE
/world/proc/TgsAvailable()
	return

//Gets the current /datum/tgs_version of the server tools running the server
/world/proc/TgsVersion()
	return

/world/proc/TgsInstanceName()
	return

//Get the current `/datum/tgs_revision_information`
/world/proc/TgsRevision()
	return

//Get the current BYOND security level
/world/proc/TgsSecurityLevel()
	return

//Gets a list of active `/datum/tgs_revision_information/test_merge`s
/world/proc/TgsTestMerges()
	return

//Forces a hard reboot of BYOND by ending the process
//unlike del(world) clients will try to reconnect
//If the service has not requested a shutdown, the next server will take over
/world/proc/TgsEndProcess()
	return

//Gets a list of connected tgs_chat_channel
/world/proc/TgsChatChannelInfo()
	return
	
//Sends a message to connected game chats
//message: The message to send
//channels: optional channels to limit the broadcast to
/world/proc/TgsChatBroadcast(message, list/channels)
	return

//Send a message to non-admin connected chats
//message: The message to send
//admin_only: If TRUE, message will instead be sent to only admin connected chats
/world/proc/TgsTargetedChatBroadcast(message, admin_only)
	return

//Send a private message to a specific user
//message: The message to send
//user: The /datum/tgs_chat_user to send to
/world/proc/TgsChatPrivateMessage(message, datum/tgs_chat_user/user)
	return

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
