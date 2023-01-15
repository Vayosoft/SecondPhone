
using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.Application.ClientContext.Commands
{
    public record UpdateMobileClient
    {
        [MaxLength(50)]
        public string Name { get; init; }
    }
}
