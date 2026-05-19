using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiService.Data.Models
{
    [Table("Cars")]
    public class Car
    {
        [Key]
        public int CarID { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Color { get; set; }

        public int YearManufactured { get; set; }

        public virtual ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    }
}