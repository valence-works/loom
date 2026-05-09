namespace Loom;

public interface IRecipeSerializer
{
    string Format { get; }

    Recipe Deserialize(string content);
}
