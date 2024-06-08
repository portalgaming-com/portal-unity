using System;

namespace Portal.Identity.Model
{
    [Serializable]
    internal class ConfirmCodeRequest
    {
        public string deviceCode;
        public int interval;
        public long? timeoutMs;
    }
}

