namespace NightreignRelicEditor.Models;

public enum RelicErrors
{
    Legitimate,
    MultipleFromCategory,           // More than one effect from the same category
    UniqueRelicEffect,              // Effects that only appear on unique relics
    NotRelicEffect,                 // Effects that should never appear on relics
}