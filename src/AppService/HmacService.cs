
using System;
using System.Security.Cryptography;
using System.Text;

namespace AppService;

public interface IHmacService
{
    string Sign(string data);
}

public class HmacService : IHmacService
{
    private readonly HmacSettings _settings;

    public HmacService(HmacSettings settings)
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
}
