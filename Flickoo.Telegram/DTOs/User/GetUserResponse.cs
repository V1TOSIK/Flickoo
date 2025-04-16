namespace Flickoo.Telegram.DTOs.User
{
    public class GetUserResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool Registered { get; set; }
    }
}
