using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IoTDisplay
{
    public sealed class PhotoReader
    {
        private string _serverUri, _imageFolder;

        private Queue<string> _images;
        private bool _loading = false;

        public PhotoReader(string serverBaseUri, string imageFolder)
        {
            _serverUri = serverBaseUri;
            _imageFolder = imageFolder;

            _images = new Queue<string>();
        }

        public async Task<string> GetImage()
        {
            if (_loading) return null;

            try
            {
                string image = null;

                bool okay = false;
                while (!okay)
                {
                    if (_images.Count == 0)
                        await ReloadImages();

                    image = _images.Dequeue();
                    okay = await ImageExists(image);
                }

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }

        private async Task<bool> ImageExists(string imageUri)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var req = new HttpRequestMessage
                    {
                        Method = new HttpMethod("PROPFIND"),
                        RequestUri = new Uri(imageUri),
                        Headers = { {"Accept", "application/xml" } }
                    };

                    XDocument xmlDoc;
                    using (HttpResponseMessage response = await http.SendAsync(req))
                    {
                        using (Stream xmlStream = await response.Content.ReadAsStreamAsync())
                            xmlDoc = XDocument.Load(xmlStream);
                    }

                    var tagName = XName.Get("getcontenttype", "DAV:");
                    var contentType = xmlDoc.Descendants(tagName).First();

                    return (contentType?.Value == "image/jpeg");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }

        private async Task ReloadImages()
        {
            _loading = true;

            try
            {
                using (var http = new HttpClient())
                {
                    var req = new HttpRequestMessage
                    {
                        Method = new HttpMethod("PROPFIND"),
                        RequestUri = new Uri(_serverUri + _imageFolder),
                        Headers =
                        {
                            {"Accept", "application/xml" },
                            {"Depth", "1" }
                        }
                    };

                    XDocument xmlDoc;
                    using (HttpResponseMessage response = await http.SendAsync(req))
                    {
                        using (Stream xmlStream = await response.Content.ReadAsStreamAsync())
                            xmlDoc = XDocument.Load(xmlStream);
                    }

                    var tagName = XName.Get("href", "DAV:");
                    var hrefs = (from xe in xmlDoc.Descendants(tagName)
                                 let linkStr = xe.Value?.ToLowerInvariant() ?? string.Empty
                                 where linkStr.EndsWith(".jpg") || linkStr.EndsWith(".jpeg")
                                 select xe.Value
                                 );

                    foreach (string imageUri in hrefs)
                        _images.Enqueue(_serverUri + imageUri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
