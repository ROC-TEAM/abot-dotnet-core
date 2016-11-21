﻿using Abot.Core;
using Abot.Poco;
using Abot.Tests.Unit.Helpers;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    [Ignore("Should replace Fiddler")]
    public class WebContentExtractorTest
    {
        WebContentExtractor _uut;
        UnitTestConfig unitTestConfig = new UnitTestConfig();
        Uri _utf8 { get { return new Uri(unitTestConfig.SiteSimulatorBaseAddress); } }
        Uri _japan = new Uri("http://aaa.jp");
        Uri _japanMetaSingleQuotes = new Uri("http://aaa2.jp");
        Uri _japanMetaDoubleQuotesAndClose = new Uri("http://aaa3.jp");
        Uri _japanMetaSingleQuotesAndClose = new Uri("http://aaa4.jp");

        [SetUp]
        public void Setup()
        {
            _uut = new WebContentExtractor();
        }

        [Test]
        public async Task GetContent_Utf8()
        {
            PageContent result = null;
            using (HttpResponseMessage response = GetWebStream(_utf8))
            {
                result = await _uut.GetContentAsync(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("utf-8", result.Charset);
            Assert.AreEqual(Encoding.UTF8, result.Encoding);
            Assert.IsTrue(result.Text.StartsWith("<!DOCTYPE html>\r\n<html>\r\n<head>\r\n"));

        }

        [Test]
        public async Task GetContent_NonUtf8()
        {
            PageContent result = null;
            using (HttpResponseMessage response = GetWebStream(_japan))
            {
                result = await _uut.GetContentAsync(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public async Task GetContent_MetaSingleQuotes_NonUtf8()
        {
            PageContent result = null;
            using (HttpResponseMessage response = GetWebStream(_japanMetaSingleQuotes))
            {
                result = await _uut.GetContentAsync(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public async Task GetContent_MetaDoubleQuotesAndClose_NonUtf8()
        {
            PageContent result = null;
            using (HttpResponseMessage response = GetWebStream(_japanMetaDoubleQuotesAndClose))
            {
                result = await _uut.GetContentAsync(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public async Task GetContent_MetaSingleQuotesAndClose_NonUtf8()
        {
            PageContent result = null;
            using (HttpResponseMessage response = GetWebStream(_japanMetaSingleQuotesAndClose))
            {
                result = await _uut.GetContentAsync(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        [Ignore("TODO Cannot create response with StremContent")]
        public async Task GetContent_Cp1251_ConvertsToWindows1251()
        {
            //WebRequest.RegisterPrefix("test", new TestWebRequestCreate());
            TestWebRequest request = TestWebRequestCreate.CreateTestRequest("<meta http-equiv=Content-Type content=\"text/html; charset=cp1251\">");
            var response = request.GetResponse();
             
            PageContent result = await _uut.GetContentAsync(response);

            Assert.IsNotNull(result.Bytes);
            Assert.AreEqual(66, result.Bytes.Length);
            Assert.AreEqual("windows-1251", result.Charset);
            Assert.AreEqual("System.Text.SBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        private HttpResponseMessage GetWebStream(Uri uri)
        {
            using (var client = new HttpClient())
            {
                return client.GetAsync(uri).GetAwaiter().GetResult();
            }
        }
    }


    class TestWebRequestCreate 
    {
        static HttpRequestMessage nextRequest;
        static object lockObject = new object();

        static public HttpRequestMessage NextRequest
        {
            get { return nextRequest; }
            set
            {
                lock (lockObject)
                {
                    nextRequest = value;
                }
            }
        }

        /// <summary>See <see cref="IWebRequestCreate.Create"/>.</summary>
        public HttpRequestMessage Create(Uri uri)
        {
            return nextRequest;
        }

        /// <summary>Utility method for creating a TestWebRequest and setting 
        /// it to be the next WebRequest to use.</summary>
        /// <param name="response">The response the TestWebRequest will return.</param>
        public static TestWebRequest CreateTestRequest(string response)
        {
            TestWebRequest request = new TestWebRequest(response);
            NextRequest = request;
            return request;
        }
    }

    class TestWebRequest : HttpRequestMessage
    {
        MemoryStream requestStream = new MemoryStream();
        MemoryStream responseStream;
        
        /// <summary>Initializes a new instance of <see cref="TestWebRequest"/> 
        /// with the response to return.</summary>
        public TestWebRequest(string response)
        {
            responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));
        }

        /// <summary>Returns the request contents as a string.</summary>
        public string ContentAsString()
        {
            return System.Text.Encoding.UTF8.GetString(requestStream.ToArray());
        }
        
        /// <summary>See <see cref="WebRequest.GetResponse"/>.</summary>
        public HttpResponseMessage GetResponse()
        {
            return new TestWebReponse(responseStream);
        }
    }

    class TestWebReponse : HttpResponseMessage
    {
        Stream responseStream;
        WebHeaderCollection headers;

        /// <summary>Initializes a new instance of <see cref="TestWebReponse"/> 
        /// with the response stream to return.</summary>
        public TestWebReponse(Stream responseStream)
        {
            Content = new StreamContent(responseStream);
            this.responseStream = responseStream;
            headers = new WebHeaderCollection();
        }
    }
}
