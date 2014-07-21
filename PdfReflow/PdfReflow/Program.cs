using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    class Program
    { 
        static void Main(string[] args)
        {
            string indexFile = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\MetadataPdfs_TM_VO.csv";
            string inputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\bboxhtml\";
            string outputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\trainingData\";

            TextReader tr = new StreamReader(indexFile);
            string line = tr.ReadLine();
            while(line != null)
            {
                Console.WriteLine(line);
                string[] fields = line.Split(',');
                string isbn = fields[0];
                string title = fields[1];
                string group = fields[2]; 
                string subject = fields[3];
                string type =fields[4];

                foreach (string file in System.IO.Directory.EnumerateFiles(inputDir, "*"+isbn + "*.html"))
                {
                    string baseName = isbn + "_" + subject + "_" + type;
                    PdfTextDocument doc = new PdfTextDocument();
                    doc.FromXHtmlFile(file);
                    doc.Reflow();
                    string path = System.IO.Path.Combine(outputDir, group);
                    if(!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }
                    doc.Export(path, baseName);
                }
                line = tr.ReadLine();
            }
            Console.Read();
        }
    }
}
