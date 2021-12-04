using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DBMSWebApp.Models
{
    public class Row
    {
        public Row()
        {
            Cells = new List<Cell>();
        }
        public int Id { get; set; }
        public int TableId { get; set; }
        public virtual Table Table { get; set; }

        public List<Cell> Cells { get; set; }
    }
}
