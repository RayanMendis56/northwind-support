namespace SupportDesk.Application.Exceptions;

/// <summary>Credentials were missing or rejected. Surfaces as HTTP 401.</summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);
