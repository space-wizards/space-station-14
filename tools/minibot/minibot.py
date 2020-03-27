#!/usr/bin/env python3
# This bot was made by tkdrg.
# Ask #coderbus@irc.rizon.net if this breaks.
# See LICENSE-bot_folder.txt for the license of the files in this folder.
from config import *
import collections
import time
import pickle
import socket
import sys
import threading
import logging
import logging.handlers as handlers
import signal

global irc

# Set to false when we've been killed
running = True
# times we've attempted to connect to server
con_attempts = 0

## Set up a logger object
logger = logging.getLogger('minibot')
logger.setLevel(logging.DEBUG)

# create a file handler (rolls over midnight, keeps 7 days of log
handler = handlers.TimedRotatingFileHandler('minibot.log', when='midnight', backupCount=7)
# most verbose
handler.setLevel(logging.DEBUG)

#only send errors/notifications to the terminal
iohandler = logging.StreamHandler()
iohandler.setLevel(logging.INFO)

# create a logging format
#time - name - level - message (string)
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s', datefmt='%Y-%m-%d %H:%M')
handler.setFormatter(formatter)
iohandler.setFormatter(formatter)

#finally attach them to the logger object
logger.addHandler(handler)
logger.addHandler(iohandler)


def setup_irc_socket():
	global irc, running, con_attempts, logger
	s = socket.socket()
	s.settimeout(240)
	#why not reuse running here? because we want to break this loop if someone sigkills us
	connected = False
	while running and con_attempts < 3 and not connected:
		try:
			s.connect((server, port))
		except socket.error:
			logger.exception("Unable to connect to server {0}:{1}, attempting to reconnect in 20 seconds, Attempt number:{2}".format(server, port, con_attempts))
			con_attempts += 1
			time.sleep(20)
			continue

		logger.info("Connection established to server {0}:{1}.".format(server, port))
		connected = True

	if connected:
		s.send(bytes("NICK {0}\r\n".format(nick), "UTF-8"))
		s.send(bytes("USER {0} {1} {2} :{3}\r\n".format(ident, server, name, realname), "UTF-8"))
	else:
		logger.error("Unable to connect, shutting down")
		running = False
	return s


def setup_nudge_socket():
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	s.bind(("", 45678))  # localhost:nudge_port
	s.listen(5)
	logger.info("Nudge socket up and listening")
	return s


def nudge_handler():
	global irc, running, con_attempts, logger
	nudge = setup_nudge_socket()
	message_queue = collections.deque()
	while running:
		if len(message_queue):
			message = message_queue.popleft()
		else:
			try:
				s, ip = nudge.accept()
			except:
				logger.exception("Nudge socket lost, attempting to reopen.")
				nudge = setup_nudge_socket()
				continue
			rawdata = s.recv(1024)
			s.close()
			data = pickle.loads(rawdata)
			logger.debug(data)
			if data["ip"][0] == "#":
				message = "{0} :AUTOMATIC ANNOUNCEMENT : {1}\r\n".format(data["ip"], str(" ".join(data["data"])))
			else:
				message = "{0} :AUTOMATIC ANNOUNCEMENT : {1} | {2}\r\n".format(defaultchannel, data["ip"], str(" ".join(data["data"])))
		try:
			irc.send(bytes("PRIVMSG {0}".format(message), "UTF-8"))
		except:
			logger.exception("Nudge received without IRC socket, appending to queue.")
			logger.debug("Message: {0}".format(message))
			message_queue.append(message)


def irc_handler():
	global irc, running, con_attempts, logger
	while running:
		try:
			buf = irc.recv(1024).decode("UTF-8").split("\n")
			for i in buf:
				logger.debug(i)
				if i[0:4] == "PING":
					irc.send(bytes("PONG {0}\r\n".format(i[5:]), "UTF-8"))
				else:
					l = i.split(" ")
					if len(l) < 2:
						continue
					elif l[1] == "001":
						logger.info("connected and registered, identifing and joining channels")
						irc.send(bytes("PRIVMSG NickServ :IDENTIFY {0}\r\n".format(password), "UTF-8"))
						time.sleep(1)
						for channel in channels:
							irc.send(bytes("JOIN {0}\r\n".format(channel), "UTF-8"))
					elif l[1] == "477":
						logger.error("Error: Nickname was not registered when joining {0}. Reauthing and retrying...".format(l[3]))
						irc.send(bytes("PRIVMSG NickServ :IDENTIFY {0}\r\n".format(password), "UTF-8"))
						time.sleep(5)
						irc.send(bytes("JOIN {0}\r\n".format(l[3]), "UTF-8"))
					elif l[1] == "433":
						logger.error("Error: Nickname already in use. Attempting to use alt nickname if available, sleeping 60s otherwise...")
						if(altnick):
							irc.send(bytes("NICK {0}\r\n".format(altnick), "UTF-8"))
						else:
							time.sleep(60)
							irc = setup_irc_socket()
		except InterruptedError as e:
			logger.exception("Interrupted, probably killed.")
			continue
		except:
			logger.exception("Lost connection to IRC server.")
			irc = setup_irc_socket()

def signal_handler(signum, frame):
	global irc, running, con_attempts, logger
	logger.info("Recieved term kill, closing")
	running = False

if __name__ == "__main__":
	#listen to signals (quit on ctrl c or kill from OS)
	signal.signal(signal.SIGINT, signal_handler)
	irc = setup_irc_socket()
	t = threading.Thread(target=nudge_handler)
	t.daemon = True
	t.start()
	irc_handler()
