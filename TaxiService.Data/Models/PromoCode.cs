using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("PromoCodes")]
    public class PromoCode
    {
        [Key]
        public int PromoCodeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = "Percent";

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<PromoCodeUsage> Usages { get; set; } = new List<PromoCodeUsage>();
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}