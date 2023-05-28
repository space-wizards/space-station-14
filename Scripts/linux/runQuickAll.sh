#!/usr/bin/env sh

# make sure to start from script dir
if [ "$(dirname $0)" != "." ]; then
    cd "$(dirname $0)"
fi

echo "will run both server and client in the same terminal so will give you both outputs at once"
echo "dont mind fatl error relating to port 1212 does not seem to change anything"

sh -e runQuickServer.sh &
sh -e runQuickClient.sh

exit
