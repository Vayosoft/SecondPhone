using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.API.Model
{
    public class OneTimePasswordRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
    }
}
