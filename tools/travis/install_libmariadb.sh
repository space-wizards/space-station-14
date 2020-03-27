#!/bin/bash
set -euo pipefail

# get libmariadb, cache it so limmex doesn't get angery
if [ -f $HOME/libmariadb ]; then
	#travis likes to interpret the cache command as it being a file for some reason
	rm $HOME/libmariadb
fi
mkdir -p $HOME/libmariadb
if [ ! -f $HOME/libmariadb/libmariadb.so ]; then
	wget http://www.byond.com/download/db/mariadb_client-2.0.0-linux.tgz
	tar -xvf mariadb_client-2.0.0-linux.tgz
	mv mariadb_client-2.0.0-linux/libmariadb.so $HOME/libmariadb/libmariadb.so
	rm -rf mariadb_client-2.0.0-linux.tgz mariadb_client-2.0.0-linux
fi
