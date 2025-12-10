using System.Text.Json;
using Microsoft.Maui.Storage;

namespace interpreter.Maui.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Persist an object in the platform key-value store. Type name is used as the key.
    /// </summary>
    public void Set<T>(T value)
    {
        var key = ResolveKey<T>();

        if (value is null)
        {
            Preferences.Remove(key);
            return;
        }

        var serialized = JsonSerializer.Serialize(value, _serializerOptions);
        Preferences.Set(key, serialized);
    }

    /// <summary>
    /// Retrieve an object from the platform key-value store.
    /// If missing/invalid, persists <paramref name="defaultValue"/> and returns it.
    /// Type name is used as the key.
    /// </summary>
    public T Get<T>(T? defaultValue = default)
    {
        var key = ResolveKey<T>();

        if (!Preferences.ContainsKey(key))
        {
            Set(defaultValue);
            return defaultValue;
        }

        var serialized = Preferences.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            Set(defaultValue);
            return defaultValue;
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(serialized, _serializerOptions);
            if (result is null)
            {
                Set(defaultValue);
                return defaultValue;
            }

            return result;
        }
        catch
        {
            // If deserialization fails, fall back to the provided default and persist it.
            Set(defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Build a stable key based on the object type when an explicit key is not provided.
    /// Uses FullName to reduce collisions across namespaces.
    /// </summary>
    private static string ResolveKey<T>(string? key = null)
    {
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        return typeof(T).FullName ?? typeof(T).Name;
    }

    /// <summary>
    /// Remove a stored value. Type name is used as the key.
    /// </summary>
    private void Remove<T>() => Preferences.Remove(ResolveKey<T>());
}