using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

#pragma warning disable EF1001
namespace Content.Server.Database
{
    // Taken from https://github.com/npgsql/efcore.pg/issues/1158
    // To support inet -> (IPAddress, int) mapping.
    public class CustomNpgsqlTypeMappingSource : NpgsqlTypeMappingSource
    {
        public CustomNpgsqlTypeMappingSource(
            TypeMappingSourceDependencies dependencies,
            RelationalTypeMappingSourceDependencies relationalDependencies,
            ISqlGenerationHelper sqlGenerationHelper,
            INpgsqlSingletonOptions npgsqlOptions)
            : base(dependencies, relationalDependencies, sqlGenerationHelper, npgsqlOptions)
        {
            StoreTypeMappings["inet"] =
                new RelationalTypeMapping[]
                {
                    new NpgsqlInetWithMaskTypeMapping(),
                    new NpgsqlInetTypeMapping()
                };
        }
    }

    // Basically copied from NpgsqlCidrTypeMapping
    public class NpgsqlInetWithMaskTypeMapping : NpgsqlTypeMapping
    {
        public NpgsqlInetWithMaskTypeMapping() : base("inet", typeof((IPAddress, int)), NpgsqlTypes.NpgsqlDbType.Inet)
        {
        }

        protected NpgsqlInetWithMaskTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters, NpgsqlTypes.NpgsqlDbType.Inet)
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        {
            return new NpgsqlInetWithMaskTypeMapping(parameters);
        }

        protected override string GenerateNonNullSqlLiteral(object value)
        {
            var (address, subnet) = ((IPAddress, int)) value;
            return $"INET '{address}/{subnet}'";
        }

        public override Expression GenerateCodeLiteral(object value)
        {
            var (address, subnet) = ((IPAddress, int)) value;
            return Expression.New(
                Constructor,
                Expression.Call(ParseMethod, Expression.Constant(address.ToString())),
                Expression.Constant(subnet));
        }

        private static readonly MethodInfo ParseMethod = typeof(IPAddress).GetMethod("Parse", new[] {typeof(string)})!;
        private static readonly ConstructorInfo Constructor =
            typeof((IPAddress, int)).GetConstructor(new[] {typeof(IPAddress), typeof(int)})!;
    }
}
