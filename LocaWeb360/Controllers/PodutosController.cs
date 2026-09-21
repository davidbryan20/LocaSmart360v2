using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocaSmart360.Data;
using LocaSmart360.Models;

namespace LocaSmart360.Controllers
{
    public class ProdutosController : Controller
    {
        private readonly LocaSmartDbContext _context;

        public ProdutosController(LocaSmartDbContext context)
        {
            _context = context;
        }

        // GET: Produtos
        public async Task<IActionResult> Index()
        {
            var produtos = await _context.Produtos.OrderBy(p => p.Nome).ToListAsync();
            return View(produtos);
        }

        // POST: Produtos/Salvar (Cria ou Edita via Modal Único)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(Produto produto)
        {
            if (ModelState.IsValid)
            {
                if (produto.ProdutoId == Guid.Empty)
                {
                    // Criação
                    produto.ProdutoId = Guid.NewGuid();
                    _context.Produtos.Add(produto);
                    TempData["Sucesso"] = "Produto cadastrado com sucesso no catálogo!";
                }
                else
                {
                    // Edição
                    try
                    {
                        _context.Produtos.Update(produto);
                        TempData["Sucesso"] = "Produto atualizado com sucesso!";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ProdutoExists(produto.ProdutoId)) return NotFound();
                        else throw;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            TempData["Erro"] = "Houve um erro ao salvar o produto. Verifique os campos.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Produtos/Excluir (Remove via Modal de Confirmação)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto != null)
            {
                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Produto excluído do catálogo com sucesso!";
            }
            else
            {
                TempData["Erro"] = "Produto não encontrado.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProdutoExists(Guid id)
        {
            return _context.Produtos.Any(e => e.ProdutoId == id);
        }
    }
}