#!/usr/bin/env python3
import sys
import pickle
import socket


def pack():
	ip = sys.argv[1]
	try:
		data = sys.argv[2:]
	except:
		data = "NO DATA SPECIFIED"

	nudge(pickle.dumps({"ip": ip, "data": data}))


def nudge(data):
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
	s.connect(("localhost", 45678))
	s.send(data)
	s.close()

if __name__ == "__main__" and len(sys.argv) > 1:
	pack()
