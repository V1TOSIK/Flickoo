namespace Flickoo.Api.DTOs.User.Create
{
    public class CreateUserResponse
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool IsRegistered { get; set; } = false;
    }
}
