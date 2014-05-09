using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    class Program
    {
        static void Main(string[] args)
        {
            PdfTextDocument doc = new PdfTextDocument();
            doc.FromXHtml(@"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\9006642476_bw_fc.html");
            doc.Reflow();
            doc.Export(@"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\9006642476_bw_fc", "BlauwePlaneet");
        }
    }
}
