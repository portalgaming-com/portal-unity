using System;

namespace Portal.Identity.Model
{
    public enum IdentityErrorType
    {
        INITIALIZATION_ERROR,
        AUTHENTICATION_ERROR,
        USER_REGISTRATION_ERROR,
        REFRESH_TOKEN_ERROR,
        OPERATION_NOT_SUPPORTED_ERROR,
        NOT_LOGGED_IN_ERROR
    }

    public class IdentityException : Exception
    {
        public Nullable<IdentityErrorType> Type;

        public IdentityException(string message, Nullable<IdentityErrorType> type = null) : base(message)
        {
            this.Type = type;
        }

        /**
        * The error message for api requests via axios that fail due to network connectivity is "Network Error".
        * This isn't the most reliable way to determine connectivity but it is currently the best we have. 
        */
        public bool IsNetworkError()
        {
            return Message.EndsWith("Network Error");
        }
    }
}