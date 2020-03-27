#!/bin/sh

has_python="$(command -v python3)"
has_git="$(command -v git)"
has_sudo="$(command -v sudo)"
has_pip="$(command -v pip3)"

set -e

if ! { [ -x "$has_python" ] && [ -x "$has_pip" ] && [ -x "$has_git" ];  }; then
    echo "Installing apt dependencies..."
    if ! [ -x "$has_sudo" ]; then
        apt update
        apt install -y python3 python3-pip git
		rm -rf /var/lib/apt/lists/*
    else
        sudo apt update
        sudo apt install -y python3 python3-pip git
		sudo rm -rf /var/lib/apt/lists/*
    fi
fi

echo "Installing pip dependencies..."
pip3 install PyYaml beautifulsoup4

cd $1

echo "Running changelog script..."
python3 tools/ss13_genchangelog.py html/changelog.html html/changelogs

echo "Committing changes..."
git add html

#we now don't care about failures
set +e
git commit -m "Automatic changelog compile, [ci skip]"
exit 0
