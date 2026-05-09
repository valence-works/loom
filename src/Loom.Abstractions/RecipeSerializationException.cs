namespace Loom;

public sealed class RecipeSerializationException(string message, Exception? innerException = null) : Exception(message, innerException);
