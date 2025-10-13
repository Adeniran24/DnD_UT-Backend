public class PdfFormData
{
    public int Id { get; set; }
    public int PdfFileId { get; set; }
    public string FieldValuesJson { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;
}
