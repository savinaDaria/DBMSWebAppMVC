using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DBMSWebApp.Models;

namespace DBMSWebApp.Controllers
{
    public class CellsController : Controller
    {
        private readonly DatabaseContext _context;

        public CellsController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var databaseContext = _context.Cells.Include(c => c.Column).Include(c => c.Row);
            return View(await databaseContext.ToListAsync());
        }

    }
}
