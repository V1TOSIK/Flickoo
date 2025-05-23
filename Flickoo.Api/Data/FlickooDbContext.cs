﻿using Flickoo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flickoo.Api.Data
{
    public class FlickooDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public FlickooDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
                optionsBuilder
                    .UseNpgsql(_configuration.GetConnectionString("Postgres"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Favourite> Favourites { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Media> Medias { get; set; }
        public DbSet<Location> Locations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlickooDbContext).Assembly);
        }
    }
}
