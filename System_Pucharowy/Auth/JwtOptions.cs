namespace System_Pucharowy.Auth
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = "TournamentGraphQL";
        public string Audience { get; set; } = "TournamentGraphQL";
        public string Key { get; set; } = "SUPER_SECRET_KEY_CHANGE_ME_123456789";
    }
}
