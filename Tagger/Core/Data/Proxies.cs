namespace Tagger.Core.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Proxies")]
    public partial class Proxies
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Proxies()
        {
            Bots = new HashSet<Bots>();
            Bots1 = new HashSet<Bots>();
            Bots2 = new HashSet<Bots>();
        }

        public int id { get; set; }

        [Required]
        [StringLength(255)]
        public string ip { get; set; }

        public int port { get; set; }

        [StringLength(255)]
        public string login { get; set; }

        [StringLength(255)]
        public string password { get; set; }

        [StringLength(255)]
        public string type { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bots> Bots { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bots> Bots1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bots> Bots2 { get; set; }
    }
}
