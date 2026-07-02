#!/usr/bin/env python
"""
This script ingests XML files in an nunit format and extracts failed tests.
This script is intended to be part of a CI/CD pipeline to detect failures.
"""

import os
import json
import xmltodict

def testcase_proc(json_input):
    """Recursively process a test-case to extract all failures.

    This function is a helper that expects to be fed with extracted test-case elements.
    """
    if isinstance(json_input, dict):
        for key, value in json_input.items():
            if key == '@result' and value == 'Failed':
                print(json_input)
                yield json_input
    elif isinstance(json_input, list):
        for item in json_input:
            yield from testcase_proc(item)

def item_generator(json_input):
    """Recursively process the test report extract all failures."""
    if isinstance(json_input, dict):
        for key, value in json_input.items():
            if key == 'test-case':
                yield from testcase_proc(value)
            else:
                yield from item_generator(value)
    elif isinstance(json_input, list):
        for item in json_input:
            yield from item_generator(item)

def extract(filename):
    """Extract all failures from an XML file."""
    with open(filename, 'r', encoding='utf-8') as xml_file:
        xml_data = xmltodict.parse(xml_file.read())

    failures = []
    for item in item_generator(xml_data['test-run']):
        failures.append(item)

    return failures

all_fails = []
all_fails.extend(extract('./test_results/logs/Content.Tests.xml'))
all_fails.extend(extract('./test_results/logs/Content.IntegrationTests.xml'))

# Create the list of processed failures to create a matrix for later jobs.
matrix = []
for fail in all_fails:
    matrix.append(
        {
            'name': fail['@name'],
            'fullname': fail['@fullname'],
            'failure': fail['failure']['message'],
            'output': fail['output']
        }
    )

# Clean up the output to ensure that no '@' symbols escape.
json_data = json.dumps(matrix).replace('@', '')

# Write to the action step output
with open(os.environ.get('GITHUB_OUTPUT'), 'a') as f:
    f.write(f'matrix={json_data}\ncount={len(matrix)}\n')
