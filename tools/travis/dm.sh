#!/bin/bash

dmepath=""
retval=1

for var
do
	if [[ $var != -* && $var == *.dme ]]
	then
		dmepath=`echo $var | sed -r 's/.{4}$//'`
		break
	fi
done

if [[ $dmepath == "" ]]
then
	echo "No .dme file specified, aborting."
	exit 1
fi

if [[ -a $dmepath.mdme ]]
then
	rm $dmepath.mdme
fi

cp $dmepath.dme $dmepath.mdme
if [[ $? != 0 ]]
then
	echo "Failed to make modified dme, aborting."
	exit 2
fi

for var
do
	arg=`echo $var | sed -r 's/^.{2}//'`
	if [[ $var == -D* ]]
	then
		sed -i '1s/^/#define '$arg'\n/' $dmepath.mdme
		continue
	fi
done

#windows
if [[ `uname` == MINGW* ]]
then
	dm=""

	if hash dm.exe 2>/dev/null
	then
		dm='dm.exe'
	elif [[ -a '/c/Program Files (x86)/BYOND/bin/dm.exe' ]]
	then
		dm='/c/Program Files (x86)/BYOND/bin/dm.exe'
	elif [[ -a '/c/Program Files/BYOND/bin/dm.exe' ]]
	then
		dm='/c/Program Files/BYOND/bin/dm.exe'
	fi

	if [[ $dm == "" ]]
	then
		echo "Couldn't find the DreamMaker executable, aborting."
		exit 3
	fi

	"$dm" $dmepath.mdme 2>&1 | tee result.log
	retval=$?
	if ! grep '\- 0 errors, 0 warnings' result.log
	then
		retval=1 #hard fail, due to warnings or errors
	fi
else
	if hash DreamMaker 2>/dev/null
	then
		DreamMaker -max_errors 0 $dmepath.mdme 2>&1 | tee result.log
		retval=$?
		if ! grep '\- 0 errors, 0 warnings' result.log
		then
			retval=1 #hard fail, due to warnings or errors
		fi
	else
		echo "Couldn't find the DreamMaker executable, aborting."
		exit 3
	fi
fi

mv $dmepath.mdme.dmb $dmepath.dmb
mv $dmepath.mdme.rsc $dmepath.rsc

rm $dmepath.mdme

exit $retval
