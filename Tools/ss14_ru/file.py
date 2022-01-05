import typing

from fluent.syntax import ast


class File:
    def __init__(self, full_path):
        self.full_path = full_path

    def read_data(self):
        file = open(self.full_path, 'r')
        # replace необходим для того, чтобы 1-е сообщение не считалось ast.Junk
        file_data = file.read().replace('﻿', '')
        file.close()

        return file_data

    def save_data(self, file_data: typing.AnyStr):
        file = open(self.full_path, 'w')
        file.write(file_data)
        file.close()

    def get_relative_path(self, base_path):
        return self.full_path.replace(f'{base_path}/', '')

    def get_parent_dir(self):
        splitted_path = self.full_path.split('/')[0:-1]
        return '/'.join(splitted_path)




class FluentFile(File):
    def __init__(self, full_path):
        super().__init__(full_path)
        self.full_path = full_path

    def parse_data(self, file_data: typing.AnyStr):
        from fluent.syntax import FluentParser

        return FluentParser().parse(file_data)

    def serialize_data(self, parsed_file_data: ast.Resource):
        from fluent.syntax import FluentSerializer

        return FluentSerializer(with_junk=True).serialize(parsed_file_data)
