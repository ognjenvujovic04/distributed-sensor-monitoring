using SensorMonitoring.Contracts;

namespace SensorMonitoring.Security;

public interface ISecureEnvelopeOpener
{
    EnvelopeOpenResult Open(SecureEnvelope envelope);
}

public enum EnvelopeOpenStatus
{
    Ok,
    UnknownSensorKey,
    InvalidSignature,
    DecryptionFailed,
    MalformedEnvelope
}

public sealed record EnvelopeOpenResult(EnvelopeOpenStatus Status, SensorMessage? Message = null)
{
    public static EnvelopeOpenResult Success(SensorMessage message) => new(EnvelopeOpenStatus.Ok, message);
    public static EnvelopeOpenResult Fail(EnvelopeOpenStatus status) => new(status);
}