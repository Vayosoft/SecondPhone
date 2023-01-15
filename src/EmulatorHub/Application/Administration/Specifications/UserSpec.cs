using Vayosoft.Commons.Models;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Identity;
using Vayosoft.Identity.Security.Models;
using Vayosoft.Persistence.Specifications;

namespace EmulatorHub.Application.Administration.Specifications
{
    public class UserSpec : PagingModelBase<UserEntityDto, string>, ILinqSpecification<UserEntity>
    {
        private readonly long? _providerId;
        private readonly string _searchTerm;

        public UserSpec(int page, int size, long? providerId = null, string searchTerm = null)
        {
            Page = page; Size = size;
            _providerId = providerId;
            _searchTerm = searchTerm;
        }

        public IQueryable<UserEntity> Apply(IQueryable<UserEntity> query)
        {
            if (!string.IsNullOrEmpty(_searchTerm))
                query = query
                    .Where(u => u.Username.Contains(_searchTerm));

            if (_providerId != null)
                query = query.Where(u => u.ProviderId == _providerId);

            return query.Where(u => u.Email != null);
        }

        protected override Sorting<UserEntityDto, string> BuildDefaultSorting()
        {
            return new Sorting<UserEntityDto, string>(x => x.Username);
        }
    }
}
