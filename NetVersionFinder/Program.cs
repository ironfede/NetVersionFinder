
using System.Globalization;
using System.Reflection.PortableExecutable;
using CsvHelper;
using Mono.Cecil;
using NetVersionFinder;

//Get Search path from args or from executing location
string path = args.Length > 0 ? args[0] : Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
if (String.IsNullOrEmpty(path)) return;

//Get all exe and dll files
var files = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories);
files = files.Union(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories)).ToArray();

//Open report csv file
using var writer = new StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\report.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
List<ReportEntry> records = new List<ReportEntry>();

foreach (var file in files)
{
    
    ReportEntry r = new ReportEntry();

    r.Name = Path.GetFileName(file);
    r.Path = file;

    try
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var pEReader = new PEReader(stream);

        //Check if this is a valid .net image
        if (pEReader.PEHeaders.CorHeader != null)
        {
            AssemblyDefinition myLibrary = AssemblyDefinition.ReadAssembly(file);
            var att = myLibrary.MainModule.GetCustomAttributes().Where(a => a.AttributeType.Name == "TargetFrameworkAttribute").FirstOrDefault();
            if (att != null)
            {
                if (att.HasConstructorArguments && att.ConstructorArguments[0].Value != null)
                {
                    string s = att.ConstructorArguments[0].Value.ToString();

                    r.FrameWorkVersion = s;

                    Console.WriteLine(r.Name + " " + r.FrameWorkVersion);
                }
            }
        }
        else
            r.FrameWorkVersion = "Not a CLI module";

    }
    catch (Exception ex)
    {
        r.FrameWorkVersion = "Error: " + ex.Message;
    }

    records.Add(r);
}

csv.WriteRecords(records);
csv.Flush();

