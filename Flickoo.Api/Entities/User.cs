namespace Flickoo.Api.Entities
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public List<Product> Products { get; set; } = [];

        public List<Like> Likes { get; set; } = [];
    }
}
