using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Models;
using Microsoft.AspNetCore.Identity;

namespace DeclutterHub.Data
{
    public class DeclutterHubContext : IdentityDbContext<User>
    {
        public DeclutterHubContext (DbContextOptions<DeclutterHubContext> options)
            : base(options)
        {
        }

        public DbSet<DeclutterHub.Models.Item> Item { get; set; } = default!;
        public DbSet<DeclutterHub.Models.User> User { get; set; } = default!;
        
        public DbSet<DeclutterHub.Models.Category> Category { get; set; } = default!;
        public DbSet<DeclutterHub.Models.Image> Image { get; set; } = default!;
        public DbSet<DeclutterHub.Models.Sale> Sale { get; set; } = default!;

        public DbSet<DeclutterHub.Models.SavedItem> SavedItem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call base class method

            // Configure the foreign key explicitly
            modelBuilder.Entity<Item>()
                .HasOne(i => i.User)  // Item has one User (ApplicationUser)
                .WithMany()  // Assuming ApplicationUser has many items (no navigation property needed in ApplicationUser)
                .HasForeignKey(i => i.UserId)  // Foreign key is UserId in Item
                .IsRequired(); // Make it required if necessary

            // Add other custom configurations as needed...
        }
    }
}
