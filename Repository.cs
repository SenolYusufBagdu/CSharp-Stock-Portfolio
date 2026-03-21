
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PortfolioEngine.Models;

namespace PortfolioEngine.Repository
{
    public class UserRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public UserRepository(string filePath = "portfolio.json")
        {
            _filePath = filePath;
        }

        // load all users from disk — returns empty list if file missing or corrupted
        public List<User> LoadAll()
        {
            if (!File.Exists(_filePath))
                return new List<User>();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<User>>(json, _options)
                       ?? new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Repository] Failed to load data: {ex.Message}");
                return new List<User>();
            }
        }

        // persist all users to disk
        public void SaveAll(List<User> users)
        {
            try
            {
                File.WriteAllText(_filePath, JsonSerializer.Serialize(users, _options));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Repository] Failed to save data: {ex.Message}");
            }
        }
    }
}
