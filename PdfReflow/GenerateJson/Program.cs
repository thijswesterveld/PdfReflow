using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenerateJson
{
    class TMdocument
    {
        public string url;

        public string title;

        public string[] content;

        public string portfolio;
    }

    class EightlyLegsResult
    {
        public string url;
        public string result;
    }

    class Program
    {
        static void Main(string[] args)
        {
            string indexFile = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\Metadata pdfs_TM.csv";
            string inputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\txt_mergereorder\";
            string outputDir = @"C:\Users\ThijsWizeNoze\Documents\pdfhtmltest\json\";
            

            TextReader tr = new StreamReader(indexFile);
            string line = tr.ReadLine();
            while (line != null)
            {
                Console.WriteLine(line);
                string[] fields = line.Split(',');
                string isbn = fields[0];
                string title = fields[1];
                string group = fields[2];
                string subject = fields[3];
                string type = fields[4];

                string baseName = string.Join("_",isbn, subject,  type, "groep"+group);
                foreach (string directory in System.IO.Directory.EnumerateDirectories(inputDir, isbn + "*"))
                {
                    List<EightlyLegsResult> results = new List<EightlyLegsResult>();
                    
                    Directory.CreateDirectory(Path.Combine(outputDir, baseName));
                    foreach (string file in System.IO.Directory.EnumerateFiles(directory, "*.txt"))
                    {
                        Match match = Regex.Match(file, @"p(\d+).txt$");
                        if (match != null && match.Groups.Count > 1)
                        {
                            string p = match.Groups[1].Value;
                            
                            results.Add(Convert(file, baseName, p));
                        }
                    }
                    string outputFile = Path.Combine(outputDir, baseName + ".json");
                    TextWriter tw = new StreamWriter(outputFile);
                    string json = JsonConvert.SerializeObject(results,Formatting.Indented);
                    tw.WriteLine(json);
                    tw.Flush();
                    tw.Close();
                }
                line = tr.ReadLine();
            }
        }

        private static EightlyLegsResult Convert(string inputFile, string portfolio, string page)
        {
            TextReader tr = new StreamReader(inputFile);
            
            TMdocument doc = new TMdocument();
            doc.portfolio = portfolio;
            doc.url = string.Format("{0}_p{1}",portfolio,page);
            List<string> paragraphs = new List<string>();
            StringBuilder paragraph = new StringBuilder();
            
            string line = tr.ReadLine();
            while (line != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    paragraphs.Add(paragraph.ToString());
                    paragraph = new StringBuilder();
                }
                else
                {
                    paragraph.AppendLine(line);
                }
                line = tr.ReadLine();
            }
            doc.content = paragraphs.ToArray();            
            
            string json = JsonConvert.SerializeObject(doc,Formatting.None).Replace("\"","\"");
            return new EightlyLegsResult()
            {
                url = doc.url,
                result = json
            };

        }
    }
}
