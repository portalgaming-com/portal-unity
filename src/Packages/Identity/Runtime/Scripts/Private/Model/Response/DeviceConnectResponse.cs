using Portal.Identity.Core;

namespace Portal.Identity.Model
{
    public class DeviceConnectResponse : BrowserResponse
    {
        public string code;
        public string deviceCode;
        public string url;
        public int interval;
    }
}
