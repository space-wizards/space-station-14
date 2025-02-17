from fluent.syntax import ast
from fluentast import FluentAstAbstract


class FluentAstManager:
    def __init__(self, sourse_parsed: ast.Resource, target_parsed: ast.Resource):
        self.sourse_parsed = sourse_parsed
        self.target_parsed = target_parsed
        self.source_elements = list(map(lambda e: FluentAstAbstract.create_element(e), sourse_parsed.body))
        self.target_elements = list(map(lambda e: FluentAstAbstract.create_element(e), target_parsed.body))

    def update_by_index(self, index, update_element: ast.Message):
        source_element = None

        try:
            source_element = self.sourse_parsed.body[index]
        except:
            raise Exception(f'Нет элемента с индексом {index}')

        if not source_element:
            raise Exception(f'Элемен с индексом {index} не существует')

        self.sourse_parsed.body[index] = update_element

        return self.sourse_parsed
