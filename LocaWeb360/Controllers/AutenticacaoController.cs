using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocaSmart360.Data;
using LocaSmart360.Models;
using LocaSmart360.Utils;
using System.Text.RegularExpressions;

namespace LocaSmart360.Controllers
{
    public class AutenticacaoController : Controller
    {
        private readonly LocaSmartDbContext _context;
        private readonly IConfiguration _configuration;

        public AutenticacaoController(LocaSmartDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private async Task<bool> EstaOnlineAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (HttpContext.Session.GetString("UsuarioId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // CORREÇÃO: Verifica o status do Supabase para exibir corretamente no badge da tela de login
            ViewBag.SistemaOnline = await EstaOnlineAsync();
            return View("~/Views/Autenticacao/Login.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string senha)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ViewBag.SistemaOnline = await EstaOnlineAsync();
                ViewBag.Erro = "Por favor, preencha todos os campos.";
                return View("~/Views/Autenticacao/Login.cshtml");
            }

            try
            {
                string senhaCriptografada = Criptografia.GerarHash(senha);

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email && u.SenhaHash == senhaCriptografada);

                if (usuario != null)
                {
                    SetarSessao(usuario.UsuarioId.ToString(), usuario.Nome, usuario.NomeLoja, usuario.Cargo ?? "Lojista");
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.SistemaOnline = true;
                ViewBag.Erro = "E-mail ou senha incorretos.";
                return View("~/Views/Autenticacao/Login.cshtml");
            }
            catch (Exception)
            {
                // Modo de Simulação (Fallback caso o Supabase caia)
                var demoEmail = _configuration["ModoDemo:Email"];
                var demoSenha = _configuration["ModoDemo:Senha"];

                if (email == demoEmail && senha == demoSenha)
                {
                    SetarSessao("0", "Administrador Demo", "Modo Simulação Offline", "Administrador");
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.SistemaOnline = false;
                ViewBag.Erro = "Erro de conexão com o banco de dados. Verifique sua rede.";
                return View("~/Views/Autenticacao/Login.cshtml");
            }
        }

        [HttpGet]
        public IActionResult Cadastro() => View("~/Views/Autenticacao/Cadastro.cshtml");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(Usuario novoUsuario, string senha, string confirmarSenha)
        {
            if (string.IsNullOrEmpty(senha) || senha != confirmarSenha)
            {
                ViewBag.Erro = "As senhas não coincidem ou estão vazias.";
                return View("~/Views/Autenticacao/Cadastro.cshtml", novoUsuario);
            }

            if (!ValidarSenhaForte(senha))
            {
                ViewBag.Erro = "A senha deve ter no mínimo 8 caracteres, com maiúscula, minúscula e caractere especial.";
                return View("~/Views/Autenticacao/Cadastro.cshtml", novoUsuario);
            }

            ModelState.Remove("SenhaHash");

            try
            {
                var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == novoUsuario.Email);
                if (emailExiste)
                {
                    ViewBag.Erro = "E-mail já cadastrado.";
                    return View("~/Views/Autenticacao/Cadastro.cshtml", novoUsuario);
                }

                if (string.IsNullOrEmpty(novoUsuario.Cargo)) novoUsuario.Cargo = "Lojista";

                novoUsuario.SenhaHash = Criptografia.GerarHash(senha);
                _context.Usuarios.Add(novoUsuario);
                await _context.SaveChangesAsync();

                TempData["SucessoCadastro"] = "Conta criada com sucesso! Faça seu login.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["SucessoCadastro"] = "Simulação (Offline): Cadastro realizado com sucesso!";
                return RedirectToAction("Login");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private bool ValidarSenhaForte(string senha)
        {
            if (senha.Length < 8) return false;
            return Regex.IsMatch(senha, @"[A-Z]") && Regex.IsMatch(senha, @"[a-z]") && Regex.IsMatch(senha, @"[\W_]");
        }

        private void SetarSessao(string id, string nome, string nomeLoja, string cargo)
        {
            HttpContext.Session.SetString("UsuarioId", id);
            HttpContext.Session.SetString("UsuarioNome", nome);
            HttpContext.Session.SetString("NomeLoja", nomeLoja);
            HttpContext.Session.SetString("UsuarioCargo", cargo);
        }
    }
}