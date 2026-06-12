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
    public sealed class SecureEnvelopeOpener : ISecureEnvelopeOpener, IDisposable
    {
        private readonly RSA _serverPrivateKey;
        private readonly IReadOnlyDictionary<string, ECDsa> _sensorPublicKeys;

        public SecureEnvelopeOpener(RSA serverPrivateKey, IReadOnlyDictionary<string, ECDsa> sensorPublicKeys)
        {
            _serverPrivateKey = serverPrivateKey;
            _sensorPublicKeys = sensorPublicKeys;
        }

        public EnvelopeOpenResult Open(SecureEnvelope envelope)
        {
            if (string.IsNullOrWhiteSpace(envelope.SensorId))
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.MalformedEnvelope);
            }

            byte[] encryptedKey, nonce, ciphertext, tag, signature;
            try
            {
                encryptedKey = Convert.FromBase64String(envelope.EncryptedKey);
                nonce = Convert.FromBase64String(envelope.Nonce);
                ciphertext = Convert.FromBase64String(envelope.Ciphertext);
                tag = Convert.FromBase64String(envelope.Tag);
                signature = Convert.FromBase64String(envelope.Signature);
            }
            catch (FormatException)
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.MalformedEnvelope);
            }

            if (nonce.Length != SensorMessageProtector.NonceSizeBytes ||
                tag.Length != SensorMessageProtector.TagSizeBytes)
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.MalformedEnvelope);
            }

            if (!_sensorPublicKeys.TryGetValue(envelope.SensorId, out var sensorPublicKey))
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.UnknownSensorKey);
            }

            var signingPayload = EnvelopeSigningPayload.Build(envelope);
            if (!sensorPublicKey.VerifyData(signingPayload, signature, HashAlgorithmName.SHA256))
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.InvalidSignature);
            }

            try
            {
                var aesKey = _serverPrivateKey.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);

                var plaintext = new byte[ciphertext.Length];
                using (var aes = new AesGcm(aesKey, SensorMessageProtector.TagSizeBytes))
                {
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);
                }

                var message = JsonSerializer.Deserialize<SensorMessage>(plaintext);
                return message is null
                    ? EnvelopeOpenResult.Fail(EnvelopeOpenStatus.MalformedEnvelope)
                    : EnvelopeOpenResult.Success(message);
            }
            catch (CryptographicException)
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.DecryptionFailed);
            }
            catch (JsonException)
            {
                return EnvelopeOpenResult.Fail(EnvelopeOpenStatus.MalformedEnvelope);
            }
        }

        public void Dispose()
        {
            _serverPrivateKey.Dispose();
            foreach (var key in _sensorPublicKeys.Values)
            {
                key.Dispose();
            }
        }
    }
}
