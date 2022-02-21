namespace Loki.Simple.Entity
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("LokiLockings")]
    public class LokiLockingEntity
    {
        [Key]
        public string ServiceKey { get; set; }

        public DateTime CreationDate { get; set; }
    }
}