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
        public DbSet<DeclutterHub.Models.CategoryClick> CategoryClick { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call base class method

            // Configure the foreign key explicitly
            modelBuilder.Entity<Item>()
                .HasOne(i => i.User)  // Item has one User (ApplicationUser)
                .WithMany()  // Assuming ApplicationUser has many items (no navigation property needed in ApplicationUser)
                .HasForeignKey(i => i.UserId)  // Foreign key is UserId in Item
                .IsRequired(); // Make it required if necessary

            modelBuilder.Entity<SavedItem>()
            .HasKey(s => s.Id);

            modelBuilder.Entity<SavedItem>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<CategoryClick>()
            .HasOne(cc => cc.User)
            .WithMany()
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete

            modelBuilder.Entity<CategoryClick>()
                .HasOne(cc => cc.Category)
                .WithMany()
                .HasForeignKey(cc => cc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete

            // Optional: Create an index for faster queries
            modelBuilder.Entity<CategoryClick>()
                .HasIndex(cc => new { cc.UserId, cc.CategoryId, cc.ClickedAt });
        }
    }
}
