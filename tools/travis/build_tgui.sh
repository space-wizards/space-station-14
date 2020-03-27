#!/bin/bash
set -euo pipefail

## Change to project root relative to the script
cd "$(dirname "${0}")/../.."
base_dir="$(pwd)"

## The final authority on what's required to fully build the project
source dependencies.sh

## Setup NVM
if [[ -e ~/.nvm/nvm.sh ]]; then
	source ~/.nvm/nvm.sh
	nvm use "${NODE_VERSION}"
fi

echo "Building 'tgui'"
cd "${base_dir}/tgui"
npm ci
node node_modules/gulp/bin/gulp.js --min

echo "Building 'tgui-next'"
cd "${base_dir}/tgui-next"
bin/tgui --ci
