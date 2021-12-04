using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DBMSWebApp.Models
{
    public class Column
    {
        public Column()
        {
            Cells = new List<Cell>();
        }
        public int Id { get; set; }
        [Required(ErrorMessage = "Name should not be empty")]
        public string Name { get; set; }

        [Display (Name = "Type")]
        [Remote(action: "TypeValid", controller: "Columns")]
        [Required(ErrorMessage = "Type should not be empty")]

        public string TypeFullName { get; set; }
        public int TableId { get; set; }
        public virtual Table Table { get; set; }
        public List<Cell> Cells { get; set; }

    }
}
