#!/usr/bin/env python

import os
import sys

folders = ["_maps/RandomRuins", "_maps/RandomZLevels", "_maps/shuttles",
           "_maps/templates"]

generated = "_maps/templates.dm"

template_filenames = []

def find_dm(path):
    L = []
    for dirpath, dirnames, filenames in os.walk(path):
        for name in filenames:
            if name.endswith(".dmm"):
                s = os.path.join(dirpath, name)
                s = s.replace("_maps/","")
                L.append(s)
    return L

for folder in folders:
    template_filenames.extend(find_dm(folder))

with open(generated, 'w') as f:
    for template in template_filenames:
        f.write('''#include "{}"\n'''.format(template))

