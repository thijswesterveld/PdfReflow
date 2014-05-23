using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PdfReflow;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationTest
{
    [TestClass]
    public class PageReflow
    {

        private TestContext testContextInstance;
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod()]
        public void ReflowTestDocs()
        {
            foreach(string html in Directory.GetFiles("TestDocs", "*.html"))
            {
                string FileId = Path.GetFileNameWithoutExtension(html);
                foreach(string txt in Directory.GetFiles("TestDocs", string.Format("{0}_p*.txt", FileId)))
                {
                    TextReader tr = new StreamReader(txt, Encoding.UTF8);
                    string expected = tr.ReadToEnd();

                    string pageString = Path.GetFileNameWithoutExtension(txt).Replace(FileId + "_p", "");
                    int page = int.Parse(pageString);
                    PdfTextDocument d = new PdfTextDocument();
                    d.FromXHtmlFile(html, page);
                    TestExtraction(expected, d.Pages[0]);
                }
            }
        }
        
        private void TestExtraction(string expected, Page page)
        {
            string actual = GetText(page);
            var actualTerms = actual.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            var expectedTerms = expected.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(expectedTerms.Length, actualTerms.Length);
            for (int i = 0; i < expectedTerms.Length; ++i)
            {
                Assert.AreEqual(expectedTerms[i], actualTerms[i]);
            }
        }

        private string GetText(Page page)
        {
            page.Reflow();
            return page.ToString();
        }
    }
}
