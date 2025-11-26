using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GadgetsOnline.Models
{
    [Table("categories", Schema = "dps_dbo")]
    public class Category
    {
        [Column("categoryid")]
        public int CategoryId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        public List<Product> Products { get; set; }
    }
}