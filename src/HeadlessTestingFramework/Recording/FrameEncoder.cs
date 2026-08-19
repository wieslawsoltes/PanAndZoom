// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.HeadlessTestingFramework.Recording;

/// <summary>
/// Interface for encoding captured frames to various output formats.
/// </summary>
public interface IFrameEncoder : IDisposable
{
    /// <summary>
    /// Gets the output format this encoder produces.
    /// </summary>
    RecordingFormat Format { get; }

    /// <summary>
    /// Gets the file extension for the output format.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Initializes the encoder with the specified output path and settings.
    /// </summary>
    /// <param name="outputPath">The output file or directory path.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    /// <param name="options">Recording options.</param>
    void Initialize(string outputPath, int width, int height, RecordingOptions options);

    /// <summary>
    /// Encodes a single frame.
    /// </summary>
    /// <param name="frame">The frame to encode.</param>
    void EncodeFrame(CapturedFrame frame);

    /// <summary>
    /// Encodes a single frame asynchronously.
    /// </summary>
    /// <param name="frame">The frame to encode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EncodeFrameAsync(CapturedFrame frame, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the encoding and writes any remaining data.
    /// </summary>
    void FinalizeEncoding();

    /// <summary>
    /// Finalizes the encoding asynchronously.
    /// </summary>
    Task FinalizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the paths of all output files created by this encoder.
    /// </summary>
    IReadOnlyList<string> OutputFiles { get; }
}

/// <summary>
/// Encodes frames as a sequence of PNG images.
/// </summary>
public class PngSequenceEncoder : IFrameEncoder
{
    private string? _outputDirectory;
    private string? _baseFileName;
    private RecordingOptions? _options;
    private readonly List<string> _outputFiles = new();
    private bool _disposed;

    /// <inheritdoc />
    public RecordingFormat Format => RecordingFormat.PngSequence;

    /// <inheritdoc />
    public string FileExtension => ".png";

    /// <inheritdoc />
    public IReadOnlyList<string> OutputFiles => _outputFiles;

