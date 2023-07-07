using System.Data.Common;
using System;
using System.Data.Entity.Migrations;
using System.Data.SQLite;
using System.Data.SQLite.EF6.Migrations;
using Tagger.Core.Data;

namespace Tagger.Migrations
{
    internal class Configuration : DbMigrationsConfiguration<DataModel>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }

        protected override void Seed(DataModel context)
        {
            base.Seed(context);
        }
    }
}
