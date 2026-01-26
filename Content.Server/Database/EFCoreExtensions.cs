using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

internal static class EFCoreExtensions
{
    extension<TEntity>(IQueryable<TEntity> query) where TEntity : class
    {
        public IQueryable<TEntity> ApplyIncludes(
            IEnumerable<Expression<Func<TEntity, object>>> properties)
        {
            var q = query;
            foreach (var property in properties)
            {
                q = q.Include(property);
            }

            return q;
        }

        public IQueryable<TEntity> ApplyIncludes<TDerived>(
            IEnumerable<Expression<Func<TDerived, object>>> properties,
            Expression<Func<TEntity, TDerived>> getDerived)
            where TDerived : class
        {
            var q = query;
            foreach (var property in properties)
            {
                q = q.Include(getDerived).ThenInclude(property);
            }

            return q;
        }
    }
}
