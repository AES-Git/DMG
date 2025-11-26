using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace GadgetsOnline.Models
{
    [Table("products", Schema = "dps_dbo")]
    public class Product
    {
        [ScaffoldColumn(false)]
        [Column("productid")]
        public int ProductId { get; set; }

        [DisplayName("Category")]
        [Column("categoryid")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10000.00")]
        [Column("price")]
        public decimal Price { get; set; }

        [DisplayName("Product Art URL")]
        [StringLength(1024)]
        [Column("productarturl")]
        public string ProductArtUrl { get; set; }

        public virtual Category Category { get; set; }

        public virtual List<OrderDetail> OrderDetails { get; set; }
    }
}