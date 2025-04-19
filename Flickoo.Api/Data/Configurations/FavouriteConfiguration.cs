using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class FavouriteConfiguration : IEntityTypeConfiguration<Favourite>
    {
        public void Configure(EntityTypeBuilder<Favourite> builder)
        {
            builder.ToTable("Favourites")
                .HasKey(l => l.Id);
            
            builder.Property(l => l.Id)
                .HasColumnName("FavouriteId");
            
            builder.Property(l => l.CreatedAt)
                .IsRequired();
            
            builder.HasOne(l => l.Product)
                .WithMany(p => p.Favourites)
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(l => l.User)
                .WithMany(u => u.Favourites)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
               .HasIndex(f => new { f.UserId, f.ProductId })
               .IsUnique();
        }
    }
}