using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.API.Model
{
    public class OneTimePasswordLoginRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
