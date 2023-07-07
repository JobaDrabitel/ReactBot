using System;
using SQLite.CodeFirst;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Tagger.Core.Data
{
    internal class MyDbContextInitializer : SqliteDropCreateDatabaseAlways<DataModel>
    {
        public MyDbContextInitializer(DbModelBuilder modelBuilder) : base(modelBuilder)
        {
        }

        protected override void Seed(DataModel context)
        {
            base.Seed(context);
        }
    }
}
