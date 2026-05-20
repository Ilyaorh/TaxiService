using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("DriverRatings")]
    public class DriverRating
    {
        [Key]
        public int DriverRatingID { get; set; }

        public int DriverID { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; }

        public int ReviewCount { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("DriverID")]
        public virtual Driver Driver { get; set; } = null!;
    }
}