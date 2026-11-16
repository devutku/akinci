using Akinci.Domain.Enums;

namespace Akinci.Domain.Exceptions;

/// <summary>
/// Kullanıcı adı gerektiren bir kimlik doğrulama türü kullanıcı adı olmadan oluşturulmak istendiğinde fırlatılır.
/// </summary>
public sealed class MissingCredentialUserNameException : DomainException
{
    public AuthenticationType AuthenticationType { get; }

    public MissingCredentialUserNameException(AuthenticationType authenticationType)
        : base(BuildMessage(authenticationType))
    {
        AuthenticationType = authenticationType;
    }

    private static string BuildMessage(AuthenticationType type) => type switch
    {
        AuthenticationType.Ntlm => "NTLM kimlik doğrulama için kullanıcı adı boş olamaz.",
        AuthenticationType.Kerberos => "Kerberos kimlik doğrulama için kullanıcı adı veya kullanıcı anahtarı boş olamaz.",
        AuthenticationType.Negotiate => "Negotiate kimlik doğrulama için kullanıcı adı boş olamaz.",
        _ => "Basic kimlik doğrulama için kullanıcı adı boş olamaz."
    };
}
