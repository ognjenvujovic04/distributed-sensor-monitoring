namespace SensorMonitoring.Contracts;

public sealed record SecureEnvelope(
    string SensorId,
    string EncryptedKey,
    string Nonce,
    string Ciphertext,
    string Tag,
    string Signature);