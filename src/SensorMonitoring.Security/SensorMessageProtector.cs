using SensorMonitoring.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SensorMonitoring.Security
{
    public sealed class SensorMessageProtector : ISensorMessageProtector, IDisposable
    {
        public const int AesKeySizeBytes = 32;
        public const int NonceSizeBytes = 12;
        public const int TagSizeBytes = 16;

        private readonly RSA _serverPublicKey;
        private readonly ECDsa _sensorPrivateKey;

        public SensorMessageProtector(RSA serverPublicKey, ECDsa sensorPrivateKey)
        {
            _serverPublicKey = serverPublicKey;
            _sensorPrivateKey = sensorPrivateKey;
        }

        public SecureEnvelope Protect(SensorMessage message)
        {
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(message);

            var aesKey = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSizeBytes];

            using (var aes = new AesGcm(aesKey, TagSizeBytes))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var encryptedKey = _serverPublicKey.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

            var sensorId = message.SensorId;
            var encryptedKeyB64 = Convert.ToBase64String(encryptedKey);
            var nonceB64 = Convert.ToBase64String(nonce);
            var ciphertextB64 = Convert.ToBase64String(ciphertext);
            var tagB64 = Convert.ToBase64String(tag);

            var signingPayload = EnvelopeSigningPayload.Build(sensorId, encryptedKeyB64, nonceB64, ciphertextB64, tagB64);
            var signature = _sensorPrivateKey.SignData(signingPayload, HashAlgorithmName.SHA256);

            return new SecureEnvelope(
                sensorId,
                encryptedKeyB64,
                nonceB64,
                ciphertextB64,
                tagB64,
                Convert.ToBase64String(signature));
        }

        public void Dispose()
        {
            _serverPublicKey.Dispose();
            _sensorPrivateKey.Dispose();
        }
    }
}