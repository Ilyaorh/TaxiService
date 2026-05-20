using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("PromoCodeUsages")]
    public class PromoCodeUsage
    {
        [Key]
        public int UsageID { get; set; }

        public int UserID { get; set; }

        public int PromoCodeID { get; set; }

        public int TripID { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PromoCodeID")]
        public virtual PromoCode PromoCode { get; set; } = null!;

        [ForeignKey("TripID")]
        public virtual Trip Trip { get; set; } = null!;
    }
}