#!/usr/bin/env python

import subprocess
import os
import sys
import argparse
import time
from subprocess import PIPE, STDOUT

null = open("/dev/null", "wb")

def wait(p):
    rc = p.wait()
    if rc != 0:
        p = play("sound/misc/compiler-failure.ogg")
        p.wait()
        assert p.returncode == 0
        sys.exit(rc)

def play(soundfile):
    p = subprocess.Popen(["play", soundfile], stdout=null, stderr=null)
    assert p.wait() == 0
    return p

def stage1():
    p = subprocess.Popen("(cd tgui; /bin/bash ./build.sh)", shell=True)
    wait(p)
    play("sound/misc/compiler-stage1.ogg")

def stage2(map):
    if map:
        txt = "-M{}".format(map)
    else:
        txt = ''
    args = "bash tools/travis/dm.sh {} tgstation.dme".format(txt)
    print(args)
    p = subprocess.Popen(args, shell=True)
    wait(p)

def stage3(profile_mode=False):
    start_time = time.time()
    play("sound/misc/compiler-stage2.ogg")
    logfile = open('server.log~','w')
    p = subprocess.Popen(
        "DreamDaemon tgstation.dmb 25001 -trusted",
        shell=True, stdout=PIPE, stderr=STDOUT)
    try:
        while p.returncode is None:
            stdout = p.stdout.readline()
            if "Initializations complete" in stdout:
                play("sound/misc/server-ready.ogg")
                time_taken = time.time() - start_time
                print("{} seconds taken to fully start".format(time_taken))
            if "Map is ready." in stdout:
                time_taken = time.time() - start_time
                print("{} seconds for initial map loading".format(time_taken))
                if profile_mode:
                    return time_taken
            sys.stdout.write(stdout)
            sys.stdout.flush()
            logfile.write(stdout)
    finally:
        logfile.flush()
        os.fsync(logfile.fileno())
        logfile.close()
        p.kill()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('-s','---stage',default=1,type=int)
    parser.add_argument('--only',action='store_true')
    parser.add_argument('-m','--map',type=str)
    parser.add_argument('--profile-mode',action='store_true')
    args = parser.parse_args()
    stage = args.stage
    assert stage in (1,2,3)
    if stage == 1:
        stage1()
        if not args.only:
            stage = 2
    if stage == 2:
        stage2(args.map)
        if not args.only:
            stage = 3
    if stage == 3:
        value = stage3(profile_mode=args.profile_mode)
        with open('profile~', 'a') as f:
            f.write("{}\n".format(value))

if __name__=='__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
