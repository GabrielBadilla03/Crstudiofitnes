using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PaginaSolicitudDescuentos.Data;
using PaginaSolicitudDescuentos.Models;

namespace PaginaSolicitudDescuentos.Controllers
{
    public class XXORA_ITEM_MASTERController : Controller
    {
        private readonly OracleContext _context;

        public XXORA_ITEM_MASTERController(OracleContext context)
        {
            _context = context;
        }

        // GET: XXORA_ITEM_MASTER
        public async Task<IActionResult> Index()
        {
            return View(await _context.XXORA_ITEM_MASTERs
                .AsNoTracking()
                .OrderBy(x => x.ITEM_NUMBER)  // o la columna que tenga sentido para vos
                .Take(100)
                .ToListAsync());
        }

        // GET: XXORA_ITEM_MASTER/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_ITEM_MASTER = await _context.XXORA_ITEM_MASTERs
                .FirstOrDefaultAsync(m => m.ITEM_NUMBER == id);
            if (xXORA_ITEM_MASTER == null)
            {
                return NotFound();
            }

            return View(xXORA_ITEM_MASTER);
        }

        // GET: XXORA_ITEM_MASTER/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: XXORA_ITEM_MASTER/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BU_NAME,ORGANIZATION_CODE,ITEM_NUMBER,DESCRIPTION,LONG_DESCRIPTION,CASE_PACK_QUANTITY,PRIMARY_UOM_CODE,TAX_CLASSIFICATION_CODE,TAX_RATE,CATEGORY_CODE,CATEGORY_NAME,SUBCATEGORY_CODE,SUBCATEGORY_NAME,ORIGIN_COUNTRY,STATUS,CREATION_DATE,CREATED_BY,LAST_UPDATE_DATE,LAST_UPDATED_BY,UNIT_WEIGHT,WEIGHT_UOM_CODE,SECONDARY_UOM_CODE")] XXORA_ITEM_MASTER xXORA_ITEM_MASTER)
        {
            if (ModelState.IsValid)
            {
                _context.Add(xXORA_ITEM_MASTER);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(xXORA_ITEM_MASTER);
        }

        // GET: XXORA_ITEM_MASTER/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_ITEM_MASTER = await _context.XXORA_ITEM_MASTERs.FindAsync(id);
            if (xXORA_ITEM_MASTER == null)
            {
                return NotFound();
            }
            return View(xXORA_ITEM_MASTER);
        }

        // POST: XXORA_ITEM_MASTER/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("BU_NAME,ORGANIZATION_CODE,ITEM_NUMBER,DESCRIPTION,LONG_DESCRIPTION,CASE_PACK_QUANTITY,PRIMARY_UOM_CODE,TAX_CLASSIFICATION_CODE,TAX_RATE,CATEGORY_CODE,CATEGORY_NAME,SUBCATEGORY_CODE,SUBCATEGORY_NAME,ORIGIN_COUNTRY,STATUS,CREATION_DATE,CREATED_BY,LAST_UPDATE_DATE,LAST_UPDATED_BY,UNIT_WEIGHT,WEIGHT_UOM_CODE,SECONDARY_UOM_CODE")] XXORA_ITEM_MASTER xXORA_ITEM_MASTER)
        {
            if (id != xXORA_ITEM_MASTER.ITEM_NUMBER)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(xXORA_ITEM_MASTER);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!XXORA_ITEM_MASTERExists(xXORA_ITEM_MASTER.ITEM_NUMBER))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(xXORA_ITEM_MASTER);
        }

        // GET: XXORA_ITEM_MASTER/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_ITEM_MASTER = await _context.XXORA_ITEM_MASTERs
                .FirstOrDefaultAsync(m => m.ITEM_NUMBER == id);
            if (xXORA_ITEM_MASTER == null)
            {
                return NotFound();
            }

            return View(xXORA_ITEM_MASTER);
        }

        // POST: XXORA_ITEM_MASTER/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var xXORA_ITEM_MASTER = await _context.XXORA_ITEM_MASTERs.FindAsync(id);
            if (xXORA_ITEM_MASTER != null)
            {
                _context.XXORA_ITEM_MASTERs.Remove(xXORA_ITEM_MASTER);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool XXORA_ITEM_MASTERExists(string id)
        {
            return _context.XXORA_ITEM_MASTERs.Any(e => e.ITEM_NUMBER == id);
        }
    }
}
