using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DBMSWebApp.Models
{
    public class Cell
    {
        public Cell()
        {
        }
        public int Id { get; set; }
        public string Value { get; set; }
        public int RowId { get; set; }
        public int ColumnID { get; set; }
        public virtual Row Row { get; set; }
        public virtual Column Column { get; set; }
    }
}
