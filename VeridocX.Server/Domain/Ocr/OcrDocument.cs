namespace VeridocX.Server.Domain.Ocr;

public record OcrTable(IReadOnlyList<IReadOnlyList<string>> Rows);

public record OcrDocument(string Text, IReadOnlyList<OcrTable> Tables);
