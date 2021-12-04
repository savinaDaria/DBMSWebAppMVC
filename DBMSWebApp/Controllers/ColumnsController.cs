using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DBMSWebApp.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
namespace DBMSWebApp.Controllers
{
    public class ColumnsController : Controller
    {
        private readonly DatabaseContext _context;

        public ColumnsController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var databaseContext = _context.Columns.Include(c => c.Table);
            return View(await databaseContext.ToListAsync());
        }

        public IActionResult Create(int? tableId)
        {
            ViewBag.TableId = tableId;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,TypeFullName,TableId")] Column column)
        {
            if (ModelState.IsValid)
            {
                var rows = _context.Rows.Where(t => t.TableId == column.TableId);
                foreach(var row in rows)
                {
                    Cell cell = new Cell()
                    {
                        ColumnID = column.Id,
                        RowId = row.Id,
                        Value = null
                    };
                    _context.Add(cell);
                    column.Cells.Add(cell);
                }
                _context.Add(column);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Rows", new { tableId = column.TableId});
            }
            return View(column);
        }
      
        public async Task<IActionResult> Delete(int? tableId)
        {
            if (tableId == null)
            {
                return NotFound();
            }

            var column = await _context.Columns
                .Include(c => c.Table).Where(m => m.TableId == tableId).ToListAsync();
            if (column == null)
            {
                return NotFound();
            }
            ViewBag.TableId = tableId;
            ViewData["Id"] = new SelectList(column, "Id", "Name");
            return View();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind("Id, TableId")]Column column)
        {
            var col = await _context.Columns.FindAsync(column.Id);
            var cells = _context.Cells.Where(t => t.ColumnID == column.Id);
            _context.Cells.RemoveRange(cells);
            _context.Columns.Remove(col);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Rows", new { tableId = column.TableId });
        }

        private bool ColumnExists(int id)
        {
            return _context.Columns.Any(e => e.Id == id);
        }
        public IActionResult TypeValid(string? TypeFullName)
        {
            if (TypeFullName.Contains("Enum") || TypeFullName.Contains("EmailAddress"))
            {
                return Json(true);
            }

            else if (!TypeFullName.Contains("EmailAddress") && !TypeFullName.Contains("Enum"))
            {
                var type = Type.GetType(TypeFullName);
                if (type == null)
                return Json (data: "Invalid type");
                else return Json(true);

            }
            else
            {
                return Json(data: "Invalid type");
            }
            
        }
    }
}
