#!/bin/bash

#If you hit github's rate limit, add a 3rd parameter here that is a github personal access token
./CreditsTool tgstation tgstation

rm ../../icons/credits.dmi

for filename in credit_pngs/*.png; do
	realname=$(basename "$filename")
	java -jar ../dmitool/dmitool.jar import ../../icons/credits.dmi "${realname%.*}" "$filename"
done

rm -rf credit_pngs
