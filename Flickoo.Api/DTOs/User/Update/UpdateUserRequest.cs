namespace Flickoo.Api.DTOs.User.Update
{
    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? LocationName { get; set; }
        public string? Nickname { get; set; }
        public bool Registered { get; set; }
    }
}
