﻿namespace Flickoo.Api.Entities
{
    public class Location
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<User> Users { get; set; } = [];
        public List<Product> Products { get; set; } = [];
    }
}
