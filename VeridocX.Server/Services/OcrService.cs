using Azure;
using Azure.AI.DocumentIntelligence;
using VeridocX.Server.Domain.Ocr;

namespace VeridocX.Server.Services;

public interface IOcrService
{
    Task<OcrDocument> ReadAsync(BinaryData image, CancellationToken ct);
}

public sealed class DocumentIntelligenceOcrService(DocumentIntelligenceClient client) : IOcrService
{
    public async Task<OcrDocument> ReadAsync(BinaryData image, CancellationToken ct)
    {
        Operation<AnalyzeResult> operation =
            await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", image, ct);

        var result = operation.Value;

        var tables = new List<OcrTable>();
        foreach (var table in result.Tables)
        {
            var grid = new string[table.RowCount][];
            for (var r = 0; r < table.RowCount; r++)
            {
                grid[r] = new string[table.ColumnCount];
                Array.Fill(grid[r], string.Empty);
            }

            foreach (var cell in table.Cells)
            {
                if (cell.RowIndex < table.RowCount && cell.ColumnIndex < table.ColumnCount)
                    grid[cell.RowIndex][cell.ColumnIndex] = cell.Content ?? string.Empty;
            }

            tables.Add(new OcrTable(grid.Select(row => (IReadOnlyList<string>)row).ToList()));
        }

        return new OcrDocument(result.Content ?? string.Empty, tables);
    }
}
