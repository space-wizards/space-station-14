/*
	Written by contributor Doohl for the /tg/station Open Source project, hosted on Google Code.
	(2012)
 */

/* TODO: work on server selection for detected admins */


#define ADMINS 1
#define PLAYERS 0

var/player_weight = 1 // players are more likely to join a server with less players
var/admin_weight = 5 // admins are more likely to join a server with less admins

var/player_substr = "players=" // search for this substring to locate # of players
var/admin_substr  = "admins=" // search for this to locate # of admins

/world
	name = "TGstation Redirector"

/world/New()
	..()
	gen_configs()

/datum/server
	var/players = 0
	var/admins = 0
	var/weight = 0 // lower weight is good; highet weight is bad

	var/link = ""

/mob/Login()
	..()

	var/list/weights = list()
	var/list/servers = list()
	for(var/x in global.servers)

		world << "[x] [servernames[ global.servers.Find(x) ]]"

		var/info = world.Export("[x]?status")
		var/datum/server/S = new()
		S.players = extract(info, PLAYERS)
		S.admins = extract(info, ADMINS)

		S.weight += player_weight * S.players
		S.link = x

		world << S.players
		world << S.admins

		weights.Add(S.weight)
		servers.Add(S)

	var/lowest = min(weights)
	var/serverlink
	for(var/datum/server/S in servers)
		if(S.weight == lowest)
			serverlink = S.link

	src << link(serverlink)

/proc/extract(var/data, var/type = PLAYERS)

	var/nextpos = 0

	if(type == PLAYERS)

		nextpos = findtextEx(data, player_substr)
		nextpos += length(player_substr)

	else

		nextpos = findtextEx(data, admin_substr)
		nextpos += length(admin_substr)

	var/returnval = ""

	for(var/i = 1, i <= 10, i++)

		var/interval = copytext(data, nextpos + (i-1), nextpos + i)
		if(interval == "&")
			break
		else
			returnval += interval

	return returnval
