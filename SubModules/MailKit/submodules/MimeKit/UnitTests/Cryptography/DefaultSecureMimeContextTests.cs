﻿//
// DefaultSecureMimeContextTests.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2013-2017 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using NUnit.Framework;

using Org.BouncyCastle.X509;

using MimeKit;
using MimeKit.Cryptography;

using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace UnitTests.Cryptography {
	[TestFixture]
	public class DefaultSecureMimeContextTests
	{
		[Test]
		public void TestArgumentExceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new DefaultSecureMimeContext ((string) null));
			Assert.Throws<ArgumentNullException> (() => new DefaultSecureMimeContext ((IX509CertificateDatabase) null));

			Assert.Throws<ArgumentNullException> (() => new DefaultSecureMimeContext (null, "password"));
			Assert.Throws<ArgumentNullException> (() => new DefaultSecureMimeContext ("fileName", null));

		}
	}
}
