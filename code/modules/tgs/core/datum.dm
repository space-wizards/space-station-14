TGS_DEFINE_AND_SET_GLOBAL(tgs, null)

/datum/tgs_api
	var/datum/tgs_version/version

/datum/tgs_api/New(datum/tgs_version/version)
	. = ..()
	src.version = version

/datum/tgs_api/latest
	parent_type = /datum/tgs_api/v4

TGS_PROTECT_DATUM(/datum/tgs_api)

/datum/tgs_api/proc/ApiVersion()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/OnWorldNew(datum/tgs_event_handler/event_handler)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/OnInitializationComplete()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/OnTopic(T)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/OnReboot()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/InstanceName()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/TestMerges()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/EndProcess()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/Revision()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/ChatChannelInfo()
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/ChatBroadcast(message, list/channels)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/ChatTargetedBroadcast(message, admin_only)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/ChatPrivateMessage(message, admin_only)
	return TGS_UNIMPLEMENTED

/datum/tgs_api/proc/SecurityLevel()
	return TGS_UNIMPLEMENTED

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
