namespace InternetCafe.API.ExceptionHandling;

public sealed class ConflictException(string message) : Exception(message);
