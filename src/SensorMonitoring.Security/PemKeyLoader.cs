using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitoring.Security
{
    public class PemKeyLoader
    {

        public const string PublicKeySuffix = ".public.pem";
        public const string PrivateKeySuffix = ".private.pem";

        public static RSA LoadRsa(string path)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(path));
            return rsa;
        }

        public static ECDsa LoadEcdsa(string path)
        {
            var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(File.ReadAllText(path));
            return ecdsa;
        }

        public static IReadOnlyDictionary<string, ECDsa> LoadSensorPublicKeys(string directory)
        {
            var keys = new Dictionary<string, ECDsa>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(directory, $"*{PublicKeySuffix}"))
            {
                var fileName = Path.GetFileName(path);
                var sensorId = fileName[..^PublicKeySuffix.Length];
                keys[sensorId] = LoadEcdsa(path);
            }

            return keys;
        }
    }
}
