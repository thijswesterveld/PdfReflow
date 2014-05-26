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
            string inputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\bboxhtml\";
            string outputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\txt_mergereorder\";
            foreach (string file in System.IO.Directory.EnumerateFiles(inputDir, "9006642476*.html"))
            {
                Console.WriteLine(file);
                string baseName = System.IO.Path.GetFileNameWithoutExtension(file);
                PdfTextDocument doc = new PdfTextDocument();
                doc.FromXHtmlFile(file,20);
                doc.Reflow();
                doc.Export(System.IO.Path.Combine(outputDir,baseName), baseName);
            }
            Console.Read();
        }
    }
}
