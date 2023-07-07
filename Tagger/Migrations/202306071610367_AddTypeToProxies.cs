namespace Tagger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTypeToProxies : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proxies", "type", c => c.String(maxLength: 255, unicode: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proxies", "type");
        }
    }
}
