namespace Flickoo.Api.DTOs
{
    public class CreateUserRequest
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
    }
}
