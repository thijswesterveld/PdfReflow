using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void FromXHtml(string filePath)
        {
            var doc = XElement.Load(filePath);
            Title = doc.XPathSelectElement("//title").Value;
            foreach(var page in doc.XPathSelectElements("//page"))
            {
                Page p = new Page();
                foreach(var word in page.XPathSelectElements("/word"))
                {
                    Word w = new Word();
                    w.Text = word.Value;
                    w.XMin = float.Parse(word.Attribute("xMin").Value);
                    w.XMax =float.Parse(word.Attribute("xMax").Value);
                    w.YMin =float.Parse(word.Attribute("yMin").Value);
                    w.YMin =float.Parse(word.Attribute("yMax").Value);
                    p.AddWord(w);
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



    }
}
