using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        public int TripID { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("TripID")]
        public virtual Trip Trip { get; set; } = null!;
    }
}