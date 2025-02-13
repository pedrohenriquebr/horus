using System.Runtime.Serialization;

namespace Horus.Modules.Shared.Contracts.SharedKernel.Exceptions;

public class GlobalApplicationException : Exception
{
    public GlobalApplicationException(ApplicationExceptionType type, string message) : base(message)
    {
        Type = type;
    }

    public GlobalApplicationException(ApplicationExceptionType type, string message, Exception inner) : base(message,
        inner)
    {
        Type = type;
    }

    public GlobalApplicationException(ApplicationExceptionType type, string message, ApplicationErrorCode code) :
        base(message)
    {
        Type = type;
        Code = code;
    }

    public GlobalApplicationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ApplicationExceptionType Type { get; private set; }
    public ApplicationErrorCode? Code { get; private set; }
    public List<string> Errors { get; } = new();


    public GlobalApplicationException AddError(string error)
    {
        Errors.Add(error);
        return this;
    }

    public GlobalApplicationException AddErrors(List<string> errors)
    {
        Errors.AddRange(errors);
        return this;
    }
}

public enum ApplicationErrorCode
{
    ConfirmationCodeExpired
}

public enum ApplicationExceptionType
{
    Application,
    Validation,
    Business
}