using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("Drivers")]
    public class Driver
    {
        [Key]
        public int DriverID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public int? CarID { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 5.00m;

        [StringLength(20)]
        public string Status { get; set; } = "Offline";

        public DateTime HireDate { get; set; } = DateTime.Now;

        [ForeignKey("CarID")]
        public virtual Car? Car { get; set; }

        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}