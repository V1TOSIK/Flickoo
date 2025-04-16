namespace Flickoo.Telegram.DTOs.User
{
    public class UpdateUserRequest
    {
        public long Id { get; set; }
        public string NickName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool Registered { get; set; }
    }
}
