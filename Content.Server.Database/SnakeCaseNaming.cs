using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Server.Database
{
    public class SnakeCaseExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info { get; }

        public SnakeCaseExtension() {
            Info = new ExtensionInfo(this);
        }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSnakeCase();
        }

        public void Validate(IDbContextOptions options) {}

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) {}

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => "Snake Case Extension";

            public override int GetServiceProviderHashCode()
            {
                return 0;
            }

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            {
                return other is ExtensionInfo;
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }
        }
    }

    public static class SnakeCaseServiceCollectionExtensions
    {
        public static IServiceCollection AddSnakeCase(
            this IServiceCollection serviceCollection)
        {
            new EntityFrameworkServicesBuilder(serviceCollection)
                .TryAdd<IConventionSetPlugin, SnakeCaseConventionSetPlugin>();

            return serviceCollection;
        }
    }

    public class SnakeCaseConventionSetPlugin : IConventionSetPlugin
    {
        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var convention = new SnakeCaseConvention();

            conventionSet.EntityTypeAddedConventions.Add(convention);
            conventionSet.EntityTypeAnnotationChangedConventions.Add(convention);
            conventionSet.PropertyAddedConventions.Add(convention);
            conventionSet.ForeignKeyOwnershipChangedConventions.Add(convention);
            conventionSet.KeyAddedConventions.Add(convention);
            conventionSet.ForeignKeyAddedConventions.Add(convention);
            conventionSet.EntityTypeBaseTypeChangedConventions.Add(convention);
            conventionSet.ModelFinalizingConventions.Add(convention);

            return conventionSet;
        }
    }

    public partial class SnakeCaseConvention :
        IEntityTypeAddedConvention,
        IEntityTypeAnnotationChangedConvention,
        IPropertyAddedConvention,
        IForeignKeyOwnershipChangedConvention,
        IKeyAddedConvention,
        IForeignKeyAddedConvention,
        IEntityTypeBaseTypeChangedConvention,
        IModelFinalizingConvention
    {
        private static readonly StoreObjectType[] _storeObjectTypes
            = { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function, StoreObjectType.SqlQuery };

        public SnakeCaseConvention() {}

        public static string RewriteName(string name)
        {
            return UpperCaseLocator()
                .Replace(
                    name,
                    (Match match) => {
                        if (match.Index == 0 && (match.Value == "FK" || match.Value == "PK" ||  match.Value == "IX")) {
                            return match.Value;
                        }
                        if (match.Value == "HWI")
                            return (match.Index == 0 ? "" : "_") + "hwi";
                        if (match.Index == 0)
                            return match.Value.ToLower();
                        if (match.Length > 1)
                            return $"_{match.Value[..^1].ToLower()}_{match.Value[^1..^0].ToLower()}";

                        // Do not add a _ if there is already one before this. This happens with owned entities.
                        if (name[match.Index - 1] == '_')
                            return match.Value.ToLower();

                        return "_" + match.Value.ToLower();
                    }
                );
        }

        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            if (entityType.ClrType == typeof(Microsoft.EntityFrameworkCore.Migrations.HistoryRow))
                return;

            if (entityType.BaseType is null)
            {
                entityTypeBuilder.ToTable(RewriteName(entityType.GetTableName()!), entityType.GetSchema());

                if (entityType.GetViewNameConfigurationSource() == ConfigurationSource.Convention)
                {
                    entityTypeBuilder.ToView(RewriteName(entityType.GetViewName()!), entityType.GetViewSchema());
                }
            }
        }

        public void ProcessEntityTypeBaseTypeChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionEntityType? newBaseType,
            IConventionEntityType? oldBaseType,
            IConventionContext<IConventionEntityType> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            if (newBaseType is null)
            {
                entityTypeBuilder.ToTable(RewriteName(entityType.GetTableName()!), entityType.GetSchema());
            }
            else
            {
                entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.Schema);
            }
        }

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder,
            IConventionContext<IConventionPropertyBuilder> context)
        {
            RewriteColumnName(propertyBuilder);
        }

        public void ProcessForeignKeyOwnershipChanged(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
        {
            var foreignKey = relationshipBuilder.Metadata;
            var ownedEntityType = foreignKey.DeclaringEntityType;

            if (foreignKey.IsOwnership && ownedEntityType.GetTableNameConfigurationSource() != ConfigurationSource.Explicit)
            {
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

                ownedEntityType.FindPrimaryKey()?.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);

                foreach (var property in ownedEntityType.GetProperties())
                {
                    RewriteColumnName(property.Builder);
                }
            }
        }

        public void ProcessEntityTypeAnnotationChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            string name,
            IConventionAnnotation? annotation,
            IConventionAnnotation? oldAnnotation,
            IConventionContext<IConventionAnnotation> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            if (entityType.ClrType == typeof(Microsoft.EntityFrameworkCore.Migrations.HistoryRow))
                return;

            if (name != RelationalAnnotationNames.TableName
                || StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is not StoreObjectIdentifier tableIdentifier)
            {
                return;
            }

            if (entityType.FindPrimaryKey() is { } primaryKey)
            {
                if (entityType.FindRowInternalForeignKeys(tableIdentifier).FirstOrDefault() is null
                    && (entityType.BaseType is null || entityType.GetTableName() == entityType.BaseType.GetTableName()))
                {
                    primaryKey.Builder.HasName(RewriteName(primaryKey.GetDefaultName()!));
                }
                else
                {
                    primaryKey.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
                }
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.Builder.HasConstraintName(RewriteName(foreignKey.GetDefaultName()!));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.Builder.HasDatabaseName(RewriteName(index.GetDefaultDatabaseName()!));
            }

            if (annotation?.Value is not null
                && entityType.FindOwnership() is { } ownership
                && (string)annotation.Value != ownership.PrincipalEntityType.GetTableName())
            {
                foreach (var property in entityType.GetProperties()
                    .Except(entityType.FindPrimaryKey()!.Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    RewriteColumnName(property.Builder);
                }

                if (entityType.FindPrimaryKey() is { } key)
                {
                    key.Builder.HasName(RewriteName(key.GetDefaultName()!));
                }
            }
        }

        public void ProcessForeignKeyAdded(
            IConventionForeignKeyBuilder relationshipBuilder,
            IConventionContext<IConventionForeignKeyBuilder> context)
        {
            relationshipBuilder.HasConstraintName(RewriteName(relationshipBuilder.Metadata.GetDefaultName()!));
        }

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
        {
            var entityType = keyBuilder.Metadata.DeclaringEntityType;

            if (entityType.ClrType == typeof(Microsoft.EntityFrameworkCore.Migrations.HistoryRow))
                return;

            if (entityType.FindOwnership() is null)
            {
                keyBuilder.HasName(RewriteName(keyBuilder.Metadata.GetDefaultName()!));
            }
        }

        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                if (entityType.ClrType == typeof(Microsoft.EntityFrameworkCore.Migrations.HistoryRow))
                    continue;

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    if (columnName.StartsWith(entityType.ShortName() + '_', StringComparison.Ordinal))
                    {
                        property.Builder.HasColumnName(
                            RewriteName(entityType.ShortName()) + columnName[entityType.ShortName().Length..]);
                    }

                    foreach (var storeObjectType in _storeObjectTypes)
                    {
                        var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
                        if (identifier is null)
                            continue;

                        if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                        {
                            columnName = property.GetColumnName(identifier.Value)!;
                            if (columnName.StartsWith(entityType.ShortName() + '_', StringComparison.Ordinal))
                            {
                                property.Builder.HasColumnName(
                                    RewriteName(entityType.ShortName())
                                    + columnName[entityType.ShortName().Length..]);
                            }
                        }
                    }
                }
            }
        }

        private static void RewriteColumnName(IConventionPropertyBuilder propertyBuilder)
        {
            var property = propertyBuilder.Metadata;
            var entityType = (IConventionEntityType)property.DeclaringType;

            if (entityType.ClrType == typeof(Microsoft.EntityFrameworkCore.Migrations.HistoryRow))
                return;

            property.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);

            var baseColumnName = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is { } tableIdentifier
                ? property.GetDefaultColumnName(tableIdentifier)
                : property.GetDefaultColumnName();

            if (baseColumnName == "Id")
                baseColumnName = entityType.GetTableName() + baseColumnName;
            propertyBuilder.HasColumnName(RewriteName(baseColumnName!));

            foreach (var storeObjectType in _storeObjectTypes)
            {
                var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
                if (identifier is null)
                    continue;

                if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                {
                    var name = property.GetColumnName(identifier.Value);
                    if (name == "Id")
                        name = entityType.GetTableName() + name;
                    propertyBuilder.HasColumnName(
                        RewriteName(name!), identifier.Value);
                }
            }
        }

        [GeneratedRegex("[A-Z]+", RegexOptions.Compiled)]
        private static partial Regex UpperCaseLocator();
    }
}
