# Common repo commands, so you don't have to memorize things after being told them once.
# To add a new command, copy the "hello" command below and replace everything after the @
# with the command line command you want to run. Makefiles do a lot more than this - loops,
# dynamic variables, and so on. Read up!

# Makefiles need to use tabs, so if you get a weird error about separators, check that your
# text editor hasn't had a skill issue.

# To run these on Windows you'll need to use Winget to grab a Make port. We recommend
# winget install ezwinports.make

# VARIABLES
hello_world_message=Hello, world! If you can read this then Make is working properly.

# COMMANDS

hello:
	@echo ${hello_world_message}

update-from-upstream:
	git pull --autostash upstream master:master

fetch-from-upstream:
	git fetch upstream master

push-master:
	git push origin master:master
