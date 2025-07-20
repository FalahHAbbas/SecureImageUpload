
using System.Security.Cryptography;
using System.Text;

namespace StorageService;

public interface IHmacSignatureValidator
{
    string Sign(string data);
    bool IsValid(string data, string signature);
}

public class HmacSignatureValidator : IHmacSignatureValidator
{
    private readonly HmacSettings _settings;

    public HmacSignatureValidator(HmacSettings settings)
    {
        _settings = settings;
    }

    public string Sign(string data)
    {
        if (_settings.ApiSecret == null) throw new InvalidOperationException("ApiSecret is not configured.");
        var key = Encoding.UTF8.GetBytes(_settings.ApiSecret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    public bool IsValid(string data, string signature)
    {
        return Sign(data) == signature;
    }
}
