namespace Flickoo.Api.DTOs.User.Get
{
    public class GetUserResponse
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool Registered { get; set; } = false;
    }
}
