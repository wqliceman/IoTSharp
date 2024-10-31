using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Dtos
{
    public class RefreshTokenDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}