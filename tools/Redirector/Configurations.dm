/*
	Written by contributor Doohl for the /tg/station Open Source project, hosted on Google Code.
	(2012)
 */

var/list/config_stream = list()
var/list/servers = list()
var/list/servernames = list()
var/list/adminfiles = list()
var/list/adminkeys = list()

proc/gen_configs()

	config_stream = dd_file2list("config.txt")

	var/server_gen = 0	// if the stream is looking for servers
	var/admin_gen = 0	// if the stream is looking for admins
	for(var/line in config_stream)

		if(line == "\[SERVERS\]")
			server_gen = 1
			if(admin_gen)
				admin_gen = 0

		else if(line == "\[ADMINS\]")
			admin_gen = 1
			if(server_gen)
				server_gen = 0

		else
			if(findtext(line, ".") && !findtext(line, "##"))
				if(server_gen)
					var/filterline = dd_replacetext(line, " ", "")
					var/serverlink = copytext(filterline, findtext( filterline, ")") + 1)
					servers.Add(serverlink)
					servernames.Add( copytext(line, findtext(line, "("), findtext(line, ")") + 1))

				else if(admin_gen)
					adminfiles.Add(line)
					world << line


	// Generate the list of admins now

	for(var/file in adminfiles)
		var/admin_config_stream = dd_file2list(file)

		for(var/line in admin_config_stream)

			var/akey = copytext(line, 1, findtext(line, " "))
			adminkeys.Add(akey)


