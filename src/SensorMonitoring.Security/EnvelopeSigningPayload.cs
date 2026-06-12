using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensorMonitoring.Contracts;

namespace SensorMonitoring.Security
{
    public class EnvelopeSigningPayload
    {
        public static byte[] Build(SecureEnvelope envelope) =>
        Build(envelope.SensorId, envelope.EncryptedKey, envelope.Nonce, envelope.Ciphertext, envelope.Tag);

        public static byte[] Build(string sensorId, string encryptedKey, string nonce, string ciphertext, string tag)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            WriteField(writer, sensorId);
            WriteField(writer, encryptedKey);
            WriteField(writer, nonce);
            WriteField(writer, ciphertext);
            WriteField(writer, tag);

            writer.Flush();
            return stream.ToArray();
        }

        private static void WriteField(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
