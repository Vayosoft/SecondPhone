using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.Application.Administration.Commands
{
    public record RegisterDevice
    {
        [Required]
        [RegularExpression(pattern:"^[\\d]{5,21}$", ErrorMessage = "invalid phone number")]
        public string PhoneNumber { get; set; }
        public string DeviceId { get; set; }
    }
}
