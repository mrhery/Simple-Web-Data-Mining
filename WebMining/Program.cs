using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;



List<string> urlsToFetch = new List<string>();
List<dynamic> jsons = new List<dynamic>();

void Start() {
    urlsToFetch.Clear();
    jsons.Clear();

    Console.Write("HTML Content ID: ");
    string contentId = Console.ReadLine();

    Console.Write("Masukkan URL: ");
    string mainUrl = Console.ReadLine();

    Console.Write("Masukkan Tajuk: ");
    string tajuk = Console.ReadLine();

    var uri = new Uri(mainUrl);
    string baseUrl = uri.Scheme + "://" + uri.Host;
    string basePath = mainUrl.Replace(baseUrl, "");

    Console.WriteLine("========================");
    Console.WriteLine("Maklumat URL untuk di proses:");
    Console.WriteLine("URL Utama: " + baseUrl);
    Console.WriteLine("Path Utama: " + basePath);

    Console.Write("Mahu crawurl atau tidak? y/n: ");
    string yesno = Console.ReadLine();

    if (yesno == "y" || yesno == "Y")
    {
        ListUrl(mainUrl, baseUrl, basePath);
        Console.WriteLine("Jumlah URL: " + urlsToFetch.Count);
    }

    Console.WriteLine("========================");

    if (yesno == "y" || yesno == "Y")
    {
        foreach (string url in urlsToFetch)
        {
            ProcessContent(baseUrl + url, contentId);
        }
    }
    else
    {
        ProcessContent(mainUrl, contentId);
    }

    var invalidChars = System.IO.Path.GetInvalidFileNameChars();

    if (!Directory.Exists("datas"))
    {
        Directory.CreateDirectory("datas");
    }

    File.WriteAllText("datas/" + new string(tajuk.Where(m => !invalidChars.Contains(m)).ToArray<char>()) + ".json", JsonConvert.SerializeObject(jsons));

    Console.WriteLine("============ Proses Selesai =============");

    Start();
}

void ListUrl(string mainUrl, string baseUrl, string basePath)
{
    WebClient wb = new WebClient();
    string html = wb.DownloadString(mainUrl);

    HtmlDocument doc = new HtmlDocument();
    doc.LoadHtml(html);

    foreach(var link in doc.DocumentNode.SelectNodes("//a[@href]"))
    {
        string url = link.GetAttributeValue("href", null);
        url = url.Replace(baseUrl, "");

        if (url.StartsWith(basePath))
        {
            urlsToFetch.Add(url);
        }
    }
}

void ProcessContent(string url, string contentId)
{
    Console.WriteLine("Sedang Diproses: " + url + " ");
    WebClient wb = new WebClient();
    string html = wb.DownloadString(url);

    HtmlDocument docx = new HtmlDocument();
    docx.LoadHtml(html);

    string title = docx.DocumentNode.Descendants("title").FirstOrDefault().InnerText;

    if(docx.GetElementbyId(contentId) != null)
    {
        string content = docx.GetElementbyId(contentId).InnerHtml;

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(content);

        doc.DocumentNode.Descendants()
            .Where(n => n.Name == "script" || n.Name == "style" || n.Name == "h1" || n.Name == "h2")
            .ToList()
            .ForEach(n => n.Remove());

        var divs = doc.DocumentNode.SelectNodes("//div");

        foreach (var div in divs)
        {
            if (div.HasClass("cover"))
            {
                div.Remove();
            }

            if (div.HasClass("tutorial-menu"))
            {
                div.Remove();
            }

            if (div.HasClass("google-bottom-ads"))
            {
                div.Remove();
            }

            if (div.HasClass("google-top-ads"))
            {
                div.Remove();
            }

            if (div.HasClass("mui-container-fluid"))
            {
                div.Remove();
            }

            if (div.HasClass("execute"))
            {
                div.Remove();
            }

            if (div.Id == "bottom_navigation")
            {
                div.Remove();
            }
        }

        var uls = doc.DocumentNode.SelectNodes("//ul");

        if (uls != null)
        {
            foreach (var ul in uls)
            {
                string pul = ul.InnerHtml;
                string npul = Regex.Replace(pul, @"<(.|\n)*?>", "");

                ul.ParentNode.ReplaceChild(HtmlNode.CreateNode(npul), ul);
            }
        }


        var pres = doc.DocumentNode.SelectNodes("//pre");

        if (pres != null)
        {
            foreach (var pre in pres)
            {
                try
                {
                    string preul = "```\n" + pre.InnerHtml + "\n```";
                    pre.ParentNode.ReplaceChild(HtmlNode.CreateNode(preul), pre);
                }catch(Exception ex)
                {
                    continue;
                }
                
            }
        }

        var nodes = doc.DocumentNode.ChildNodes;

        Console.Write("Tajuk: " + title + "\n\n");

        string cleaned = "";

        foreach (var node in nodes)
        {
            string p = node.OuterHtml;

            p = Regex.Replace(p, @"<(.|\n)*?>", "");
            cleaned += p;
        }

        cleaned = cleaned.Trim('\r', '\n');

        dynamic obj = new ExpandoObject();

        obj.title = title;
        obj.text = cleaned;
        obj.labels = new string[] { "java", "programming" };

        jsons.Add(obj);

        Console.WriteLine("Siap");
        Console.WriteLine("-----------------------");
    }
    else
    {
        Console.WriteLine("Tiada content dijumpai dengan id " + contentId);
    }
}


Start();
