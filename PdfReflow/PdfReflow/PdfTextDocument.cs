using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace PdfReflow
{
    public class PdfTextDocument
    {
        public PdfTextDocument()
        {
            Pages = new List<Page>();
        }

        /// <summary>
        /// Read HTML file with pages, words and bounding boxes into pdftextdocument format
        /// </summary>
        /// <param name="filePath">Path to HTML file which is output of `pdftotext -enc Latin1 -raw -bbox somefile.pdf`</param>
        public void FromXHtmlFile(string filePath, int pageNumber = -1)
        {
            TextReader tr = new StreamReader(filePath, Encoding.GetEncoding("ISO-8859-1"));
            FromXHtmlString(tr.ReadToEnd(), pageNumber);
        }

        public void FromXHtmlString(string text, int pageNumber = -1)
        {
            var doc = XElement.Parse(text);

            // Create a namespace manager
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
            
            // Define your default namespace including a prefix to be used later in the XPath expression
            namespaceManager.AddNamespace("xhtml","http://www.w3.org/1999/xhtml");
            
            int pageSequence = 0;
            var title = doc.XPathSelectElement("//xhtml:title", namespaceManager);
            if(title != null)
            {
                this.Title = title.Value;
            }
            var pages = doc.XPathSelectElements("//xhtml:page", namespaceManager);
            if(pageNumber >0)
            {
                pages = pages.Where((p,i) => i == pageNumber-1);
            }
            foreach (var page in pages)
            {
                float pageWidth = float.Parse(page.Attribute("width").Value);
                float pageHeight = float.Parse(page.Attribute("height").Value);
                int ignoreBorder = 10; /// number of pts on edges of page to ignore
                Page p = new Page();
                p.PageNumber = ++pageSequence;
                foreach (var word in page.XPathSelectElements("./xhtml:word", namespaceManager))
                {
                    Word w = new Word();
                    int idx = word.Value.IndexOf((char)8226);
                    if(idx >= 0)
                    {
                        // replace bullet '•' with '-'
                        word.Value=word.Value.Replace((char)8226,'-');
                    }
                    w.Text = word.Value;
                    
                    w.XMin = (float)Math.Round(float.Parse(word.Attribute("xMin").Value),2);
                    w.XMax = (float)Math.Round(float.Parse(word.Attribute("xMax").Value), 2);
                    w.YMin = (float)Math.Round(float.Parse(word.Attribute("yMin").Value), 2);
                    w.YMax = (float)
                        Math.Round(float.Parse(word.Attribute("yMax").Value), 2);
                    if (w.YMax > pageHeight - ignoreBorder || w.XMax > pageWidth - ignoreBorder || w.YMin < ignoreBorder || w.XMin < ignoreBorder)
                    {
                        //Console.WriteLine("Skip word: {0}", w.Text);
                    }
                    else
                    {
                        p.AddWord(w);
                    }
                }
                Pages.Add(p);
            }
        }

        public void Reflow()
        {
            foreach(Page p in Pages)
            {
                    p.Reflow();
            }
        }

        /// <summary>
        /// Document title
        /// </summary>
        public string Title;

        /// <summary>
        /// Pages of the document
        /// </summary>
        public List<Page> Pages;

        public void Export(string basePath, string title)
        {
            foreach(Page p in Pages)
            {
                string file = Path.Combine(basePath,string.Format("{0}_p{1:000}.txt",title,p.PageNumber));
                if(!System.IO.Directory.Exists(basePath))
                {
                    System.IO.Directory.CreateDirectory(basePath);
                }
                TextWriter tw = new StreamWriter(file, false, Encoding.UTF8);
                tw.Write(p.ToString());
                tw.Flush();
                tw.Close();
            }
            
        }


    }
}
