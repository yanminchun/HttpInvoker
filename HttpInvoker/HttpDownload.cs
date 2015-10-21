using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace HttpInvoker
{
    public class HttpRequest
    {
        public HttpWebRequest WebRequest { get; set; }

        public HttpRequest(string url)
        {
            WebRequest = (HttpWebRequest)System.Net.WebRequest.Create(url);

            if (WebRequest.RequestUri.AbsoluteUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.DefaultConnectionLimit = 50;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => { return true; };
            }
        }

        public void SetPostData(string data, Encoding encoding)
        {
            if (data == null)
                data = string.Empty;
            if (encoding == null)
                encoding = Encoding.UTF8;

            Stream requestStream = null;
            try
            {
                byte[] buffer = encoding.GetBytes(data);
                WebRequest.ContentLength = buffer.Length;
                requestStream = WebRequest.GetRequestStream();
                requestStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (requestStream != null)
                {
                    requestStream.Close();
                    requestStream = null;
                }
            }
        }
    }

    public class HttpUnInitException : Exception
    {
        public static readonly string UnInit = "HttpRequest尚未初始化化";

        public HttpUnInitException() :
            this(UnInit)
        {

        }

        public HttpUnInitException(string message)
        {

        }
    }

    public class HttpDownload
    {
        private HttpRequest HttpRequest { get; set; }

        public Encoding ResponseEncoding { get; set; }

        public HttpDownload(HttpRequest httpRequest)
        {
            HttpRequest = httpRequest;
            ResponseEncoding = Encoding.UTF8;
        }

        public HttpDownload(HttpRequest httpRequest, Encoding responseEncoding)
            : this(httpRequest)
        {
            ResponseEncoding = responseEncoding;
        }

        public HttpResult GetResponse()
        {
            HttpResult result = new HttpResult();

            try
            {
                if (HttpRequest.WebRequest == null)
                {
                    throw new HttpUnInitException();
                }

                using (HttpWebResponse response = (HttpWebResponse)(HttpRequest.WebRequest.GetResponse()))
                {
                    using (Stream stream = GetResponseStream(response))
                    {
                        result.Headers = response.Headers;
                        result.StatusCode = response.StatusCode;
                        result.StatusDescription = response.StatusDescription;
                        result.Cookies = response.Cookies;

                        using (MemoryStream _stream = new MemoryStream())
                        {
                            stream.CopyTo(_stream);
                            result.ResponseBuffer = _stream.ToArray();
                            result.ResponseString = ResponseEncoding.GetString(result.ResponseBuffer);
                        }

                        result.Success = true;
                    }
                }
            }
            catch (Exception e)
            {
                result.Exception = e;
            }

            return result;
        }

        private Stream GetResponseStream(HttpWebResponse response)
        {
            Stream stream;
            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
            {
                stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else
            {
                stream = response.GetResponseStream();
            }
            return stream;
        }
    }

    public class HttpResult
    {
        public bool Success { get; set; }

        public string ResponseString { get; set; }

        public byte[] ResponseBuffer { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public WebHeaderCollection Headers { get; set; }

        public string StatusDescription { get; set; }

        public CookieCollection Cookies { get; set; }

        public Exception Exception { get; set; }
    }
}
