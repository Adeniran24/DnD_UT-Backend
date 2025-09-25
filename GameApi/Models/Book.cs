public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    // kép URL (feltöltés után /uploads/cover.jpg pl.)
    public string CoverImagePath { get; set; } = string.Empty;

    // könyv URL (feltöltés után /uploads/book.pdf pl.)
    public string FilePath { get; set; } = string.Empty;
}
