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
    public class XXORA_CUSTOMER_MASTERController : Controller
    {
        private readonly OracleContext _context;

        public XXORA_CUSTOMER_MASTERController(OracleContext context)
        {
            _context = context;
        }

        // GET: XXORA_CUSTOMER_MASTER
        public async Task<IActionResult> Index()
        {
            var data = await _context.XXORA_CUSTOMER_MASTERs
                .AsNoTracking()
                .OrderBy(x => x.REGISTRY_ID)  // o la columna que tenga sentido para vos
                .Take(100)
                .ToListAsync();

            return View(data);
        }

        // GET: XXORA_CUSTOMER_MASTER/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_CUSTOMER_MASTER = await _context.XXORA_CUSTOMER_MASTERs
                .FirstOrDefaultAsync(m => m.REGISTRY_ID == id);
            if (xXORA_CUSTOMER_MASTER == null)
            {
                return NotFound();
            }

            return View(xXORA_CUSTOMER_MASTER);
        }

        // GET: XXORA_CUSTOMER_MASTER/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: XXORA_CUSTOMER_MASTER/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BU_NOMBRE,REGISTRY_ID,PARTY_NAME,PARTY_ID,IDCLIENTE,NOMBRE_CLIENTE,NOMBRE_CLASECLIENTE,ACCOUNT_ID,CLIENTE_ESTATUS,SITIO,PARTY_SITE_NUMBER,NOMBRE_SITIO,SITIO_ESTATUS,PAIS,SITIO_DIR1,SITIO_DIR2,SITIO_DIR3,SITIO_CIUDAD,SITIO_ESTADO,SITIO_POSTALCODE,SITIO_PROVINCIA,SITIO_CANTON,SITIO_DISTRITO,EMAIL_CLIENTE,TELEFONO1_CLIENTE,LATITUD_MUNICIPIO,LONGITUD_MUNICIPIO,VENDEDOR,GRUPO_CLIENTE,RUTA,AR_NUMERO,CATEGORIA,CUST_ACCT_SITE_ID,SITE_USE_ID,BILL_TO_SITE_USE_ID,ORGANIZATION_ID,LIMITECREDITO,LIMITECREDITO_MONEDA,TERMINO_PAGO,PARTY_SITE_PRIMARY_FLAG,CEDULA,IDVENDEDOR,PARTY_SITE_ID,ACCT_LAST_UPDATE_DATE,SITE_LAST_UPDATE_DATE,BILL_TO_SITE")] XXORA_CUSTOMER_MASTER xXORA_CUSTOMER_MASTER)
        {
            if (ModelState.IsValid)
            {
                _context.Add(xXORA_CUSTOMER_MASTER);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(xXORA_CUSTOMER_MASTER);
        }

        // GET: XXORA_CUSTOMER_MASTER/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_CUSTOMER_MASTER = await _context.XXORA_CUSTOMER_MASTERs.FindAsync(id);
            if (xXORA_CUSTOMER_MASTER == null)
            {
                return NotFound();
            }
            return View(xXORA_CUSTOMER_MASTER);
        }

        // POST: XXORA_CUSTOMER_MASTER/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("BU_NOMBRE,REGISTRY_ID,PARTY_NAME,PARTY_ID,IDCLIENTE,NOMBRE_CLIENTE,NOMBRE_CLASECLIENTE,ACCOUNT_ID,CLIENTE_ESTATUS,SITIO,PARTY_SITE_NUMBER,NOMBRE_SITIO,SITIO_ESTATUS,PAIS,SITIO_DIR1,SITIO_DIR2,SITIO_DIR3,SITIO_CIUDAD,SITIO_ESTADO,SITIO_POSTALCODE,SITIO_PROVINCIA,SITIO_CANTON,SITIO_DISTRITO,EMAIL_CLIENTE,TELEFONO1_CLIENTE,LATITUD_MUNICIPIO,LONGITUD_MUNICIPIO,VENDEDOR,GRUPO_CLIENTE,RUTA,AR_NUMERO,CATEGORIA,CUST_ACCT_SITE_ID,SITE_USE_ID,BILL_TO_SITE_USE_ID,ORGANIZATION_ID,LIMITECREDITO,LIMITECREDITO_MONEDA,TERMINO_PAGO,PARTY_SITE_PRIMARY_FLAG,CEDULA,IDVENDEDOR,PARTY_SITE_ID,ACCT_LAST_UPDATE_DATE,SITE_LAST_UPDATE_DATE,BILL_TO_SITE")] XXORA_CUSTOMER_MASTER xXORA_CUSTOMER_MASTER)
        {
            if (id != xXORA_CUSTOMER_MASTER.REGISTRY_ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(xXORA_CUSTOMER_MASTER);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!XXORA_CUSTOMER_MASTERExists(xXORA_CUSTOMER_MASTER.REGISTRY_ID))
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
            return View(xXORA_CUSTOMER_MASTER);
        }

        // GET: XXORA_CUSTOMER_MASTER/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xXORA_CUSTOMER_MASTER = await _context.XXORA_CUSTOMER_MASTERs
                .FirstOrDefaultAsync(m => m.REGISTRY_ID == id);
            if (xXORA_CUSTOMER_MASTER == null)
            {
                return NotFound();
            }

            return View(xXORA_CUSTOMER_MASTER);
        }

        // POST: XXORA_CUSTOMER_MASTER/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var xXORA_CUSTOMER_MASTER = await _context.XXORA_CUSTOMER_MASTERs.FindAsync(id);
            if (xXORA_CUSTOMER_MASTER != null)
            {
                _context.XXORA_CUSTOMER_MASTERs.Remove(xXORA_CUSTOMER_MASTER);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool XXORA_CUSTOMER_MASTERExists(string id)
        {
            return _context.XXORA_CUSTOMER_MASTERs.Any(e => e.REGISTRY_ID == id);
        }
    }
}
