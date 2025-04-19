using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.OwnsOne(p => p.Price, price =>
            {
                price.Property(p => p.Amount)
                .HasColumnName("PriceAmount")
                .IsRequired();

                price.Property(p => p.Currency)
                .HasColumnName("PriceCurrency")
                .IsRequired()
                .HasMaxLength(3);
            });
            
            builder.ToTable("Products")
                .HasKey(p => p.Id);
            
            builder.Property(p => p.Id)
                .HasColumnName("ProductId");
            
            builder.Property(p => p.Name)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(500);
            
            builder.Property(p => p.CreatedAt)
                .IsRequired();
            
            builder.HasOne(p => p.Location)
                .WithMany(l => l.Products)
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(p => p.User)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(p => p.ProductMedias)
                .WithOne(pm => pm.Product)
                .HasForeignKey(pm => pm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(p => p.Favourites)
                .WithOne(l => l.Product)
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}