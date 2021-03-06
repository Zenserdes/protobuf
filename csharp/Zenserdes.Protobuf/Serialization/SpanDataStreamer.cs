﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Zenserdes.Protobuf.Serialization
{
	public ref struct SpanDataStreamer<TBufferWriter>
		where TBufferWriter : IBufferWriter<byte>
	{
		public readonly ReadOnlySpan<byte> ReadOnlySpan;

		// *not* marked as readonly, incase it is a mutating struct
		public TBufferWriter BufferWriter;

		public int Position;

		public SpanDataStreamer(ReadOnlySpan<byte> span, TBufferWriter bufferWriter)
		{
			ReadOnlySpan = span;
			BufferWriter = bufferWriter;
			Position = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Next(ref byte data)
		{
			if (Position < ReadOnlySpan.Length)
			{
				data = ReadOnlySpan[Position++];
				return true;
			}

			return false;
		}


		public ReadOnlySpan<byte> MaybeReadWorkaround(int max)
			=> ReadOnlySpan.Slice(Position, Math.Min(ReadOnlySpan.Length - Position, max));

		/// <summary>
		/// Only used for varint decoding. This isn't very useful in any other situation,
		/// because there is no way to tell how many bytes were copied.
		/// </summary>
		/// <param name="target"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void MaybeRead(Span<byte> target)
		{
			ReadOnlySpan.Slice(Position).CopyTo(target);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Advance(int bytes)
		{
			Position += bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ReadPermanent(int bytes, out ReadOnlyMemory<byte> memory)
		{
			if (Position + bytes <= ReadOnlySpan.Length)
			{
				var target = BufferWriter.GetMemory(bytes).Slice(0, bytes);
				BufferWriter.Advance(bytes);

				ReadOnlySpan.Slice(Position, bytes).CopyTo(target.Span);
				Position += bytes;
				memory = target;
				return true;
			}

			memory = default;
			return false;
		}
	}
}