namespace Tagger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bots",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        phone = c.String(nullable: false, maxLength: 255, unicode: false),
                        password = c.String(nullable: false, maxLength: 255, unicode: false),
                        proxy_id = c.Int(),
                        api_hash = c.String(maxLength: 32, unicode: false),
                        api_id = c.Int(),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.Proxies", t => t.proxy_id)
                .Index(t => t.proxy_id);
            
            CreateTable(
                "dbo.Proxies",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        ip = c.String(nullable: false, maxLength: 255, unicode: false),
                        port = c.Int(nullable: false),
                        login = c.String(maxLength: 255, unicode: false),
                        password = c.String(maxLength: 255, unicode: false),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Bots", "proxy_id", "dbo.Proxies");
            DropIndex("dbo.Bots", new[] { "proxy_id" });
            DropTable("dbo.Proxies");
            DropTable("dbo.Bots");
        }
    }
}
