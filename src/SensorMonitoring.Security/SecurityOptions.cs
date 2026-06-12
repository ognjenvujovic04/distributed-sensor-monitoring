using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitoring.Security
{
    public sealed class SecurityOptions
    {
        public const string SectionName = "Security";

        public string ServerPrivateKeyPath { get; set; } = "keys/server/server_rsa_private.pem";

        public string SensorPublicKeysDirectory { get; set; } = "keys/server/sensors";

        public int TimestampToleranceSeconds { get; set; } = 30;
    }
}
