using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VCS.Models;

namespace VCS.Utils
{
    public class CurrentSessionManager
    {
        static string ApplicationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Entrivax", "VCS");
        static string SessionFilePath = Path.Combine(ApplicationPath, "Session.json");
        public static async Task Save(SessionData sessionData)
        {
            if (!Directory.Exists(ApplicationPath))
            {
                Directory.CreateDirectory(ApplicationPath);
            }

            using (var fileStream = File.Open(SessionFilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, sessionData);
            }
        }

        public static async Task<SessionData?> Load()
        {
            if (!File.Exists(SessionFilePath))
            {
                return null;
            }

            using (var fileStream = File.OpenRead(SessionFilePath))
            {
                return await JsonSerializer.DeserializeAsync<SessionData>(fileStream);
            }
        }

        public static void DeleteSession()
        {
            if (File.Exists(SessionFilePath))
            {
                File.Delete(SessionFilePath);
            }
        }
    }

    public class SessionData
    {
        public FileConfig[]? Files { get; set; }
    }
}
