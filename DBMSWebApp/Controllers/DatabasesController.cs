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
    public class DatabasesController : Controller
    {
        private readonly DatabaseContext _context;

        public DatabasesController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Databases.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewBag.DatabaseId = id;
            var database = await _context.Databases
                .FirstOrDefaultAsync(m => m.Id == id);
            if (database == null)
            {
                return NotFound();
            }
            return RedirectToAction("Index", "Tables", new { databaseId = database.Id});

        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Database database)
        {
            if (ModelState.IsValid)
            {
                _context.Add(database);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(database);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var database = await _context.Databases
                .FirstOrDefaultAsync(m => m.Id == id);
            if (database == null)
            {
                return NotFound();
            }

            return View(database);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tables = _context.Tables.Where(t => t.DatabaseId == id);
            foreach(var table in tables)
            {
                var columns = _context.Columns.Where(t => t.TableId == table.Id);
                var rows = _context.Rows.Where(t => t.TableId == table.Id);
                foreach (var row in rows)
                {
                    var cells = _context.Cells.Where(t => t.RowId == row.Id);
                    _context.RemoveRange(cells);
                }
                _context.RemoveRange(rows);
                _context.RemoveRange(columns);
            }
            _context.RemoveRange(tables);
            var database = await _context.Databases.FindAsync(id);
            _context.Databases.Remove(database);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DatabaseExists(int id)
        {
            return _context.Databases.Any(e => e.Id == id);
        }
    }
}
