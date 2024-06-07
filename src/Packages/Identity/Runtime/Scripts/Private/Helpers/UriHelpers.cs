using System;
using UnityEngine;

namespace Portal.Identity.Helpers
{
    public static class UriExtensions
    {
        /// <summary>
        /// Gets the specified query parameter from the given URI
        /// </summary> 
        public static string GetQueryParameter(this Uri uri, string key)
        {
            try
            {
                string query = uri.Query;
                string[] queryParameters = query.Split(new char[] { '?', '&' });
                for (int i = 0; i < queryParameters.Length; i++)
                {
                    string[] keyValue = queryParameters[i].Split('=');
                    if (keyValue[0] == key)
                    {
                        return keyValue[1];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to get query parameter {key}: {e.Message}");
            }
            return null;
        }
    }
}