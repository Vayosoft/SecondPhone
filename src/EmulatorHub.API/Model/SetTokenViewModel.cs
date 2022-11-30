using System.ComponentModel.DataAnnotations;

namespace EmulatorHub.API.Model
{
    public class SetTokenViewModel
    {
        [Required]
        [MaxLength(500)]
        public string Token { get; set; }
    }
}
