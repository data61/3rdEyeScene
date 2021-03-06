﻿using System;
using System.IO;
using System.IO.Compression;

namespace Tes.IO
{
  /// <summary>
  /// The <c>StreamReader</c> supports reading and decoding of a data stream into <see cref="T:PacketBuffer"/>
  /// packets.
  /// </summary>
  /// <remarks>
  /// The reader supports reading from a stream and decoding uncompressed <see cref="T:PacketBuffer"/> data,
  /// packets in <see cref="T:CollatedPacket"/> messages, both compressed and uncompressed, and data compressed
  /// using GZip compression.
  ///
  /// The algorithm supports the <see cref="BaseStream"/> being a GZip encoded stream part way through,
  /// separate to the contents of a <see cref="T:CollatedPacket"/>. If the stream is compressed, then it
  /// can no longer seek, but can reset to the beginning, so long as the <see cref="BaseStream"/> supports
  /// seeking.
  ///
  /// Compression of the overall stream and in <see cref="T:CollatedPacket"/> messages is redundant.
  /// </remarks>
  public class PacketStreamReader : Stream
  {
    /// <summary>
    /// Create a Tes stream reader around the given <paramref name="baseStream"/>.
    /// </summary>
    /// <param name="baseStream">The underlying stream to reader from.</param>
    /// <remarks>
    /// The <paramref name="baseStream"/> should generally be a <c>FileStream</c>.
    /// </remarks>
    public PacketStreamReader(Stream baseStream)
    {
      BaseStream = baseStream;
      _activeStream = baseStream;
      EndOfStream = false;
    }

    /// <summary>
    /// Access the underlying <c>Stream</c>.
    /// </summary>
    public Stream BaseStream { get; protected set; }

    /// <summary>
    /// True when the end of the stream has been reached.
    /// </summary>
    public bool EndOfStream { get; protected set; }

    /// <summary>
    /// True while in the middle of decoding a collated packet.
    /// </summary>
    public bool DecodingCollated { get { return _decoder.Decoding; } }
    /// <summary>
    /// <c>false</c>
    /// </summary>
    public override bool CanRead { get { return BaseStream.CanRead; } }

    /// <summary>
    /// <c>false</c>
    /// </summary>
    public override bool CanSeek { get { return BaseStream.CanSeek && !_isGZipStream; } }

    /// <summary>
    /// <c>false</c>
    /// </summary>
    public override bool CanWrite { get { return false; } }

    /// <summary>
    /// Returns the number of bytes written plus the outstanding buffered bytes.
    /// </summary>
    public override long Length { get { return Position; } }

    /// <summary>
    /// Get/set the number of bytes written plus the outstanding buffered bytes.
    /// </summary>
    /// <remarks>
    /// Seeking is only supported for uncompressed streams. Also, it is recommended that the
    /// position only be set to the start of an uncompressed <see cref="T:PacketHeader"/>.
    ///
    /// Setting the position to zero is equivalent to calling <see cref="Reset()"/>.
    /// </remarks>
    public override long Position
    {
      get
      {
        return BaseStream.Position;
      }

      set
      {
        Reset();
        if (value != 0)
        {
          BaseStream.Position = value;
        }
      }
    }

    /// <summary>
    /// Flush the collation buffer and the underlying stream.
    /// </summary>
    public override void Flush()
    {
      BaseStream.Flush();
    }

    /// <summary>
    /// Read directly from the stream. Preferred usage is <see cref="NextPacket(ref long)"/>.
    /// </summary>
    /// <returns>The number of bytes read. Zero at the end of the stream</returns>
    /// <param name="buffer">Buffer to read into.</param>
    /// <param name="offset">Offset into <paramref name="buffer"/> to read into.</param>
    /// <param name="count">Number of bytes to read.</param>
    public override int Read(byte[] buffer, int offset, int count)
    {
      return _activeStream.Read(buffer, offset, count);
    }

