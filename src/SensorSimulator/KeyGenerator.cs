using SensorMonitoring.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SensorSimulator
{
    public static class KeyGenerator
    {
        public static void Generate(string outputDirectory)
        {
            var serverDir = Path.Combine(outputDirectory, "server");
            var serverSensorsDir = Path.Combine(serverDir, "sensors");
            var clientDir = Path.Combine(outputDirectory, "client");
            var clientSensorsDir = Path.Combine(clientDir, "sensors");

            Directory.CreateDirectory(serverSensorsDir);
            Directory.CreateDirectory(clientSensorsDir);

            using (var serverRsa = RSA.Create(2048))
            {
                File.WriteAllText(
                    Path.Combine(serverDir, "server_rsa_private.pem"),
                    serverRsa.ExportPkcs8PrivateKeyPem());
                File.WriteAllText(
                    Path.Combine(clientDir, "server_rsa_public.pem"),
                    serverRsa.ExportSubjectPublicKeyInfoPem());
            }

            Console.WriteLine("Generated server RSA-2048 key pair.");

            foreach (var sensorId in SensorConfig.Sensors.Keys)
            {
                using var sensorKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                File.WriteAllText(
                    Path.Combine(clientSensorsDir, $"{sensorId}{PemKeyLoader.PrivateKeySuffix}"),
                    sensorKey.ExportPkcs8PrivateKeyPem());
                File.WriteAllText(
                    Path.Combine(serverSensorsDir, $"{sensorId}{PemKeyLoader.PublicKeySuffix}"),
                    sensorKey.ExportSubjectPublicKeyInfoPem());
                Console.WriteLine($"Generated ECDSA P-256 key pair for {sensorId}.");
            }

            Console.WriteLine();
            Console.WriteLine($"Keys written to: {Path.GetFullPath(outputDirectory)}");
            Console.WriteLine("  server/  -> stays on the server machine (mounted into IngestionService)");
            Console.WriteLine("  client/  -> copy to the machine running the sensor simulators");
        }
    }
}
