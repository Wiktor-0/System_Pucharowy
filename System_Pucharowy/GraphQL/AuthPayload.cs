using System_Pucharowy.Models;

namespace System_Pucharowy.GraphQL
{
    public class AuthPayload
    {
        public string Token { get; set; } = "";
        public User User { get; set; } = null!;
    }
}
