using System;
using UnityEngine;
using Portal.Identity.Helpers;

namespace Portal.Identity.Core
{
    public class BrowserResponse
    {
        public string responseFor;
        public string requestId;
        public bool success;
        public string errorType;
        public string error;
    }

    public class StringResponse : BrowserResponse
    {
        public string result;
    }

    public class BoolResponse : BrowserResponse
    {
        public bool result;
    }

    public static class BrowserResponseExtensions
    {
        /// <summary>
        /// Deserializes the json to StringResponse and returns the result
        /// See <see cref="Portal.Identity.Core.BrowserResponse.StringResponse"></param>
        /// </summary>
        public static string GetStringResult(this string json)
        {
            StringResponse stringResponse = json.OptDeserializeObject<StringResponse>();
            if (stringResponse != null)
            {
                return stringResponse.result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes the json to BoolResponse and returns the result
        /// See <see cref="Portal.Identity.Core.BrowserResponse.BoolResponse"></param>
        /// </summary>
        public static Nullable<bool> GetBoolResponse(this string json)
        {
            BoolResponse boolResponse = json.OptDeserializeObject<BoolResponse>();
            if (boolResponse != null)
            {
                return boolResponse.result;
            }
            else
            {
                return null;
            }
        }
    }
}