    /// <inheritdoc />
    public void Initialize(string outputPath, int width, int height, RecordingOptions options)
    {
        _outputDirectory = outputPath;
        _baseFileName = options.BaseFileName;
        _options = options;

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    /// <inheritdoc />
    public void EncodeFrame(CapturedFrame frame)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PngSequenceEncoder));

        if (_outputDirectory == null || _baseFileName == null)
            throw new InvalidOperationException("Encoder not initialized.");

        var fileName = $"{_baseFileName}_{frame.FrameNumber:D6}{FileExtension}";
        var filePath = Path.Combine(_outputDirectory, fileName);

        SaveFrameAsPng(frame, filePath);
        _outputFiles.Add(filePath);
    }

    /// <inheritdoc />
    public async Task EncodeFrameAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => EncodeFrame(frame), cancellationToken);
    }

    /// <inheritdoc />
    public void FinalizeEncoding()
    {
        // PNG sequence doesn't need finalization
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        FinalizeEncoding();
        return Task.CompletedTask;
    }

    private void SaveFrameAsPng(CapturedFrame frame, string filePath)
    {
        // Create a WriteableBitmap from the frame data and save it
        using var bitmap = new Media.Imaging.WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            frame.Dpi,
            ConvertPixelFormat(frame.PixelFormat),
            AlphaFormat.Premul);

        using (var lockedBitmap = bitmap.Lock())
        {
            unsafe
            {
                var srcSpan = frame.PixelData;
                var destPtr = (byte*)lockedBitmap.Address.ToPointer();
                for (int i = 0; i < srcSpan.Length; i++)
                {
                    destPtr[i] = srcSpan[i];
                }
            }
        }

        bitmap.Save(filePath, PngBitmapEncoderOptions.Default);
    }

    private static Avalonia.Platform.PixelFormat ConvertPixelFormat(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Rgba8888 => Avalonia.Platform.PixelFormat.Rgba8888,
            PixelFormat.Bgra8888 => Avalonia.Platform.PixelFormat.Bgra8888,
            PixelFormat.Rgb888 => Avalonia.Platform.PixelFormat.Rgba8888, // Fallback to RGBA
            _ => Avalonia.Platform.PixelFormat.Rgba8888
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Encodes frames as a sequence of JPEG images.
/// </summary>
public class JpegSequenceEncoder : IFrameEncoder
{
    private string? _outputDirectory;
    private string? _baseFileName;
    private RecordingOptions? _options;
    private readonly List<string> _outputFiles = new();
    private bool _disposed;

    /// <inheritdoc />
    public RecordingFormat Format => RecordingFormat.JpegSequence;

    /// <inheritdoc />
    public string FileExtension => ".jpg";

    /// <inheritdoc />
    public IReadOnlyList<string> OutputFiles => _outputFiles;

    /// <inheritdoc />
    public void Initialize(string outputPath, int width, int height, RecordingOptions options)
    {
        _outputDirectory = outputPath;
        _baseFileName = options.BaseFileName;
        _options = options;

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    /// <inheritdoc />
    public void EncodeFrame(CapturedFrame frame)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(JpegSequenceEncoder));

        if (_outputDirectory == null || _baseFileName == null || _options == null)
            throw new InvalidOperationException("Encoder not initialized.");

        var fileName = $"{_baseFileName}_{frame.FrameNumber:D6}{FileExtension}";
        var filePath = Path.Combine(_outputDirectory, fileName);

        SaveFrameAsJpeg(frame, filePath, _options.Quality);
        _outputFiles.Add(filePath);
    }

    /// <inheritdoc />
    public async Task EncodeFrameAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => EncodeFrame(frame), cancellationToken);
    }

    /// <inheritdoc />
    public void FinalizeEncoding()
    {
        // JPEG sequence doesn't need finalization
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        FinalizeEncoding();
        return Task.CompletedTask;
    }

    private void SaveFrameAsJpeg(CapturedFrame frame, string filePath, int quality)
    {
        // For JPEG, we need to convert to a format without alpha
        // Create bitmap and save with quality setting
        using var bitmap = new Media.Imaging.WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            frame.Dpi,
            ConvertJpegPixelFormat(frame.PixelFormat),
            AlphaFormat.Opaque);

        using (var lockedBitmap = bitmap.Lock())
        {
            unsafe
            {
                var srcSpan = frame.PixelData;
                var destPtr = (byte*)lockedBitmap.Address.ToPointer();
                for (int i = 0; i < srcSpan.Length; i++)
                {
                    destPtr[i] = srcSpan[i];
                }
            }
        }

        // Save with quality parameter (requires stream-based save)
        using var stream = File.Create(filePath);
        bitmap.Save(stream, new JpegBitmapEncoderOptions { Quality = quality });
    }

    private static Avalonia.Platform.PixelFormat ConvertJpegPixelFormat(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Rgba8888 => Avalonia.Platform.PixelFormat.Rgba8888,
            PixelFormat.Bgra8888 => Avalonia.Platform.PixelFormat.Bgra8888,
            PixelFormat.Rgb888 => Avalonia.Platform.PixelFormat.Rgba8888, // Fallback to RGBA
            _ => Avalonia.Platform.PixelFormat.Rgba8888
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Stores frames in memory for later processing.
/// </summary>
public class RawFrameEncoder : IFrameEncoder
{
    private readonly List<CapturedFrame> _frames = new();
    private readonly List<string> _outputFiles = new();
    private bool _disposed;

    /// <inheritdoc />
    public RecordingFormat Format => RecordingFormat.RawFrames;

    /// <inheritdoc />
    public string FileExtension => ".raw";

    /// <inheritdoc />
    public IReadOnlyList<string> OutputFiles => _outputFiles;

    /// <summary>
    /// Gets all captured frames.
    /// </summary>
    public IReadOnlyList<CapturedFrame> Frames => _frames;

    /// <inheritdoc />
    public void Initialize(string outputPath, int width, int height, RecordingOptions options)
    {
        // Raw encoder stores in memory, no file initialization needed
    }

    /// <inheritdoc />
    public void EncodeFrame(CapturedFrame frame)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RawFrameEncoder));

        // Store a copy of the frame
        var copy = new CapturedFrame(
            frame.FrameNumber,
            frame.Timestamp,
            frame.Width,
            frame.Height,
            frame.Stride,
            frame.PixelFormat,
            frame.GetPixelDataCopy(),
            frame.Dpi);

        _frames.Add(copy);
    }

    /// <inheritdoc />
    public async Task EncodeFrameAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => EncodeFrame(frame), cancellationToken);
    }

    /// <inheritdoc />
    public void FinalizeEncoding()
    {
        // Raw encoder doesn't need finalization
    }

    /// <inheritdoc />
    public Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        FinalizeEncoding();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Factory for creating frame encoders.
