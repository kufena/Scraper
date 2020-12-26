using System;
using HtmlAgilityPack;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Scraper
{
    //
    // A scraper static class.
    class Program
    {

        // Document types.  Will we need these?
        enum Type { html, css, other };

        //
        // Takes as arguments an initial site or document to scrape, and a
        // directory name to write the contents to, or maybe update, if we're
        // clever about it.
        static async Task Main(string[] args)
        {
            Uri uri = new Uri(args[0]);
            string host = uri.Host;

            Console.WriteLine($"Parts of URI = {uri.Host} {uri.Scheme} {uri.LocalPath} {uri.PathAndQuery} {uri.Query}");

            Stack<Uri> retrievals = new Stack<Uri>();
            retrievals.Push(uri);
            HashSet<Uri> seen = new HashSet<Uri>();

            while (retrievals.Count > 0)
            {
                Uri referral = retrievals.Pop();
                seen.Add(referral);
                Console.WriteLine($"Retrieving next on list, {referral}");

                var type = Type.html;
                if (referral.LocalPath.EndsWith(".css"))
                    type = Type.css;
                if (referral.LocalPath.EndsWith(".pdf"))
                    type = Type.other;

                var newlinks = await HandleDocument(referral, type);
                foreach(var ll in newlinks)
                {
                    if (ll.Host == host)
                    {
                        if (!seen.Contains(ll))
                            retrievals.Push(ll);
                    }
                }
            }
        }

        //
        // Takes a document name and a type, does the http request to
        // retrieve that document, and then processes it.
        // Processing might mean 
        private static async Task<List<Uri>> HandleDocument(Uri filename, Type ofdoc)
        {
            HttpClientHandler hch = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            HttpClient htmlclient = new HttpClient(hch);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, filename);
            var links = new List<Uri>();
            var response = await htmlclient.SendAsync(request,HttpCompletionOption.ResponseContentRead);
            if (response.IsSuccessStatusCode)
            {
                var bodystr = await response.Content.ReadAsStringAsync();
                Uri location = response.Headers.Location;
                if (location == null) location = filename;

                switch (ofdoc)
                {
                    case Type.html:

                        var x = new HtmlDocument();
                        x.LoadHtml(bodystr);

                        findLinks(links, x.DocumentNode, location);
                        Console.WriteLine($"Found {links.Count} links.");
                        break;

                    case Type.css:
                    case Type.other:
                        break;

                }
            }

            return links;
        }

        //
        // Goes through an HTML document tree to find links and a elements, and
        // returns a list of, hopefully unique links.
        static void findLinks(List<Uri> links, HtmlNode node, Uri rootlocation)
        {
            //if (node.NodeType == HtmlNodeType.Element)
            //    Console.WriteLine(node.Name);
            if (node.NodeType == HtmlNodeType.Element && node.Name == "link")
            {
                var refs = node.Attributes.AttributesWithName("href");
                foreach (var r in refs)
                {
                    Uri newr = new Uri(rootlocation, r.Value);
                    if (!links.Contains(newr))
                        links.Add(newr);
                }
            }
            if (node.NodeType == HtmlNodeType.Element && node.Name == "a")
            {
                var refs = node.Attributes.AttributesWithName("href");
                foreach (var r in refs)
                {
                    Uri newr = new Uri(rootlocation, r.Value);
                    if (!links.Contains(newr))
                        links.Add(newr);
                }
            }

            if (node.FirstChild != null)
            {
                findLinks(links, node.FirstChild, rootlocation);
            }

            if (node.NextSibling != null)
                findLinks(links, node.NextSibling, rootlocation);
        }
    }
}
