namespace Flickoo.Api.Entities
{
    public class Like
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ProductId { get; set; }

        public required User User { get; set; }
        public required Product Product { get; set; }
    }
}