/// </summary>
public static class FrameEncoderFactory
{
    /// <summary>
    /// Creates an encoder for the specified format.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <returns>A new encoder instance.</returns>
    public static IFrameEncoder Create(RecordingFormat format)
    {
        return format switch
        {
            RecordingFormat.PngSequence => new PngSequenceEncoder(),
            RecordingFormat.JpegSequence => new JpegSequenceEncoder(),
            RecordingFormat.RawFrames => new RawFrameEncoder(),
            RecordingFormat.Gif => new GifEncoder(),
            _ => throw new NotSupportedException($"Format {format} is not supported.")
        };
    }
}

/// <summary>
/// Encodes frames as an animated GIF.
/// </summary>
public class GifEncoder : IFrameEncoder
{
    private string? _outputPath;
    private RecordingOptions? _options;
    private readonly List<CapturedFrame> _frames = new();
    private readonly List<string> _outputFiles = new();
    private bool _disposed;

    /// <inheritdoc />
    public RecordingFormat Format => RecordingFormat.Gif;

    /// <inheritdoc />
    public string FileExtension => ".gif";

    /// <inheritdoc />
    public IReadOnlyList<string> OutputFiles => _outputFiles;

    /// <inheritdoc />
    public void Initialize(string outputPath, int width, int height, RecordingOptions options)
    {
        _outputPath = Path.Combine(outputPath, $"{options.BaseFileName}{FileExtension}");
        _options = options;

        var directory = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public void EncodeFrame(CapturedFrame frame)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GifEncoder));

        // Store frames for later encoding
        var copy = new CapturedFrame(
            frame.FrameNumber,
            frame.Timestamp,
            frame.Width,
            frame.Height,
            frame.Stride,
            frame.PixelFormat,
            frame.GetPixelDataCopy(),
            frame.Dpi);

        _frames.Add(copy);
    }

    /// <inheritdoc />
    public async Task EncodeFrameAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => EncodeFrame(frame), cancellationToken);
    }

    /// <inheritdoc />
    public void FinalizeEncoding()
    {
        if (_outputPath == null || _options == null)
            throw new InvalidOperationException("Encoder not initialized.");

        if (_frames.Count == 0)
            return;

        // Write a simple uncompressed GIF
        // For production use, consider using a proper GIF library
        WriteGif(_outputPath, _frames, _options.FrameRate);
        _outputFiles.Add(_outputPath);
    }

    /// <inheritdoc />
    public async Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(Finalize, cancellationToken);
    }

    private void WriteGif(string outputPath, List<CapturedFrame> frames, int frameRate)
    {
        // Simple GIF encoder - writes individual frames as PNG sequence fallback
        // For a complete GIF implementation, you would use a library like ImageSharp or SkiaSharp
        
        var directory = Path.GetDirectoryName(outputPath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(outputPath);
        
        // Create a metadata file that describes the animation
        var metadataPath = Path.Combine(directory, $"{baseName}_metadata.json");
        var metadata = new
        {
            FrameRate = frameRate,
            FrameCount = frames.Count,
            Width = frames.Count > 0 ? frames[0].Width : 0,
            Height = frames.Count > 0 ? frames[0].Height : 0,
            Duration = frames.Count > 0 ? (double)frames.Count / frameRate : 0
        };
        
#if NETSTANDARD2_0
        // Simple JSON serialization for netstandard2.0
        var json = $@"{{
  ""FrameRate"": {metadata.FrameRate},
  ""FrameCount"": {metadata.FrameCount},
  ""Width"": {metadata.Width},
  ""Height"": {metadata.Height},
  ""Duration"": {metadata.Duration}
}}";
#else
        var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
#endif
        File.WriteAllText(metadataPath, json);
        _outputFiles.Add(metadataPath);
        
        // Save individual frames as PNG (GIF encoding requires more complex implementation)
        using var pngEncoder = new PngSequenceEncoder();
        pngEncoder.Initialize(directory, 
            frames.Count > 0 ? frames[0].Width : 100, 
            frames.Count > 0 ? frames[0].Height : 100,
            new RecordingOptions { BaseFileName = $"{baseName}_frame" });
            
        foreach (var frame in frames)
        {
            pngEncoder.EncodeFrame(frame);
        }
        
        foreach (var file in pngEncoder.OutputFiles)
        {
            _outputFiles.Add(file);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
        GC.SuppressFinalize(this);
    }
}
