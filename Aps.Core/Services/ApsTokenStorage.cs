using System;
using System.IO;
using Aps.Core.Interfaces;
using Aps.Core.Models;
using Newtonsoft.Json;

namespace Aps.Core.Services;

/// <summary>
/// File-based implementation of token storage.
/// </summary>
public class ApsTokenStorage : ITokenStorage
{
    private readonly string _tokenFile;

    public ApsTokenStorage()
    {
        _tokenFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "COBIeManager", "aps_token.json");
    }

    /// <summary>
    /// Saves the authentication token to local storage.
    /// </summary>
    public void SaveToken(string accessToken, string refreshToken, DateTime expiresAt)
    {
        var folder = Path.GetDirectoryName(_tokenFile);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var token = new StoredTokenModel
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        var json = JsonConvert.SerializeObject(token, Formatting.Indented);
        File.WriteAllText(_tokenFile, json);
    }

    /// <summary>
    /// Retrieves the stored authentication token.
    /// </summary>
    public (string accessToken, string refreshToken, DateTime expiresAt)? GetToken()
    {
        if (!File.Exists(_tokenFile))
            return null;

        var json = File.ReadAllText(_tokenFile);
        var token = JsonConvert.DeserializeObject<StoredTokenModel>(json);

        if (token == null)
            return null;

        return (token.AccessToken ?? string.Empty, token.RefreshToken ?? string.Empty, token.ExpiresAt);
    }

    /// <summary>
    /// Clears any stored tokens.
    /// </summary>
    public void ClearToken()
    {
        if (File.Exists(_tokenFile))
            File.Delete(_tokenFile);
    }
}
