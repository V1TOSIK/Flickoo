using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flickoo.Api.Data.Configurations
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.ToTable("Locations")
                .HasKey(l => l.Id);
            
            builder.Property(l => l.Id)
                .HasColumnName("LocationId");
            
            builder.Property(l => l.Name)
                .IsRequired();
            
            builder.HasMany(l => l.Products)
                .WithOne(p => p.Location)
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(l => l.Users)
                .WithOne(u => u.Location)
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
