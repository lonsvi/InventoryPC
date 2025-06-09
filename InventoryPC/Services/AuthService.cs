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
                Log($"Authenticating user: {login}");
                var user = await _dbService.GetUserByLoginAsync(login);
                if (user == null)
                {
                    Log($"Authentication failed: User {login} not found in database.");
                    return null;
                }

                Log($"User found: {login}, Stored hash: {user.PasswordHash}, Role: {user.Role}");
                string passwordHash = ComputeSHA256Hash(password);
                Log($"Generated hash for password: {passwordHash}");
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
        public string TestHash(string input)
        {
            return ComputeSHA256Hash(input);
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