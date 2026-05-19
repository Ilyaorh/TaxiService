using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("Tariffs")]
    public class Tariff
    {
        [Key]
        public int TariffID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerKm { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionPercent { get; set; }

        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}