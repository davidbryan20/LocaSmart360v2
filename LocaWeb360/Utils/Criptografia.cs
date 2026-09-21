using System;
using System.Security.Cryptography;
using System.Text;

namespace LocaSmart360.Utils
{
    public static class Criptografia
    {
        /// <summary>
        /// Transforma uma string de texto limpo em um Hash SHA-256 hexadecimal.
        /// Garantia de conformidade com a LGPD e segurança de dados no Supabase.
        /// </summary>
        public static string GerarHash(string senha)
        {
            if (string.IsNullOrEmpty(senha)) return string.Empty;

            // Converte a senha em bytes, calcula o Hash SHA-256 e transforma em texto Hexadecimal
            byte[] bytes = Encoding.UTF8.GetBytes(senha);
            byte[] hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}