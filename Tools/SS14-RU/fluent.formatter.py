#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import os
import glob
import typing
from pathlib import Path
from glob import glob
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer


######################################### Class defifitions ############################################################
class FluentFile:
    def __init__(self, full_path):
        self.full_path = full_path

    def read_data(self):
        file = open(self.full_path, 'r')
        file_data = file.read()
        file.close()

        return file_data

    def parse_data(self, file_data: typing.AnyStr):
        return fluent_parser.parse(file_data)

    def serialize_data(self, parsed_file_data: ast.Resource):
        return fluent_serializer.serialize(parsed_file_data)

    def save_data(self, file_data: typing.AnyStr):
        file = open(self.full_path, 'w')
        file.write(file_data)
        file.close()


class FluentFormatter:
    def __init__(self, directory: typing.AnyStr):
        self.directory = directory

    def format(self):
        files_paths_list = glob(f'{self.directory}/**/*.ftl', recursive=True)

        for file_path in files_paths_list:
            try:
                file = FluentFile(file_path)
            except:
                continue

            # replace необходим для того, чтобы 1-е сообщение не считалось ast.Junk
            file_data = file.read_data().replace('﻿', '')
            parsed_file_data = file.parse_data(file_data)
            serialized_file_data = file.serialize_data(parsed_file_data)
            file.save_data(serialized_file_data)


######################################### Var defifitions ##############################################################

# путь корневой директории
root_dir_path = Path(os.path.abspath(os.curdir)).parent.parent.resolve()
# путь директории русскоязычных переводов
ru_translations_dir_path = os.path.join(root_dir_path, 'Resources', 'Locale', 'ru-RU')
# путь директории англоязычных переводов
en_translations_dir_path = os.path.join(root_dir_path, 'Resources', 'Locale', 'en-US')

fluent_parser = FluentParser()
fluent_serializer = FluentSerializer()
formatter = FluentFormatter(ru_translations_dir_path)

########################################################################################################################
formatter.format()
