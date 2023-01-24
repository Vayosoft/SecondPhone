using EmulatorHub.Application.Administration.Models;
using EmulatorHub.Domain.Commons.Entities;
using Vayosoft.Commons.Models;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Persistence.Specifications;

namespace EmulatorHub.Application.Administration.Specifications
{
    public class EmulatorSpec : PagingModelBase<EmulatorDto, string>, ILinqSpecification<Emulator>
    {
        private readonly string _searchTerm;

        public EmulatorSpec(int page, int size, string searchTerm = null)
        {
            Page = page; PageSize = size;
            _searchTerm = searchTerm;
        }

        public IQueryable<Emulator> Apply(IQueryable<Emulator> query)
        {
            if (!string.IsNullOrEmpty(_searchTerm))
                query = query
                    .Where(u => u.Name.Contains(_searchTerm));

            return query;
            //return query.OrderBy(e => e.Name);
        }

        protected override Sorting<EmulatorDto, string> BuildDefaultSorting()
        {
            return new Sorting<EmulatorDto, string>(x => x.Name);
        }
    }
}
