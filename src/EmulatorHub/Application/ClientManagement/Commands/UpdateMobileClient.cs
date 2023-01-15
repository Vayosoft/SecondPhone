
using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.Application.ClientManagement.Commands
{
    public record UpdateMobileClient
    {
        [MaxLength(50)]
        public string Name { get; init; }
    }
}
