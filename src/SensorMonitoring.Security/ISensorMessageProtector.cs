using SensorMonitoring.Contracts;

namespace SensorMonitoring.Security;

public interface ISensorMessageProtector
{
    SecureEnvelope Protect(SensorMessage message);
}