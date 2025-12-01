namespace Guetta.App;

using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class BufferedPipeStream : Stream
{
    // --- Configuration ---
    private const int DefaultCapacity = 1_000; // Max chunks in memory (Backpressure)
    private readonly int _preBufferBytes;      // How many bytes to wait for before playing (Anti-Stutter)

    // --- State ---
    private readonly Channel<byte[]> _channel;
    private readonly TaskCompletionSource<bool> _preBufferSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _totalBytesWritten = 0;
    
    // Internal read state
    private byte[]? _currentChunk;
    private int _currentChunkOffset;

    public BufferedPipeStream(int preBufferBytes = 192 * 1024) // Default ~1 sec of stereo audio
    {
        _preBufferBytes = preBufferBytes;

        // Your existing capacity logic
        int capacity = int.TryParse(Environment.GetEnvironmentVariable("DISCORD_W_CAPACITY"), out var size) 
            ? size 
            : DefaultCapacity;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait, // Key: Pauses download if memory is full
            SingleWriter = true,
            SingleReader = true
        };

        _channel = Channel.CreateBounded<byte[]>(options);
    }

    // --- Write Implementation (The Web/CLI Tool side) ---
    public override bool CanWrite => true;

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Convert sync write to async to push to channel
        WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // Create a copy of the specific segment to store in the channel
        // We must copy because the caller might reuse 'buffer'
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);

        await _channel.Writer.WriteAsync(chunk, cancellationToken);
        
        CheckPreBufferStatus(count);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        // Optimization: If you can use ReadOnlyMemory natively, do so. 
        // But assuming Channel<byte[]>, we convert here.
        var chunk = buffer.ToArray();
        
        // We must await the write to respect backpressure
        var writeTask = _channel.Writer.WriteAsync(chunk, cancellationToken);
        
        if (writeTask.IsCompletedSuccessfully)
        {
            CheckPreBufferStatus(chunk.Length);
            return default;
        }

        return new ValueTask(FinishWriteAsync(writeTask, chunk.Length));
    }

    private async Task FinishWriteAsync(ValueTask writeTask, int length)
    {
        await writeTask;
        CheckPreBufferStatus(length);
    }

    private void CheckPreBufferStatus(int bytesAdded)
    {
        var total = Interlocked.Add(ref _totalBytesWritten, bytesAdded);
        
        // If we crossed the threshold, tell the Reader it can start
        if (total >= _preBufferBytes)
        {
            _preBufferSignal.TrySetResult(true);
        }
    }

    public void Complete() // Call this when download finishes
    {
        _channel.Writer.TryComplete();
        // Ensure reader doesn't hang if file was smaller than pre-buffer
        _preBufferSignal.TrySetResult(true); 
    }

    // --- Read Implementation (The Discord side) ---
    public override bool CanRead => true;
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // 1. Wait for Pre-Buffer (Anti-Stutter)
        // This will pause execution here until 'Write' has pushed enough bytes
        await _preBufferSignal.Task;

        int bytesCopied = 0;

        while (bytesCopied < count)
        {
            // If we don't have a current chunk, try to get one
            if (_currentChunk == null)
            {
                // If channel is empty and writer is done -> End of Stream
                if (await _channel.Reader.WaitToReadAsync(cancellationToken) == false)
                {
                    return bytesCopied; 
                }

                if (_channel.Reader.TryRead(out byte[]? nextChunk))
                {
                    _currentChunk = nextChunk;
                    _currentChunkOffset = 0;
                }
            }

            if (_currentChunk != null)
            {
                int bytesLeftInChunk = _currentChunk.Length - _currentChunkOffset;
                int bytesNeeded = count - bytesCopied;
                int toCopy = Math.Min(bytesLeftInChunk, bytesNeeded);

                Array.Copy(_currentChunk, _currentChunkOffset, buffer, offset + bytesCopied, toCopy);

                bytesCopied += toCopy;
                _currentChunkOffset += toCopy;

                if (_currentChunkOffset >= _currentChunk.Length)
                {
                    _currentChunk = null; // Chunk consumed
                }
            }
        }

        return bytesCopied;
    }

    // --- Boilerplate ---
    public override bool CanSeek => false;
    public override long Length => _totalBytesWritten; // Not strictly accurate for a pipe, but useful debug
    public override long Position { get => 0; set => throw new NotSupportedException(); }
    
    public ChannelReader<byte[]> Reader => _channel.Reader;

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    
    // Dispose pattern to close the channel
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _channel.Writer.TryComplete();
        }
    }
}