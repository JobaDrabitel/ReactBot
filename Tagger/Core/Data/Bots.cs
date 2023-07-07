namespace Tagger.Core.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Bots")]
    public partial class Bots
    {
        public int id { get; set; }

        [Required]
        [StringLength(255)]
        public string phone { get; set; }

        [Required]
        [StringLength(255)]
        public string password { get; set; }

        public int? proxy_id { get; set; }

        [StringLength(32)]
        public string api_hash { get; set; }

        public int? api_id { get; set; }

        public virtual Proxies Proxies { get; set; }

        public virtual Proxies Proxies1 { get; set; }

        public virtual Proxies Proxies2 { get; set; }
    }
}
