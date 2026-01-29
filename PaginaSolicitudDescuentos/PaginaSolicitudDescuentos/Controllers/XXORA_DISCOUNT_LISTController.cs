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
    public class XXORA_DISCOUNT_LISTController : Controller
    {
        private readonly OracleContext _context;

        public XXORA_DISCOUNT_LISTController(OracleContext context)
        {
            _context = context;
        }

        // GET: XXORA_DISCOUNT_LIST
        public async Task<IActionResult> Index()
        {
            return View(await _context.XXORA_DISCOUNT_LISTs
                .AsNoTracking()
                .OrderBy(x => x.ITEM_NUMBER)  // o la columna que tenga sentido para vos
                .Take(100)
                .ToListAsync());
        }

        // GET: XXORA_DISCOUNT_LIST/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_DISCOUNT_LIST = await _context.XXORA_DISCOUNT_LISTs
                .FirstOrDefaultAsync(m => m.ITEM_NUMBER == id);
            if (xXORA_DISCOUNT_LIST == null)
            {
                return NotFound();
            }

            return View(xXORA_DISCOUNT_LIST);
        }

        // GET: XXORA_DISCOUNT_LIST/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: XXORA_DISCOUNT_LIST/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DISCOUNT_LIST_ID,DISCOUNT_LIST_NAME,BU_NAME,CURRENCY_CODE,DISCOUNT_LIST_ITEM_ID,ITEM_NUMBER,PRICING_UOM_CODE,RULE_DISCOUNT_NAME,PRICING_RULE_TYPE_CODE,PARTY_NUMBER,DISCOUNT_TYPE,DISCOUNT_PRICE,START_DATE,END_DATE,STATUS,CREATION_DATE,CREATED_BY,LAST_UPDATE_DATE,LAST_UPDATED_BY")] XXORA_DISCOUNT_LIST xXORA_DISCOUNT_LIST)
        {
            if (ModelState.IsValid)
            {
                _context.Add(xXORA_DISCOUNT_LIST);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(xXORA_DISCOUNT_LIST);
        }

        // GET: XXORA_DISCOUNT_LIST/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_DISCOUNT_LIST = await _context.XXORA_DISCOUNT_LISTs.FindAsync(id);
            if (xXORA_DISCOUNT_LIST == null)
            {
                return NotFound();
            }
            return View(xXORA_DISCOUNT_LIST);
        }

        // POST: XXORA_DISCOUNT_LIST/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("DISCOUNT_LIST_ID,DISCOUNT_LIST_NAME,BU_NAME,CURRENCY_CODE,DISCOUNT_LIST_ITEM_ID,ITEM_NUMBER,PRICING_UOM_CODE,RULE_DISCOUNT_NAME,PRICING_RULE_TYPE_CODE,PARTY_NUMBER,DISCOUNT_TYPE,DISCOUNT_PRICE,START_DATE,END_DATE,STATUS,CREATION_DATE,CREATED_BY,LAST_UPDATE_DATE,LAST_UPDATED_BY")] XXORA_DISCOUNT_LIST xXORA_DISCOUNT_LIST)
        {
            if (id != xXORA_DISCOUNT_LIST.ITEM_NUMBER)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(xXORA_DISCOUNT_LIST);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!XXORA_DISCOUNT_LISTExists(xXORA_DISCOUNT_LIST.ITEM_NUMBER))
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
            return View(xXORA_DISCOUNT_LIST);
        }

        // GET: XXORA_DISCOUNT_LIST/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_DISCOUNT_LIST = await _context.XXORA_DISCOUNT_LISTs
                .FirstOrDefaultAsync(m => m.ITEM_NUMBER == id);
            if (xXORA_DISCOUNT_LIST == null)
            {
                return NotFound();
            }

            return View(xXORA_DISCOUNT_LIST);
        }

        // POST: XXORA_DISCOUNT_LIST/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var xXORA_DISCOUNT_LIST = await _context.XXORA_DISCOUNT_LISTs.FindAsync(id);
            if (xXORA_DISCOUNT_LIST != null)
            {
                _context.XXORA_DISCOUNT_LISTs.Remove(xXORA_DISCOUNT_LIST);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool XXORA_DISCOUNT_LISTExists(string id)
        {
            return _context.XXORA_DISCOUNT_LISTs.Any(e => e.ITEM_NUMBER == id);
        }
    }
}
