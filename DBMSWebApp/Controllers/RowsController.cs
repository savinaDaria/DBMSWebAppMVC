using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DBMSWebApp.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DBMSWebApp.Controllers
{
    public class RowsController : Controller
    {
        private readonly DatabaseContext _context;

        public RowsController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? tableId)
        {
            var databaseContext = _context.Rows.Where(t=>t.TableId == tableId)
                .Include(t=>t.Cells);
            ViewBag.Table = _context.Tables.Where(t => t.Id == tableId).Include(t=>t.Columns).First();
            return View(await databaseContext.ToListAsync());
        }

        public IActionResult Create(int? tableId)
        {
            var table = _context.Tables.Where(t => t.Id == tableId).Include(t => t.Columns).First();
            ViewBag.Table = table;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Num,TableId, Cells")] Row row)
        {
            if (ModelState.IsValid)
            {
                for(int i =0; i< row.Cells.Count; i++)
                {
                    if (!CellValid(row.Cells[i].Value, row.Cells[i].ColumnID)) 
                    {
                        var col = _context.Columns.Find(row.Cells[i].ColumnID);
                        ModelState.AddModelError("Cells",string.Format("Column {0} has {1} type", col.Name, col.TypeFullName));
                        var table = _context.Tables.Where(t => t.Id == row.TableId).Include(t => t.Columns).First();
                        ViewBag.Table = table;
                    }
                }
                if(!ModelState.IsValid) return View(row);
                foreach (var cell in row.Cells)
                {
                     cell.RowId = row.Id;
                    _context.Add(cell);
                }
                _context.Add(row);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Rows", new { tableId = row.TableId });
            }
            return View(row);
        }

        public async Task<IActionResult> Edit(int? id, int? tableId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var row = await _context.Rows.Include(t => t.Cells).FirstOrDefaultAsync(m => m.Id == id);
 
            if (row == null)
            {
                return NotFound();
            }

            var table = _context.Tables.Where(t => t.Id == tableId).Include(t => t.Columns).First();
            ViewBag.Table = table;

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Num,TableId,Cells")] Row row)
        {
            if (id != row.Id)
            {
                return NotFound();
            }
    
            if (ModelState.IsValid)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    if (!CellValid(row.Cells[i].Value, row.Cells[i].ColumnID))
                    {
                        var col = _context.Columns.Find(row.Cells[i].ColumnID);
                        ModelState.AddModelError("Cells", string.Format("Column {0} has {1} type", col.Name, col.TypeFullName));
                        var table = _context.Tables.Where(t => t.Id == row.TableId).Include(t => t.Columns).First();
                        ViewBag.Table = table;
                    }
                }
                if (!ModelState.IsValid) return View(row);
                foreach (var cell in row.Cells)
                {
                    cell.RowId = row.Id;
                    _context.Update(cell);
                }
                try
                {
                    _context.Update(row);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RowExists(row.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Rows", new { tableId = row.TableId });
            }
            return View(row);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var row = await _context.Rows
                .Include(r => r.Table).Include(t=>t.Cells)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (row == null)
            {
                return NotFound();
            }
            return View(row);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cells = _context.Cells.Where(t => t.RowId == id);
            _context.RemoveRange(cells);
            var row = await _context.Rows.FindAsync(id);
            _context.Rows.Remove(row);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Rows", new { tableId = row.TableId });
        }

        private bool RowExists(int id)
        {
            return _context.Rows.Any(e => e.Id == id);
        }
        public bool CellValid(string Value, int ColumnID)
        {
            var col = _context.Columns.Find(ColumnID);
            if (col == null) return false;
            if (col.TypeFullName.Contains("EmailAddress"))
            {
                try
                {
                    MailAddress m = new MailAddress(Value);
                    return true;
                }
                catch (FormatException)
                {
                    return false;
                }
            }
            if (col.TypeFullName.Contains("Enum"))
            {
                Regex regex = new Regex(@"\s*(\s*\w* *(= *\d*)?\s*,?)*?\s*");
                MatchCollection matches = regex.Matches(Value);
                if (matches.Count > 0)
                {
                    return true;
                }
                return false;
            }
            else if (CheckCast(Value, col.TypeFullName))
                return true;

            return false;
        }
        private bool CheckCast(string value, string type)
        {
            if (value == null) return true;
            try
            {
                var resultVal = Convert.ChangeType(value, Type.GetType(type));
                if (!resultVal.ToString().Equals(value.ToString()))
                    throw new InvalidCastException();
                return true;
            }
            catch
            {
                return false;
            }
        }
      

    }
}
