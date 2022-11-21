using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Vayosoft.Commons.Models;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;

namespace EmulatorHub.Infrastructure.Persistence.Filters
{
    public enum QueryFilterTypes { SoftDelete, ProviderId }
    public static class QueryFilter
    {
        public static void AddQueryFilter(this IMutableEntityType entityData, QueryFilterTypes queryFilterType, IUserContext? userContext = null)
        {
            var methodName = $"Get{queryFilterType}Filter";

            var methodToCall = typeof(QueryFilter)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityData.ClrType);

            var filter = methodToCall.Invoke(null, new object[] { userContext });

            entityData.SetQueryFilter((LambdaExpression)filter!);

            switch (queryFilterType)
            {
                case QueryFilterTypes.SoftDelete:
                    entityData.AddIndex(entityData.FindProperty(nameof(ISoftDelete.SoftDeleted))!);
                    break;
                case QueryFilterTypes.ProviderId:
                    entityData.AddIndex(entityData.FindProperty((nameof(IProviderId.ProviderId)))!);
                    break;
            }
        }

        private static LambdaExpression GetProviderIdFilter<TEntity>(IUserContext userContext)
            where TEntity : class, IProviderId
        {
            Expression<Func<TEntity, bool>> filter = x => (long)x.ProviderId == userContext.User.Identity.GetProviderId();
            return filter;
        }

        private static LambdaExpression GetSoftDeleteFilter<TEntity>(IUserContext? userContext = null)
            where TEntity : class, ISoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
            return filter;
        }

    }
}
