﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DeclutterHub.Models;

namespace DeclutterHub.Data
{
    public class DeclutterHubContext : DbContext
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
    }
}