namespace Akinci.Application.Exceptions;

/// <summary>
/// Arşiv veya program gibi bir dosya beklenirken sunucu bir web sayfası (HTML) döndürdüğünde fırlatılır.
/// Bağlantı doğrudan indirme linki değildir; yeniden denemek işe yaramaz.
/// </summary>
public sealed class HtmlInsteadOfFileException : InvalidOperationException
{
    public HtmlInsteadOfFileException()
        : base("Sunucu doğrudan dosya yerine bir web sayfası (HTML) döndürdü. Yapıştırdığınız bağlantı doğrudan dosya indirme linki (Direct Download Link) değil, bir web sayfasıdır.")
    {
    }
}
