namespace interpreter.Maui.Services;

public interface ILocalStorageService
{
    /// <summary>
    /// Store an object using the type name as the key. Passing null removes the key.
    /// </summary>
    /// <typeparam name="T">Type to store.</typeparam>
    /// <param name="value">Instance to persist or null to remove.</param>
    void Set<T>(T value);

    /// <summary>
    /// Retrieve an object using the type name as the key.
    /// </summary>
    /// <typeparam name="T">Type to retrieve.</typeparam>
    /// <param name="defaultValue">Returned if missing or deserialization fails.</param>
    /// <returns>Stored value or the provided default.</returns>
    T Get<T>(T? defaultValue = default);
}