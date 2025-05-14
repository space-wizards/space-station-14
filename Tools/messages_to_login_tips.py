#!/usr/bin/env python

import csv
import textwrap
from sys import argv

# discord-chat-exporter-cli export --format 'Csv' --channel '1328125558139392000'

fp = argv[1]
i = int(argv[2])
skip = int(argv[3])

with open(fp) as f:
    reader = csv.reader(f)
    messages = [row[3] for row in reader]

messages = messages[skip:]

lines = []
for m in messages:
    if not m:
        m = "[EMPTY]"
    elif "\n" in m:
        m = "\n" + textwrap.indent(m, "    ")

    tip = f"login-tips-dataset-{i} = {m}"
    lines.append(tip)

    i += 1

print("\n".join(lines))
