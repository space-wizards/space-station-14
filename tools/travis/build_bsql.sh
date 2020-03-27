#!/usr/bin/env bash
set -euo pipefail

source dependencies.sh

mkdir -p BSQL
cd BSQL
git init
git remote add origin https://github.com/tgstation/BSQL
git fetch --depth 1 origin $BSQL_VERSION
git checkout FETCH_HEAD

mkdir -p artifacts
cd artifacts
export CXX=g++-7
# The -D will be unnecessary past BSQL v1.4.0.0
cmake .. -DMARIA_LIBRARY=/usr/lib/i386-linux-gnu/libmariadb.so
make

mkdir -p ~/.byond/bin
ln -s $PWD/src/BSQL/libBSQL.so ../../libBSQL.so
