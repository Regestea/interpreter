using Opus.Services;

namespace interpreter.Api.Services;

/// <summary>
/// Service for managing voice files in WAV format
/// </summary>
public class VoiceFileService : IVoiceFileService
{
    private readonly IOpusCodecService _opusCodecService;
    private readonly ILogger<VoiceFileService> _logger;
    private readonly string _voiceFilesDirectory;
    private const string FileExtension = ".wav";

    public VoiceFileService(
        IOpusCodecService opusCodecService,
        ILogger<VoiceFileService> logger)
    {
        _opusCodecService = opusCodecService;
        _logger = logger;
        
        // Create VoiceFiles folder next to project files
        _voiceFilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "VoiceFiles");
        
        // Ensure directory exists
        if (!Directory.Exists(_voiceFilesDirectory))
        {
            Directory.CreateDirectory(_voiceFilesDirectory);
            _logger.LogInformation("Created voice files directory: {Directory}", _voiceFilesDirectory);
        }
    }

    public async Task<string> SaveVoiceFileAsync(string name, Stream opusStream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (opusStream == null)
            throw new ArgumentNullException(nameof(opusStream));

        try
        {
            // Decode OPUS to WAV
            _logger.LogInformation("Decoding OPUS stream for voice file: {Name}", name);
            var wavStream = await _opusCodecService.DecodeAsync(opusStream, cancellationToken);

            // Save to file
            var filePath = GetFilePath(name);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            wavStream.Position = 0;
            await wavStream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Voice file saved successfully: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving voice file: {Name}", name);
            throw;
        }
    }

    public Task<List<string>> GetVoiceFileNamesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_voiceFilesDirectory, $"*{FileExtension}")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToList();

            _logger.LogInformation("Found {Count} voice files", files.Count);
            return Task.FromResult(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice file names");
            throw;
        }
    }

    public Task<Stream?> GetVoiceFileStreamAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        try
        {
            var filePath = GetFilePath(name);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Voice file not found: {Name}", name);
                return Task.FromResult<Stream?>(null);
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogInformation("Retrieved voice file stream: {Name}", name);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice file stream: {Name}", name);
            throw;
        }
    }

    public Task<bool> DeleteVoiceFileAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        try
        {
            var filePath = GetFilePath(name);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Voice file not found for deletion: {Name}", name);
                return Task.FromResult(false);
            }

            File.Delete(filePath);
            _logger.LogInformation("Voice file deleted: {Name}", name);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting voice file: {Name}", name);
            throw;
        }
    }

    public Task<bool> VoiceFileExistsAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var filePath = GetFilePath(name);
        return Task.FromResult(File.Exists(filePath));
    }

    private string GetFilePath(string name)
    {
        // Sanitize the filename to prevent directory traversal
        var sanitizedName = Path.GetFileNameWithoutExtension(name);
        return Path.Combine(_voiceFilesDirectory, $"{sanitizedName}{FileExtension}");
    }
}

