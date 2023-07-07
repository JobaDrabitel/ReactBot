using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tagger.Core.Data
{
    internal class Configuration : DbConfiguration
    {
        public Configuration()
        {
            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);

            var providerServices = (System.Data.Entity.Core.Common.DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(System.Data.Entity.Core.Common.DbProviderServices));

            SetProviderServices("System.Data.SQLite", providerServices);
            SetProviderServices("System.Data.SQLite.EF6", providerServices);
        }

        public DbConnection CreateConnection(string connectionString)
            => new SQLiteConnection(connectionString);
    }
}
