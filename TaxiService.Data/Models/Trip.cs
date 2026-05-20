using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("Trips")]
    public class Trip
    {
        [Key]
        public int TripID { get; set; }

        public int UserID { get; set; }

        public int? DriverID { get; set; }

        public int TariffID { get; set; }

        [Required]
        [StringLength(255)]
        public string StartAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string EndAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DistanceKm { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ServiceCommission { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Created";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? OriginalCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountAmount { get; set; }

        public int? PromoCodeID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("DriverID")]
        public virtual Driver? Driver { get; set; }

        [ForeignKey("TariffID")]
        public virtual Tariff Tariff { get; set; } = null!;

        [ForeignKey("PromoCodeID")]
        public virtual PromoCode? PromoCode { get; set; }

        public virtual Payment? Payment { get; set; }

        public virtual Review? Review { get; set; }
    }
}