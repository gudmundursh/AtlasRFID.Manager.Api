namespace AtlasRFID.Manager.Api.Dtos
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresInSeconds { get; set; }
    }
}
