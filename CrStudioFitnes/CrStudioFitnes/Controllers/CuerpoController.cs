using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CrStudioFitnes.Controllers
{
    [Authorize(Roles = "Administrador,Entrenador")]
    public class CuerpoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuerpoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cuerpo
        public async Task<IActionResult> Index()
        {
            var data = await _context.Cuerpos.AsNoTracking().ToListAsync();
            return View(data);
        }

        // GET: Cuerpo/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cuerpo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCuerpo,Nombre,Detalle")] Cuerpo cuerpo)
        {
            if (!ModelState.IsValid) return View(cuerpo);

            _context.Add(cuerpo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Cuerpo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cuerpo = await _context.Cuerpos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdCuerpo == id);

            if (cuerpo == null) return NotFound();

            return View(cuerpo);
        }

        // POST: Cuerpo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cuerpo = await _context.Cuerpos.FindAsync(id);
            if (cuerpo != null)
            {
                _context.Cuerpos.Remove(cuerpo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
