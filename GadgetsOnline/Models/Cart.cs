using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GadgetsOnline.Models
{
    [Table("carts", Schema = "dps_dbo")]
    public class Cart
    {
        [Key]
        [Column("recordid")]
        public int RecordId { get; set; }
        [Column("cartid")]
        public string CartId { get; set; }
        [Column("productid")]
        public int ProductId { get; set; }
        [Column("count")]
        public int Count { get; set; }
        [Column("datecreated")]
        public DateTime DateCreated { get; set; }
        public virtual Product Product { get; set; }

    }
}