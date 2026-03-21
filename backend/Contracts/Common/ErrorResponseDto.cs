namespace Backend.Contracts.Common;

public sealed class ErrorResponseDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public IDictionary<string, string[]>? DetallesValidacion { get; set; }
}
