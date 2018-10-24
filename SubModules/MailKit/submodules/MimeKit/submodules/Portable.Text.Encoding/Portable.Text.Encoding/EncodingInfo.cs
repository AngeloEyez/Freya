//
// System.Text.EncodingInfo.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//

//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


namespace Portable.Text
{
	public sealed class EncodingInfo
	{
		readonly int codepage;
		Encoding encoding;

		internal EncodingInfo (int cp)
		{
			codepage = cp;
		}

		public int CodePage {
			get { return codepage; }
		}

		public string DisplayName {
			get { return Name; }
		}

		public string Name {
			get {
				if (encoding == null)
					encoding = GetEncoding ();
				return encoding.WebName;
			}
		}

		public override bool Equals (object obj)
		{
			var info = obj as EncodingInfo;

			return info != null && info.codepage == codepage;
		}

		public override int GetHashCode ()
		{
			return codepage;
		}

		public Encoding GetEncoding ()
		{
			return Encoding.GetEncoding (codepage);
		}
	}
}