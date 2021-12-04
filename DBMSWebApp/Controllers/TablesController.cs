using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DBMSWebApp.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;

namespace DBMSWebApp.Controllers
{
    public class TablesController : Controller
    {
        private readonly DatabaseContext _context;

        public TablesController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? databaseId)
        {
            ViewBag.DatabaseId = databaseId;
            var databaseContext = _context.Tables.Where(t=> t.DatabaseId == databaseId).Include(t=>t.Database);
            return View(await databaseContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var table = await _context.Tables
                .Include(t => t.Database)
                .Include(t=>t.Columns)
                .Include(t=>t.Rows)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (table == null)
            {
                return NotFound();
            }
            return RedirectToAction("Index", "Rows", new { tableId = table.Id });
        }

        public IActionResult Create(int? databaseId)
        {
            ViewBag.DatabaseId = databaseId;
            return View() ;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DatabaseId")] Table table)
        {
            if (ModelState.IsValid)
            {
                _context.Add(table);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Tables", new { databaseId = table.DatabaseId });
            }
            return View(table);
        }
 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
 
            var table = await _context.Tables
                .Include(t => t.Database)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var columns = _context.Columns.Where(t => t.TableId == id);
            var rows = _context.Rows.Where(t => t.TableId == id);
            foreach (var row in rows)
            {
                var cells = _context.Cells.Where(t => t.RowId == row.Id);
                _context.RemoveRange(cells);
            }
            _context.RemoveRange(rows);
            _context.RemoveRange(columns);
            var table = await _context.Tables.FindAsync(id);
            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Tables", new { databaseId = table.DatabaseId });
        }

        private bool TableExists(int id)
        {
            return _context.Tables.Any(e => e.Id == id);
        }
        public IActionResult Sub(int? databaseId)
        {
            var db = _context.Databases.Include(t=>t.Tables).First(t=>t.Id == databaseId);
            if(db.Tables.Count < 2)
            {
                ModelState.AddModelError("Database", string.Format("Database {0} contains only {1} tables",db.Name, db.Tables.Count));
                return View();
            }
            ViewBag.DatabaseId = databaseId;
            ViewBag.Tables = new SelectList(_context.Tables.Where(t=>t.DatabaseId == databaseId), "Id", "Name");
            var firstTable = _context.Tables.Where(t => t.DatabaseId == databaseId).First();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> SubTables([Bind("FirstTable, SecondTable")] JoinViewModel joinModel)
        {
            var firsttbl = _context.Tables.Include(t => t.Columns).Include(t => t.Rows).FirstOrDefault(t => t.Id == joinModel.FirstTable);
            var secondtbl = _context.Tables.Include(t => t.Columns).Include(t => t.Rows).FirstOrDefault(t => t.Id == joinModel.SecondTable);

            var f_cols = _context.Columns.Where(t => t.TableId == joinModel.FirstTable);
            var s_cols = _context.Columns.Where(t => t.TableId == joinModel.SecondTable);

            var shared_cols = f_cols.Join(s_cols, fc => new { fc.Name, fc.TypeFullName }, sc => new { sc.Name, sc.TypeFullName }, (fc, sc) => new { fc, sc });

            var sh_f_cells = _context.Cells.Where(t => shared_cols.Select(f => f.fc.Id).Contains(t.ColumnID)).AsEnumerable();
            var sh_s_cells = _context.Cells.Where(t => shared_cols.Select(f => f.sc.Id).Contains(t.ColumnID)).AsEnumerable();

            var sh_f_cell_group = from cell in sh_f_cells
                                  group cell by cell.RowId into g
                                  select new
                                  {
                                      RowId = g.Key,
                                      Count = g.Count(),
                                      Cells = (from c in g select c.Value).ToList()
                                  };
            var sh_s_cell_group = from cell in sh_s_cells
                                  group cell by cell.RowId into g
                                  select new
                                  {
                                      RowId = g.Key,
                                      Count = g.Count(),
                                      Cells = (from c in g select c.Value).ToList()
                                  };

            List<int> l = new List<int>();
            foreach (var gr_f in sh_f_cell_group)
            {
                foreach (var gr_s in sh_s_cell_group)
                {
                    if (gr_f.Cells.SequenceEqual(gr_s.Cells))
                    {
                        l.Add(gr_f.RowId);
                        l.Add(gr_s.RowId);
                    }
                }
            }
            var validRows = _context.Cells.Where(c => f_cols.Select(f => f.Id).Contains(c.ColumnID) && !l.Contains(c.RowId));
            var f_rows = _context.Rows.Where(r => r.TableId == joinModel.FirstTable && !l.Contains(r.Id));
            foreach(var f in f_rows)
            {
               
                f.Cells.Clear();
                f.Cells = new List<Cell>();
            }
            var table = new Table
            {
                Columns = f_cols.OrderBy(c=>c.Id).ToList(),
                Rows=f_rows.ToList()
            };
            foreach (var row in table.Rows)
            {
                var list = validRows.Where(c => c.RowId == row.Id).ToList();
                foreach (var cell in list)
                {
                     if(!row.Cells.Contains(cell))
                        row.Cells.Add(cell);
                }
                row.Cells= row.Cells.OrderBy(c => c.ColumnID).ToList();
            }
            table.Name = firsttbl.Name + " SUBTRACT " + secondtbl.Name;
            table.DatabaseId = firsttbl.DatabaseId;
            
            return View(table);
        }

        [HttpGet]
        public async Task<JsonResult> GetColumns(int tableId)
        {
            List<Column> columns = await _context.Columns.Where(x => x.TableId == tableId).ToListAsync();
            return Json(columns);

        }
    }
}
