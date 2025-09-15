#!/usr/bin/env python3

import os
import re

from project import Project

RE_INNER_DASH = re.compile(r'(?<=\s)-(?=\s)')

def should_skip(line: str) -> bool:
    stripped = line.lstrip()
    return not line.strip() or stripped.startswith(('#', '-'))

def replace_dashes(text: str) -> str:
    ts = text.strip()
    if ts.startswith('-') or ts.endswith('-'):
        return text
    return RE_INNER_DASH.sub('—', text)

def process_file(filepath: str):
    with open(filepath, encoding='utf-8') as f:
        lines = f.readlines()

    new_lines = []
    changed = False

    for line in lines:
        if should_skip(line):
            new_lines.append(line)
            continue

        if '=' in line:
            key, val = line.split('=', 1)
            new_val = replace_dashes(val)
            new_lines.append(f"{key}={new_val}")
            if new_val != val:
                changed = True
        else:
            new_line = replace_dashes(line)
            new_lines.append(new_line)
            if new_line != line:
                changed = True

    if changed:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.writelines(new_lines)
        print(f"Обновлён: {filepath}")


def main():
    project = Project()
    root = project.ru_locale_dir_path

    for dirpath, _, files in os.walk(root):
        for fn in files:
            if fn.lower().endswith('.ftl'):
                process_file(os.path.join(dirpath, fn))

if __name__ == '__main__':
    main()
