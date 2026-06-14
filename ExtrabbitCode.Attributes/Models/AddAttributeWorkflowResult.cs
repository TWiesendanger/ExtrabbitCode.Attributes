namespace ExtrabbitCode.Attributes.Models;

public sealed record AddAttributeWorkflowResult(
    bool IsSuccess,
    object? OwnerObject,
    AddAttributeDialogResult? Input,
    string? ErrorMessage = null);