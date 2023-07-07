using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace Tagger.Core.Data
{
    [DbConfigurationType(typeof(Configuration))]
    public partial class DataModel : DbContext
    {
        public DataModel() : base("name=DataModel")
        {
            Database.SetInitializer<DataModel>(null);
        }


        public virtual DbSet<Bots> Bots { get; set; }
        public virtual DbSet<Proxies> Proxies { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bots>()
                .Property(e => e.phone)
                .IsUnicode(false);

            modelBuilder.Entity<Bots>()
                .Property(e => e.password)
                .IsUnicode(false);

            modelBuilder.Entity<Bots>()
                .Property(e => e.api_hash)
                .IsUnicode(false);

            modelBuilder.Entity<Proxies>()
                .Property(e => e.ip)
                .IsUnicode(false);

            modelBuilder.Entity<Proxies>()
                .Property(e => e.login)
                .IsUnicode(false);

            modelBuilder.Entity<Proxies>()
                .Property(e => e.password)
                .IsUnicode(false);

            modelBuilder.Entity<Proxies>()
                .Property(e => e.type)
                .IsUnicode(false);

            modelBuilder.Entity<Proxies>()
                .HasMany(e => e.Bots)
                .WithOptional(e => e.Proxies)
                .HasForeignKey(e => e.proxy_id);

            modelBuilder.Entity<Proxies>()
                .HasMany(e => e.Bots1)
                .WithOptional(e => e.Proxies1)
                .HasForeignKey(e => e.proxy_id);

            modelBuilder.Entity<Proxies>()
                .HasMany(e => e.Bots2)
                .WithOptional(e => e.Proxies2)
                .HasForeignKey(e => e.proxy_id);
        }
    }
}
