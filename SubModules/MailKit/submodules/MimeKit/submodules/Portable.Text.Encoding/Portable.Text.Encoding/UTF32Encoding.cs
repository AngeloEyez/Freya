/*
 * UTF32Encoding.cs - Implementation of the
 *		"System.Text.UTF32Encoding" class.
 *
 * Author: Atsushi Enomoto <atsushi@ximian.com>
 *
 * Copyright (C) 2005 Novell, Inc.  http://www.novell.com
 *
 * The basic part (now almost nothing) is copied from UnicodeEncoding.cs.
 * Original copyrights goes here:
 *
 * Copyright (c) 2001, 2002  Southern Storm Software, Pty Ltd
 * Copyright (C) 2003, 2004 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;

namespace Portable.Text
{
	public sealed class UTF32Encoding : Encoding
	{
		// Magic numbers used by Windows for UTF32.
		internal const int UTF32_CODE_PAGE     = 12000;
		internal const int BIG_UTF32_CODE_PAGE = 12001;

		// Internal state.
		readonly bool byteOrderMark;
		readonly bool bigEndian;

		// Constructors.
		public UTF32Encoding () : this (false, true, false)
		{
		}

		public UTF32Encoding (bool bigEndian, bool byteOrderMark) : this (bigEndian, byteOrderMark, false)
		{
		}

		public UTF32Encoding (bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters) : base ((bigEndian ? BIG_UTF32_CODE_PAGE : UTF32_CODE_PAGE))
		{
			this.byteOrderMark = byteOrderMark;
			this.bigEndian = bigEndian;

			if (throwOnInvalidCharacters)
				SetFallbackInternal (EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
			else
				SetFallbackInternal (new EncoderReplacementFallback ("\uFFFD"), new DecoderReplacementFallback ("\uFFFD"));

			if (bigEndian) {
				body_name = "utf-32BE";
				encoding_name = "UTF-32 (Big-Endian)";
				header_name = "utf-32BE";
				web_name = "utf-32BE";
			} else {
				body_name = "utf-32";
				encoding_name = "UTF-32";
				header_name = "utf-32";
				web_name = "utf-32";
			}

			// Windows reports the same code page number for
			// both the little-endian and big-endian forms.
			windows_code_page = UTF32_CODE_PAGE;
		}

		// Get the number of bytes needed to encode a character buffer.
		public override int GetByteCount (char[] chars, int index, int count)
		{
			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (index < 0 || index > chars.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || count > (chars.Length - index))
				throw new ArgumentOutOfRangeException ("count");

			int ret = 0;
			for (int i = index; i < index + count; i++) {
				if (Char.IsSurrogate (chars [i])) {
					if (i + 1 < chars.Length && Char.IsSurrogate (chars [i + 1])) {
						ret += 4;
					} else {
						// FIXME: handle fallback
						//ret += DecoderFallback.MaxCharCount;
						ret += 4;
					}
				} else {
					ret += 4;
				}
			}

			return ret;
		}

		// Get the bytes that result from encoding a character buffer.
		public override int GetBytes (char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (charIndex < 0 || charIndex > chars.Length)
				throw new ArgumentOutOfRangeException ("charIndex");

			if (charCount < 0 || charCount > (chars.Length - charIndex))
				throw new ArgumentOutOfRangeException ("charCount");

			if (byteIndex < 0 || byteIndex > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteIndex");

			if ((bytes.Length - byteIndex) < (charCount * 4))
				throw new ArgumentException ("Insufficient space available.");

			int posn = byteIndex;
			char ch;

			while (charCount-- > 0) {
				ch = chars[charIndex++];
				if (Char.IsSurrogate (ch)) {
					if (charCount-- > 0) {
						int value = 0x400 * (ch - 0xD800) + 0x10000 + chars [charIndex++] - 0xDC00;
						if (bigEndian) {
							for (int i = 0; i < 4; i++) {
								bytes [posn + 3 - i] = (byte) (value % 0x100);
								value >>= 8;
							}
							posn += 4;
						} else {
							for (int i = 0; i < 4; i++) {
								bytes [posn++] = (byte) (value % 0x100);
								value >>= 8;
							}
						}
					} else {
						// Illegal surrogate
						// FIXME: use fallback
						if (bigEndian) {
							bytes[posn++] = 0;
							bytes[posn++] = 0;
							bytes[posn++] = 0;
							bytes[posn++] = (byte) '?';
						} else {
							bytes[posn++] = (byte) '?';
							bytes[posn++] = 0;
							bytes[posn++] = 0;
							bytes[posn++] = 0;
						}
					}
				} else {
					if (bigEndian) {
						bytes[posn++] = 0;
						bytes[posn++] = 0;
						bytes[posn++] = (byte) (ch >> 8);
						bytes[posn++] = (byte) ch;
					} else {
						bytes[posn++] = (byte) ch;
						bytes[posn++] = (byte) (ch >> 8);
						bytes[posn++] = 0;
						bytes[posn++] = 0;
					}
				}
			}

			return posn - byteIndex;
		}

		// Get the number of characters needed to decode a byte buffer.
		public override int GetCharCount (byte[] bytes, int index, int count)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (index < 0 || index > bytes.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || count > (bytes.Length - index))
				throw new ArgumentOutOfRangeException ("count");

			return count / 4;
		}

		// Get the characters that result from decoding a byte buffer.
		public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (byteIndex < 0 || byteIndex > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteIndex");

			if (byteCount < 0 || byteCount > (bytes.Length - byteIndex))
				throw new ArgumentOutOfRangeException ("byteCount");

			if (charIndex < 0 || charIndex > chars.Length)
				throw new ArgumentOutOfRangeException ("charIndex");

			/*
			// Determine the byte order in the incoming buffer.
			bool isBigEndian;
			if (byteCount >= 2) {
				if (bytes[byteIndex] == (byte)0xFE && bytes[byteIndex + 1] == (byte)0xFF) {
					isBigEndian = true;
				} else if (bytes[byteIndex] == (byte)0xFF && bytes[byteIndex + 1] == (byte)0xFE) {
					isBigEndian = false;
				} else {
					isBigEndian = bigEndian;
				}
			} else {
				isBigEndian = bigEndian;
			}
			*/

			// Validate that we have sufficient space in "chars".
			if ((chars.Length - charIndex) < (byteCount / 4))
				throw new ArgumentException ("Insufficient space available.");

			// Convert the characters.
			int posn = charIndex;
			if (bigEndian) {
				while (byteCount >= 4) {
					chars[posn++] = (char) (bytes[byteIndex] << 24 | bytes[byteIndex + 1] << 16 | bytes[byteIndex + 2] << 8 | bytes[byteIndex + 3]);
					byteIndex += 4;
					byteCount -= 4;
				}
			} else {
				while (byteCount >= 4) {
					chars[posn++] = (char) (bytes[byteIndex] | bytes[byteIndex + 1] << 8 | bytes[byteIndex + 2] << 16 | bytes[byteIndex + 3] << 24);
					byteIndex += 4;
					byteCount -= 4;
				}
			}

			return posn - charIndex;
		}

		// Get the maximum number of bytes needed to encode a
		// specified number of characters.
		public override int GetMaxByteCount (int charCount)
		{
			if (charCount < 0)
				throw new ArgumentOutOfRangeException ("charCount");

			return charCount * 4;
		}

		// Get the maximum number of characters needed to decode a
		// specified number of bytes.
		public override int GetMaxCharCount (int byteCount)
		{
			if (byteCount < 0)
				throw new ArgumentOutOfRangeException ("byteCount");

			return byteCount / 4;
		}

		// Get a UTF32-specific decoder that is attached to this instance.
		public override
		#if STANDALONE
		Decoder
		#else
		System.Text.Decoder
		#endif
		GetDecoder ()
		{
			return new UTF32Decoder (bigEndian);
		}

		// Get the UTF32 preamble.
		public override byte[] GetPreamble ()
		{
			if (byteOrderMark) {
				var preamble = new byte[4];

				if (bigEndian) {
					preamble[2] = (byte) 0xFE;
					preamble[3] = (byte) 0xFF;
				} else {
					preamble[0] = (byte) 0xFF;
					preamble[1] = (byte) 0xFE;
				}
				return preamble;
			}

			return new byte[0];
		}

		// Determine if this object is equal to another.
		public override bool Equals (Object obj)
		{
			var encoding = obj as UTF32Encoding;

			return encoding != null && codePage == encoding.codePage && bigEndian == encoding.bigEndian && byteOrderMark == encoding.byteOrderMark && base.Equals (obj);
		}

		// Get the hash code for this object.
		public override int GetHashCode ()
		{
			int basis = base.GetHashCode ();

			if (bigEndian)
				basis ^= 0x1F;

			if (byteOrderMark)
				basis ^= 0x3F;

			return basis;
		}

		// UTF32 decoder implementation.
		sealed class UTF32Decoder : Decoder
		{
			readonly bool bigEndian;
			int leftOverLength;
			int leftOverByte;

			// Constructor.
			public UTF32Decoder (bool bigEndian)
			{
				this.bigEndian = bigEndian;
				leftOverByte = -1;
			}

			// Override inherited methods.
			public override int GetCharCount (byte[] bytes, int index, int count)
			{
				if (bytes == null)
					throw new ArgumentNullException ("bytes");

				if (index < 0 || index > bytes.Length)
					throw new ArgumentOutOfRangeException ("index");

				if (count < 0 || count > (bytes.Length - index))
					throw new ArgumentOutOfRangeException ("count");

				if (leftOverByte != -1)
					return (count + 1) / 4;

				return count / 4;
			}

			public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			{
				if (bytes == null)
					throw new ArgumentNullException ("bytes");

				if (chars == null)
					throw new ArgumentNullException ("chars");

				if (byteIndex < 0 || byteIndex > bytes.Length)
					throw new ArgumentOutOfRangeException ("byteIndex");

				if (byteCount < 0 || byteCount > (bytes.Length - byteIndex))
					throw new ArgumentOutOfRangeException ("byteCount");

				if (charIndex < 0 || charIndex > chars.Length)
					throw new ArgumentOutOfRangeException ("charIndex");

				// Convert the characters.
				int leftOver = leftOverByte;
				int length = chars.Length;
				int posn = charIndex;
				char ch;

				int remain = 4 - leftOverLength;
				if (leftOverLength > 0 && byteCount > remain) {
					if (bigEndian) {
						for (int i = 0; i < remain; i++)
							leftOver += bytes [byteIndex++] << (4 - byteCount--);
					} else {
						for (int i = 0; i < remain; i++)
							leftOver += bytes [byteIndex++] << byteCount--;
					}

					if (leftOver > char.MaxValue && posn + 1 < length || posn < length)
						throw new ArgumentException ("Insufficient space available.");

					if (leftOver > char.MaxValue) {
						chars [posn++] = (char) ((leftOver - 10000) / 0x400 + 0xD800);
						chars [posn++] = (char) ((leftOver - 10000) % 0x400 + 0xDC00);
					} else {
						chars [posn++] = (char) leftOver;
					}

					leftOverLength = 0;
					leftOver = -1;
				}

				while (byteCount > 3) {
					if (bigEndian) {
						ch = (char) (bytes[byteIndex++] << 24 | bytes[byteIndex++] << 16 | bytes[byteIndex++] << 8 | bytes[byteIndex++]);
					} else {
						ch = (char) (bytes[byteIndex++] | bytes[byteIndex++] << 8 | bytes[byteIndex++] << 16 | bytes[byteIndex++] << 24);
					}

					byteCount -= 4;

					if (posn < length) {
						chars[posn++] = ch;
					} else {
						throw new ArgumentException ("Insufficient space available.");
					}
				}

				if (byteCount > 0) {
					leftOverLength = byteCount;
					leftOver = 0;

					if (bigEndian) {
						for (int i = 0; i < byteCount; i++)
							leftOver += bytes [byteIndex++] << (4 - byteCount--);
					} else {
						for (int i = 0; i < byteCount; i++)
							leftOver += bytes [byteIndex++] << byteCount--;
					}

					leftOverByte = leftOver;
				}

				// Finished - return the converted length.
				return posn - charIndex;
			}
		}

		public unsafe override int GetByteCount (char *chars, int count)
		{
			if (chars == null)
				throw new ArgumentNullException ("chars");

			return count * 4;
		}
	}
}
