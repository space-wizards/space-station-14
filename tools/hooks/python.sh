#!/bin/bash
set -e
if command -v python3 >/dev/null 2>&1; then
	PY=python3
else
	PY=python
fi
PATHSEP=$($PY - <<'EOF'
import sys, os
if sys.version_info.major != 3 or sys.version_info.minor < 6:
	sys.stderr.write("Python 3.6+ is required: " + sys.version + "\n")
	exit(1)
print(os.pathsep)
EOF
)
export PYTHONPATH=tools/mapmerge2/${PATHSEP}${PYTHONPATH}
$PY "$@"
