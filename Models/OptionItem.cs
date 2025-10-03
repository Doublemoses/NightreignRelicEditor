namespace NightreignRelicEditor.Models;

public class OptionItem<T> where T : notnull
{
    public string Name { get; set; } = "-";
    public required T Value { get; set; }
}