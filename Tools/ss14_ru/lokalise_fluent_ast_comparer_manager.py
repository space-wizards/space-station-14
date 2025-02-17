from fluent.syntax import ast

from fluentast import FluentAstMessage
from fluentastcomparer import FluentAstComparer
from fluentastmanager import FluentAstManager


class LokaliseFluentAstComparerManager:
    def __init__(self, sourse_parsed: ast.Resource, target_parsed: ast.Resource):
        self.sourse_parsed = sourse_parsed
        self.target_parsed = target_parsed
        self.comparer = FluentAstComparer(sourse_parsed, target_parsed)
        self.ast_manager = FluentAstManager(sourse_parsed, target_parsed)

    def for_update(self):
        for_update = self.comparer.get_not_equal_exist_values_with_attrs()

        if not len(for_update):
            return []

        return for_update

    def update(self, for_update):
        for update in for_update:
            idx = self.comparer.sourse_parsed.body.index(update.element)
            update_mess: FluentAstMessage = self.comparer.find_message_by_id_name(update.get_id_name(),
                                                                                  self.comparer.target_elements)
            self.ast_manager.update_by_index(idx, update_mess.element)

        return self.ast_manager.sourse_parsed

    def for_delete(self):
        for_delete = self.comparer.get_not_exist_id_names()

        if len(for_delete):
            keys = list(map(lambda el: el.get_id_name(), for_delete))
            print(f'Следующие ключи есть в lokalise, но нет в файле. Возможно, их нужно удалить из lokalise: {keys}')

        return for_delete

    def for_create(self):
        for_create = self.comparer.get_not_equal_id_names()

        if len(for_create):
            keys = list(map(lambda el: el.get_id_name(), for_create))
            print(f'Следующих ключей файла нет в lokalise. Необходимо добавить: {keys}')

        return for_create
