using System.Text.Json.Serialization;

namespace MonsterEnergyCodeRedeemer.Models
{
    public class UserModel
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }
}