    /// <summary>
    /// Seek to position in the stream. See class remarks on seeking support.
    /// </summary>
    /// <returns>The new position in the stream.</returns>
    /// <param name="offset">Byte offset from <paramref name="origin"/> to seek to.</param>
    /// <param name="origin">Location to seek relative to.</param>
    /// <remarks>
    /// Seeking to the beginning of the stream is equivalent to calling <see cref="Reset()"/>.
    /// </remarks>
    public override long Seek(long offset, SeekOrigin origin)
    {
      if (origin == SeekOrigin.Begin)
      {
        Reset(offset);
        return Position;
      }

      Reset(0L);
      return _activeStream.Seek(offset, origin);
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="value">Ignored.</param>
    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <returns>N/A</returns>
    /// <param name="buffer">Ignored.</param>
    /// <param name="offset">Ignored.</param>
    /// <param name="count">Ignored.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Reset to the start of the stream.
    /// </summary>
    public void Reset()
    {
      Reset(0L);
    }

    /// <summary>
    /// Reset the stream to the given seekable position. Provides common code to <see cref="Reset()"/>
    /// and <see cref="Seek(long, SeekOrigin)"/>.
    /// </summary>
    /// <param name="streamPosition">The stream position to set. Must be a valid seekable position
    /// for the current stream.</param>
    /// <remarks>
    /// See class comments on seeking.
    /// </remarks>
    protected void Reset(long streamPosition)
    {
      _activeStream = BaseStream;
      _activeStream.Flush();
      _activeStream.Position = streamPosition;
      _isGZipStream = false;
      EndOfStream = false;
      _decoder.SetPacket(null);
    }

    /// <summary>
    /// Read the next <see cref="T:PacketBuffer"/> from the stream.
    /// </summary>
    /// <returns>The next packet extracted from the stream.</returns>
    /// <param name="processedBytes">Incremented by the number of bytes read from the stream. See remarks.</param>
    /// <remarks>
    /// The next packet is returned first from the current <see cref="T:CollatedPacketDecoder"/> if possible.
    /// Otherwise packet data are read from the stream and decoded as required. The <paramref name="processedBytes"/>
    /// value is only adjusted when data are read from the stream, not when a packet is extracted from the
    /// collation buffer.
    /// </remarks>
    /// <exception cref="MarkerNotFoundException">Thrown when the <see cref="PacketHeader.PacketMarker" /> bytes cannot
    /// be found.</exception>
    /// <exception cref="DecodeException">Thrown when decoding the packet header fails.</exception>
    /// <exception cref="InsufficientDataException">Thrown when the stream does not contain sufficient bytes to
    /// complete a packet once the header has been read or when there are insufficient bytes to read a packet header.
    /// </exception>
    public PacketBuffer NextPacket(ref long processedBytes)
    {
      PacketBuffer packet = null;

      // If decoding, just pull from the decoder.
      if (DecodingCollated)
      {
        packet = _decoder.Next();
        if (packet != null)
        {
          return packet;
        }
      }

      // Check for transition from raw stream to GZip stream.
      if (!_isGZipStream && GZipUtil.IsGZipStream(_activeStream))
      {
        _isGZipStream = true;
        _activeStream = new GZipStream(_activeStream, CompressionMode.Decompress);
      }

      // Read next packet.
      int bytesRead = 0;
      _headerStream.Position = 0;
      _headerStream.SetLength(PacketHeader.Size);

      if (!_searchForHeader)
      {
        bytesRead = _activeStream.Read(_headerStream.GetBuffer(), 0, PacketHeader.Size);
      }
      else
      {
        _searchForHeader = false;
        // Look for the marker bytes.
        UInt32 marker = 0;
        int markerSize = 4;
        while (!EndOfStream && marker != PacketHeader.PacketMarker)
        {
          bytesRead = _activeStream.Read(_headerStream.GetBuffer(), 0, markerSize);
          marker = Endian.FromNetwork(BitConverter.ToUInt32(_headerStream.GetBuffer(), 0));
        }

        if (marker != PacketHeader.PacketMarker)
        {
          // Failed.
          EndOfStream = true;
          throw new MarkerNotFoundException("Packet marker not found in stream");
        }

        bytesRead += _activeStream.Read(_headerStream.GetBuffer(), markerSize, PacketHeader.Size - markerSize);
      }

      if (bytesRead == 0 && (_decoder == null || !_decoder.Decoding))
      {
        // Nothing more read and fully up to date.
        EndOfStream = true;
        return null;
      }

      if (bytesRead < PacketHeader.Size)
      {
        EndOfStream = true;
        throw new InsufficientDataException("Insufficient data in stream to complete packet");
      }

      _headerStream.SetLength(bytesRead);

      // Decode header.
      if (!_header.Read(new NetworkReader(_headerStream)))
      {
        // TODO: Throw exception
        _searchForHeader = true;
        throw new DecodeException("Packet header decoding failure");
      }

      // Extract packet.
      int crcSize = ((_header.Flags & (byte)PacketFlag.NoCrc) == 0) ? Crc16.CrcSize : 0;
      packet = new PacketBuffer(_header.PacketSize + crcSize);
      packet.Emplace(_headerStream.GetBuffer(), bytesRead);
      processedBytes += packet.Emplace(_activeStream, _header.PacketSize + crcSize - bytesRead);
      if (packet.Status != PacketBufferStatus.Complete)
      {
        _searchForHeader = true;
        if (packet.Status == PacketBufferStatus.CrcError)
        {
          throw new CrcFailureException("Invalid CRC.");
        }
        throw new InsufficientDataException("Incomplete packet");
      }

      // Decoder packet.
      _decoder.SetPacket(packet);
      packet = _decoder.Next();
      return packet;
    }

    private CollatedPacketDecoder _decoder = new CollatedPacketDecoder();
    /// <summary>
    /// The active stream may either match the <see cref="BaseStream"/> or differ when decoding
    /// GZip compression. This object is used to actually read.
    /// </summary>
    private Stream _activeStream = null;
    private MemoryStream _headerStream = new MemoryStream(PacketHeader.Size);
    private PacketHeader _header = new PacketHeader();
    private bool _isGZipStream = false;
    /// <summary>
    /// True when we need to search for a header. Occurs after an invalid header or packet completion failure.
    /// </summary>
    private bool _searchForHeader = false;
  }
}
