using InventoryPC.Models;
using System;
using System.IO; // Добавлено пространство имен для работы с File
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InventoryPC.Services
{
    public class AuthService
    {
        private readonly DatabaseService _dbService;
        private readonly string _logPath = @"C:\Inventory\log.txt";

        public AuthService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task<User?> AuthenticateAsync(string login, string password)
        {
            try
            {
                var user = await _dbService.GetUserByLoginAsync(login);
                if (user == null)
                {
                    Log($"Authentication failed: User {login} not found.");
                    return null;
                }

                string passwordHash = ComputeSHA256Hash(password);
                if (user.PasswordHash == passwordHash)
                {
                    Log($"Authentication successful for user: {login}, Role: {user.Role}");
                    return user;
                }
                else
                {
                    Log($"Authentication failed: Incorrect password for user {login}.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log($"Error in AuthenticateAsync: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private string ComputeSHA256Hash(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder();
            foreach (byte b in hash)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n", Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
    }
}